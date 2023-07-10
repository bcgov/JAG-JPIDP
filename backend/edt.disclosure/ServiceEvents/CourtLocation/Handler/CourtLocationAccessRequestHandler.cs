namespace edt.disclosure.ServiceEvents.CourtLocation.Handler;

using edt.disclosure.Data;
using edt.disclosure.Exceptions;
using edt.disclosure.HttpClients.Keycloak;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using edt.disclosure.ServiceEvents.Models;
using NodaTime;
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
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> producer;
    private readonly DisclosureDataStoreDbContext context;
    private static readonly Histogram CourtLocationRequestDuration = Metrics.CreateHistogram("court_location_request_duration", "Histogram of court location request call durations.");

    public CourtLocationAccessRequestHandler(
    EdtDisclosureServiceConfiguration configuration,
    IKeycloakAdministrationClient keycloakAdministrationClient,
    IKafkaProducer<string, GenericProcessStatusResponse> producer,
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
            var processResponseKey = Guid.NewGuid().ToString();

            try
            {
                var userInfo = await this.keycloakAdministrationClient.GetUser(courtLocationEvent.UserId) ?? throw new EdtDisclosureServiceException($"Userinfo not found for {courtLocationEvent.UserId}");
                Serilog.Log.Information("Received request for court location {0} case {1} party {2}", courtLocationEvent.EventType, courtLocationEvent.CourtLocationKey, courtLocationEvent.PartyId);

                // we'll flag it as completed and send a message back
                var uniqueKey = Guid.NewGuid().ToString();

                var result = await this.edtClient.HandleCourtLocationRequest(uniqueKey, courtLocationEvent);

                Serilog.Log.Information($"Publishing court location response for {courtLocationEvent.RequestId} {courtLocationEvent.Username} result: {result}");

                if (result.IsCompletedSuccessfully)
                {
                    var produced = this.producer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, processResponseKey, new GenericProcessStatusResponse
                    {
                        DomainEvent = courtLocationEvent.EventType == "court-location-provision" ? "digitalevidence-court-location-provision-complete" : "digitalevidence-court-location-decommission-complete",
                        PartId = courtLocationEvent.Username,
                        Id = courtLocationEvent.RequestId,
                        Status = "Complete",
                        EventTime = SystemClock.Instance.GetCurrentInstant()
                    });
                }
                else
                {
                    var produced = this.producer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, processResponseKey, new GenericProcessStatusResponse
                    {
                        DomainEvent = courtLocationEvent.EventType == "court-location-provision" ? "digitalevidence-court-location-provision-error" : "digitalevidence-court-location-decommission-error",
                        PartId = courtLocationEvent.Username,
                        Id = courtLocationEvent.RequestId,
                        Status = "Error",
                        ErrorList = new List<string> { result.Exception.Message },
                        EventTime = SystemClock.Instance.GetCurrentInstant()
                    });
                }
            }
            catch (Exception ex)
            {
                var produced = this.producer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, processResponseKey, new GenericProcessStatusResponse
                {
                    DomainEvent = courtLocationEvent.EventType == "court-location-provision" ? "digitalevidence-court-location-provision-error" : "digitalevidence-court-location-decommission-error",
                    PartId = courtLocationEvent.Username,
                    Id = courtLocationEvent.RequestId,
                    Status = "Error",
                    ErrorList = new List<string> { ex.Message },
                    EventTime = SystemClock.Instance.GetCurrentInstant()
                });
            }

            return Task.CompletedTask;
        }
    }
}
