using AutoMapper;
using Common.Models;
using edt.disclosure;
using edt.disclosure.Data;
using edt.disclosure.Exceptions;
using edt.disclosure.HttpClients.Services.EdtDisclosure;
using edt.disclosure.Kafka.Interfaces;
using edt.disclosure.Models;
using NodaTime;

public class UserChangeHandler : IKafkaHandler<string, UserChangeModel>
{
    private readonly EdtDisclosureServiceConfiguration configuration;
    private readonly IEdtDisclosureClient edtClient;
    private readonly IMapper mapper;

    private readonly IClock clock;
    private readonly ILogger logger;
    private readonly DisclosureDataStoreDbContext context;
    private readonly IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer;

    public UserChangeHandler(
    EdtDisclosureServiceConfiguration configuration,
    IEdtDisclosureClient edtClient,
    IClock clock,
    IMapper mapper,
    ILogger logger,
    IKafkaProducer<string, GenericProcessStatusResponse> processResponseProducer,
    DisclosureDataStoreDbContext context)
    {

        this.configuration = configuration;
        this.context = context;
        this.clock = clock;
        this.mapper = mapper;
        this.logger = logger;
        this.edtClient = edtClient;
        this.processResponseProducer = processResponseProducer;

    }

    public async Task<Task> HandleAsync(string consumerName, string key, UserChangeModel userChangeEvent)
    {
        using var trx = this.context.Database.BeginTransaction();
        try
        {
            //check whether this message has been processed before   
            if (await this.context.HasBeenProcessed(key, consumerName))
            {
                await trx.RollbackAsync();
                return Task.CompletedTask;
            }


            var response = await this.edtClient.UpdateUser(userChangeEvent);
            var responseID = Guid.NewGuid().ToString();
            var processResponse = new GenericProcessStatusResponse
            {
                DomainEvent = response.successful ? "digitalevidencedisclosure-usermodification-complete" : "digitalevidencedisclosure-usermodification-error",
                EventTime = this.clock.GetCurrentInstant(),
                PartId = userChangeEvent.UserID,
                Status = response.successful ? "Complete" : "Error",
                ErrorList = response.Errors,
                TraceId = responseID
            };

            var publishResult = await this.processResponseProducer.ProduceAsync(this.configuration.KafkaCluster.ProcessResponseTopic, responseID, processResponse);
            if (publishResult.Status == Confluent.Kafka.PersistenceStatus.Persisted)
            {
                Serilog.Log.Information($"Process response {responseID} for update request {userChangeEvent.ChangeId}");
                return Task.CompletedTask;
            }
            else
            {
                Serilog.Log.Error($"Failed to create response {responseID} for update request {userChangeEvent.ChangeId}");
                return Task.FromException(new EdtDisclosureServiceException($"Failed to create response {responseID} for update request {userChangeEvent.ChangeId}"));
            }
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            return Task.FromException(ex);

        }
    }
}
