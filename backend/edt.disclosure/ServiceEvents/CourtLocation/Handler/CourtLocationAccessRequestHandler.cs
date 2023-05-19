namespace edt.disclosure.ServiceEvents.CourtLocation.Handler;

using edt.disclosure.Data;
using edt.disclosure.Exceptions;
using edt.disclosure.HttpClients.Keycloak;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.Models;
using Prometheus;



/// <summary>
/// Handle requests to access court location case folders
/// </summary>
public class CourtLocationAccessRequestHandler : IKafkaHandler<string, CourtLocationDomainEvent>
{
    private readonly EdtDisclosureServiceConfiguration configuration;
    private readonly IEdtDisclosureClient edtClient;
    private readonly ILogger logger;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IKafkaProducer<string, NotificationAckModel> producer;
    private readonly DisclosureDataStoreDbContext context;
    private static readonly Histogram CourtLocationRequestDuration = Metrics.CreateHistogram("court_location_request_duration", "Histogram of court location request call durations.");

    public CourtLocationAccessRequestHandler(
    EdtDisclosureServiceConfiguration configuration,
    IKeycloakAdministrationClient keycloakAdministrationClient,
    IKafkaProducer<string, NotificationAckModel> producer,
    DisclosureDataStoreDbContext context,

    IEdtDisclosureClient edtClient,
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

        using (CourtLocationRequestDuration.NewTimer())
        {
            var userInfo = await this.keycloakAdministrationClient.GetUser(courtLocationEvent.UserId) ?? throw new EdtDisclosureServiceException($"Userinfo not found for {courtLocationEvent.UserId}");
            Serilog.Log.Information("Received request for court location {0} case {1} party {2}", courtLocationEvent.EventType, courtLocationEvent.CourtLocation, courtLocationEvent.PartyId);

            // we'll flag it as completed and send a message back
            var uniqueKey = Guid.NewGuid().ToString();

            var result = await this.edtClient.HandleCourtLocationRequest(uniqueKey, courtLocationEvent);

            return Task.CompletedTask;
        }
    }
}
