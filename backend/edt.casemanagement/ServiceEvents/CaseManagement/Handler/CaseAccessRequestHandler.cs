namespace edt.casemanagement.ServiceEvents.CaseManagement.Handler;

using edt.casemanagement.Exceptions;
using edt.casemanagement.HttpClients.Services.EdtCore;
using edt.casemanagement.Kafka.Interfaces;
using edt.casemanagement.ServiceEvents.CaseManagement.Models;
using EdtService.HttpClients.Keycloak;

public class CaseAccessRequestHandler : IKafkaHandler<string, SubAgencyDomainEvent>
{


    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;


    public CaseAccessRequestHandler(
    EdtServiceConfiguration configuration,
    IKeycloakAdministrationClient keycloakAdministrationClient,
    IEdtClient edtClient,
     ILogger logger)
    {
        this.configuration = configuration;
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.logger = logger;
        this.edtClient = edtClient;
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
            var partId = userInfo.Attributes.GetValueOrDefault("partId").FirstOrDefault();
            if (string.IsNullOrEmpty(partId))
            {
                // get the EDT user info
                Serilog.Log.Error("No partId found for {0} - possible attempt to bypass security", caseEvent.UserId);
            }
            else
            {
                var result = await this.edtClient.HandleCaseRequest(partId, caseEvent);


            }



        }

        return Task.CompletedTask;
    }



}
