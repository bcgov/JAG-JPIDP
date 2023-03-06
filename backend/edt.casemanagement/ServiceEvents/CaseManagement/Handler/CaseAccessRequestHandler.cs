namespace edt.casemanagement.ServiceEvents.CaseManagement.Handler;

using edt.casemanagement.Data;
using edt.casemanagement.Exceptions;
using edt.casemanagement.HttpClients.Services.EdtCore;
using edt.casemanagement.Kafka.Interfaces;
using edt.casemanagement.Models;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;
using edt.casemanagement.ServiceEvents.UserAccountCreation.Models;
using EdtService.HttpClients.Keycloak;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Server.IIS.Core;
using NodaTime.Extensions;

public class CaseAccessRequestHandler : IKafkaHandler<string, SubAgencyDomainEvent>
{


    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IKafkaProducer<string, NotificationAckModel> producer;
    private readonly CaseManagementDataStoreDbContext context;


    public CaseAccessRequestHandler(
    EdtServiceConfiguration configuration,
    IKeycloakAdministrationClient keycloakAdministrationClient,
    IKafkaProducer<string, NotificationAckModel> producer,
    CaseManagementDataStoreDbContext context,

    IEdtClient edtClient,
     ILogger logger)
    {
        this.configuration = configuration;
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.logger = logger;
        this.context = context;
        this.edtClient = edtClient;
        this.producer = producer;
    }

    public async Task<Task> HandleAsync(string consumerName, string key, SubAgencyDomainEvent caseEvent)
    {

        Serilog.Log.Information("Received request for event {0} case {1} party {2}", caseEvent.EventType, caseEvent.CaseId, caseEvent.PartyId);

        // get the user from keycloak

        // get the cases the user currently has access to
        // var currentCases = edtClient.GetUserCases(

        var userInfo = await this.keycloakAdministrationClient.GetUser(caseEvent.UserId);

        if (userInfo == null)
        {
            throw new EdtServiceException($"serinfo not found for {caseEvent.UserId}");
        }
        else
        {

            using var trx = context.Database.BeginTransaction();


            var partId = userInfo.Attributes.GetValueOrDefault("partId").FirstOrDefault();
            if (string.IsNullOrEmpty(partId))
            {
                // get the EDT user info
                Serilog.Log.Error("No partId found for {0} - possible attempt to bypass security", caseEvent.UserId);
            }
            else
            {
                var result = await this.edtClient.HandleCaseRequest(partId, caseEvent);

                try
                {
                    //save notification ref in notification table database
                    await this.context.CaseRequests.AddAsync(new CaseRequest
                    {
                        AgencFileNumber = caseEvent.AgencyFileNumber,
                        PartyId = caseEvent.PartyId,
                        CaseId = caseEvent.CaseId,
                        RemoveRequested = caseEvent.EventType.Equals(CaseEventType.Decommission, StringComparison.Ordinal),
                        Requested = caseEvent.RequestedOn.ToInstant()
                    });
                    await this.context.SaveChangesAsync();


                    if (result != null)
                    {
                        if (result.IsCompleted)
                        {
                            var uniqueKey = Guid.NewGuid().ToString();

                            await this.producer.ProduceAsync(this.configuration.KafkaCluster.AckTopicName, key: uniqueKey, new NotificationAckModel
                            {
                                Status = "Completed",
                                AccessRequestId = caseEvent.RequestId,
                                PartId = partId,
                                EmailAddress = userInfo.Email,
                                Subject = NotificationSubject.CaseAccessRequest,
                                EventType = caseEvent.EventType
                            });
                        }
                    }

                    await trx.CommitAsync();

                }
                catch (Exception ex) {
                    Serilog.Log.Error("Failed to process request {0}", string.Join(", ", ex.Message));
                    await trx.RollbackAsync();
                    return Task.FromException(new CaseAssignmentException($"Failed to process request {caseEvent}"));

                }

            }



        }

        return Task.CompletedTask;
    }



}
