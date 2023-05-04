namespace edt.casemanagement.ServiceEvents.CourtLocation.Handler;

using edt.casemanagement.Data;
using edt.casemanagement.Exceptions;
using edt.casemanagement.HttpClients.Services.EdtCore;
using edt.casemanagement.Kafka.Interfaces;
using edt.casemanagement.ServiceEvents.CourtLocation.Models;
using edt.casemanagement.ServiceEvents.UserAccountCreation.Models;
using EdtService.HttpClients.Keycloak;
using Microsoft.Identity.Client;
using Prometheus;



/// <summary>
/// Handle requests to access court location case folders
/// </summary>
public class CourtLocationAccessRequestHandler : IKafkaHandler<string, CourtLocationDomainEvent>
{
    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IKafkaProducer<string, NotificationAckModel> producer;
    private readonly CaseManagementDataStoreDbContext context;
    private static readonly Histogram CourtCaseRequestDuration = Metrics.CreateHistogram("court_location_request_duration", "Histogram of court location request call durations.");

    public CourtLocationAccessRequestHandler(
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

    public async Task<Task> HandleAsync(string consumerName, string key, CourtLocationDomainEvent courtLocationEvent)
    {

        using (CourtCaseRequestDuration.NewTimer())
        {
            var userInfo = await this.keycloakAdministrationClient.GetUser(courtLocationEvent.UserId) ?? throw new EdtServiceException($"Userinfo not found for {courtLocationEvent.UserId}");
            Serilog.Log.Information("Received request for court location {0} case {1} party {2}", courtLocationEvent.EventType, courtLocationEvent.CourtLocation, courtLocationEvent.PartyId);

            // we'll flag it as completed and send a message back
            var uniqueKey = Guid.NewGuid().ToString();

            var result = await this.edtClient.HandleCourtLocationRequest(uniqueKey, courtLocationEvent);

            return Task.CompletedTask;
        }
    }
}
