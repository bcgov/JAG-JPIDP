namespace edt.disclosure.ServiceEvents.CourtLocation.Handler;

using edt.disclosure.Data;
using edt.disclosure.Exceptions;
using edt.disclosure.HttpClients.Keycloak;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Models;
using edt.disclosure.ServiceEvents.CourtLocation.Models;
using NodaTime;
using Prometheus;



/// <summary>
/// Handle requests to access court location case folders
/// </summary>
public class CourtLocationAccessRequestHandler(
EdtDisclosureServiceConfiguration configuration,
IKeycloakAdministrationClient keycloakAdministrationClient,
IKafkaProducer<string, GenericProcessStatusResponse> producer,
DisclosureDataStoreDbContext context,
IClock clock,
IEdtDisclosureClient edtClient,
 ILogger<CourtLocationAccessRequestHandler> logger) : IKafkaHandler<string, CourtLocationDomainEvent>
{
    private readonly EdtDisclosureServiceConfiguration configuration = configuration;
    private readonly IEdtDisclosureClient edtClient = edtClient;
    private readonly IKeycloakAdministrationClient keycloakAdministrationClient = keycloakAdministrationClient;
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> producer = producer;
    private readonly DisclosureDataStoreDbContext context = context;
    private static readonly Histogram CourtLocationRequestDuration = Metrics.CreateHistogram("court_location_request_duration", "Histogram of court location request call durations.");
    private readonly IClock clock = clock;

    public async Task<Task> HandleAsync(string consumerName, string key, CourtLocationDomainEvent courtLocationEvent)
    {

        using var trx = this.context.Database.BeginTransaction();

        //check whether this message has been processed before   
        if (await this.context.HasBeenProcessed(key, consumerName))
        {
            await trx.RollbackAsync();
            return Task.CompletedTask;
        }

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

                Serilog.Log.Information($"Publishing court location response for {courtLocationEvent.RequestId} {courtLocationEvent.Username} success: {result.IsCompletedSuccessfully}");

                if (result.IsCompletedSuccessfully)
                {
                    var produced = await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, processResponseKey, new GenericProcessStatusResponse
                    {
                        DomainEvent = courtLocationEvent.EventType == "court-location-provision" ? "digitalevidence-court-location-provision-complete" : "digitalevidence-court-location-decommission-complete",
                        PartId = courtLocationEvent.Username,
                        Id = courtLocationEvent.RequestId,
                        Status = courtLocationEvent.EventType == "court-location-provision" ? "Complete" : "Deleted",
                        EventTime = SystemClock.Instance.GetCurrentInstant()
                    });

                    if (produced.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                    {
                        Serilog.Log.Information($"{processResponseKey} msg published to {this.configuration.KafkaCluster.ProcessResponseTopic}");
                    }
                    else
                    {
                        Serilog.Log.Error($"{processResponseKey} msg publish failed to {this.configuration.KafkaCluster.ProcessResponseTopic}");
                    }

                }
                else
                {
                    var produced = await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, processResponseKey, new GenericProcessStatusResponse
                    {
                        DomainEvent = courtLocationEvent.EventType == "court-location-provision" ? "digitalevidence-court-location-provision-error" : "digitalevidence-court-location-decommission-error",
                        PartId = courtLocationEvent.Username,
                        Id = courtLocationEvent.RequestId,
                        Status = "Error",
                        ErrorList = [result.Exception.Message],
                        EventTime = SystemClock.Instance.GetCurrentInstant()
                    });

                    if (produced.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                    {
                        Serilog.Log.Information($"{processResponseKey} msg published to {this.configuration.KafkaCluster.ProcessResponseTopic}");
                    }
                    else
                    {
                        Serilog.Log.Error($"{processResponseKey} msg publish failed to {this.configuration.KafkaCluster.ProcessResponseTopic}");
                    }
                }
            }
            catch (Exception ex)
            {
                var produced = await this.producer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, processResponseKey, new GenericProcessStatusResponse
                {
                    DomainEvent = courtLocationEvent.EventType == "court-location-provision" ? "digitalevidence-court-location-provision-error" : "digitalevidence-court-location-decommission-error",
                    PartId = courtLocationEvent.Username,
                    Id = courtLocationEvent.RequestId,
                    Status = "Error",
                    ErrorList = [ex.Message],
                    EventTime = SystemClock.Instance.GetCurrentInstant()
                });

                if (produced.Status == Confluent.Kafka.PersistenceStatus.Persisted)
                {
                    Serilog.Log.Information($"{processResponseKey} msg published to {this.configuration.KafkaCluster.ProcessResponseTopic}");
                }
                else
                {
                    Serilog.Log.Error($"{processResponseKey} msg publish failed to {this.configuration.KafkaCluster.ProcessResponseTopic}");
                }
            }

            //add to tell message has been processed by consumer
            await this.context.IdempotentConsumer(messageId: key, consumer: consumerName, consumeDate: this.clock.GetCurrentInstant());
            await this.context.SaveChangesAsync();
            await trx.CommitAsync();

            return Task.CompletedTask;
        }
    }
}
