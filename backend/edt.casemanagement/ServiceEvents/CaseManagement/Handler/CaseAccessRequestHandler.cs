namespace edt.casemanagement.ServiceEvents.CaseManagement.Handler;

using edt.casemanagement.Data;
using edt.casemanagement.Exceptions;
using edt.casemanagement.HttpClients.Services.EdtCore;
using edt.casemanagement.Kafka.Interfaces;
using edt.casemanagement.Models;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;
using edt.casemanagement.ServiceEvents.UserAccountCreation.Models;
using EdtService.HttpClients.Keycloak;
using NodaTime.Extensions;
using Prometheus;

public class CaseAccessRequestHandler : IKafkaHandler<string, SubAgencyDomainEvent>
{


    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IKafkaProducer<string, NotificationAckModel> producer;
    private readonly CaseManagementDataStoreDbContext context;
    private static readonly Histogram CaseRequestDuration = Metrics
    .CreateHistogram("case_request_duration", "Histogram of case request call durations.");

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

        Serilog.Log.Information("Received request for event {0} case {1} party {2} {3}", caseEvent.EventType, caseEvent.CaseId, caseEvent.PartyId, caseEvent.UserId);



        // get the cases the user currently has access to
        // var currentCases = edtClient.GetUserCases(
        using var trx = this.context.Database.BeginTransaction();

        using (CaseRequestDuration.NewTimer())
        {
            var userInfo = await this.keycloakAdministrationClient.GetUser("BCPS", caseEvent.UserId);


            if (userInfo == null)
            {
                throw new EdtServiceException($"userinfo not found for {caseEvent.UserId}");
            }
            else
            {



                var partId = userInfo.Attributes.GetValueOrDefault("partId").FirstOrDefault();
                if (string.IsNullOrEmpty(partId))
                {
                    // get the EDT user info
                    Serilog.Log.Error("No partId found for {0} - possible attempt to bypass security", caseEvent.UserId);
                }
                else
                {
                    var result = await this.edtClient.HandleCaseRequest(partId, caseEvent);

                    if (result != null && result.IsCompleted)
                    {
                        try
                        {
                            var caseRequest = new CaseRequest
                            {
                                AgencFileNumber = caseEvent.AgencyFileNumber,
                                PartyId = caseEvent.PartyId,
                                Party = caseEvent.Username,
                                Status = "Complete",
                                CaseId = caseEvent.CaseId,
                                RemoveRequested = caseEvent.EventType.Equals(CaseEventType.Decommission, StringComparison.Ordinal),
                                Requested = caseEvent.RequestedOn.ToInstant()
                            };

                            if (result != null && result.Status == TaskStatus.RanToCompletion && result.Exception == null)
                            {

                                caseRequest.Status = "Complete";
                                //save notification ref in notification table database
                                var response = await this.context.CaseRequests.AddAsync(caseRequest);


                                Serilog.Log.Information($"Sending completed response for user {caseEvent.UserId} and case {caseEvent.CaseId}");

                                if (result.IsCompleted)
                                {
                                    var uniqueKey = Guid.NewGuid().ToString();

                                    var producerResponse = await this.producer.ProduceAsync(this.configuration.KafkaCluster.AckTopicName, key: uniqueKey, new NotificationAckModel
                                    {
                                        Status = "Complete",
                                        AccessRequestId = caseEvent.RequestId,
                                        PartId = partId,
                                        EmailAddress = userInfo.Email,
                                        Subject = NotificationSubject.CaseAccessRequest,
                                        EventType = caseEvent.EventType
                                    });

                                    Serilog.Log.Information($"Response {producerResponse}");

                                }
                            }
                            else
                            {
                                Serilog.Log.Error($"Sending failure response for user {caseEvent.UserId} and case {caseEvent.CaseId} [{string.Join(",", result.Exception)}]");

                                caseRequest.Status = "Failure";
                                caseRequest.Details = string.Join(",", result.Exception);
                                //save notification ref in notification table database
                                var response = await this.context.CaseRequests.AddAsync(caseRequest);

                                var uniqueKey = Guid.NewGuid().ToString();

                                await this.producer.ProduceAsync(this.configuration.KafkaCluster.AckTopicName, key: uniqueKey, new NotificationAckModel
                                {
                                    Status = "Failure",
                                    AccessRequestId = caseEvent.RequestId,
                                    PartId = partId,
                                    Details = (result?.Exception != null) ? result.Exception.Message : "No details provided",
                                    EmailAddress = userInfo.Email,
                                    Subject = NotificationSubject.CaseAccessRequest,
                                    EventType = caseEvent.EventType
                                });
                            }


                            //add to tell message has been processed by consumer
                            await this.context.IdempotentConsumer(messageId: key, consumer: consumerName);
                            await this.context.SaveChangesAsync();
                            await trx.CommitAsync();

                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error("Failed to process request {0}", string.Join(", ", ex.Message));
                            await trx.RollbackAsync();
                            return Task.FromException(new CaseAssignmentException($"Failed to process request {caseEvent}"));

                        }

                    }



                }

                return Task.CompletedTask;
            }
        }


    }
}
