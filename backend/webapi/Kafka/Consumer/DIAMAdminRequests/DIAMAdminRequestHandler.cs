namespace Pidp.Kafka.Consumer.DIAMAdminRequests;

using System.Diagnostics;
using System.Reflection;
using Common.Exceptions;
using CommonModels.Models.DIAMAdmin;
using NodaTime;
using Pidp.Data;
using Pidp.Infrastructure.Services;
using Pidp.Kafka.Interfaces;
using Serilog;

public class DIAMAdminRequestHandler : IKafkaHandler<AdminRequestKey, AdminRequestModel>

{
    private readonly PidpDbContext context;
    private readonly IClock clock;
    private readonly IKafkaProducer<string, AdminResponseModel> adminResponseProducer;
    private readonly PidpConfiguration config;
    private readonly IDIAMAdminService adminService;
    private readonly string AssemblyName = Assembly.GetEntryAssembly().GetName().Name;

    public DIAMAdminRequestHandler(PidpDbContext context, IClock clock,
        IKafkaProducer<string, AdminResponseModel> adminResponseProducer,
        PidpConfiguration config, IDIAMAdminService adminService)
    {
        this.context = context;
        this.clock = clock;
        this.adminResponseProducer = adminResponseProducer;
        this.config = config;
        this.adminService = adminService;
    }

    public async Task<Task> HandleAsync(string consumerName, AdminRequestKey header, AdminRequestModel value)
    {
        var started = Stopwatch.StartNew();
        Log.Logger.Information($"Admin request [{value}] message received on {consumerName} with key {header.Key}");
        if (header.TargetServices.Count > 0)
        {
            if (!header.TargetServices.Contains(this.AssemblyName))
            {
                Log.Logger.Information($"{header.Key} ignored as its not for me");
                return Task.CompletedTask;
            }
        }

        //check whether this message has been processed before
        if (await context.HasBeenProcessed(header.Key, consumerName))
        {
            return Task.CompletedTask;
        }

        // process the admin request
        Log.Information($"Processing admin request {value}");

        try
        {
            // the heavy lifting is in this adminService class
            var success = await adminService.ProcessAdminRequestAsync(value);
            if (success)
            {
                await this.PublishSuccessResponse(consumerName, Guid.Parse(header.Key), value.RequestData);
            }
            else
            {
                await this.PublishErrorResponse(consumerName, Guid.Parse(header.Key), value.RequestData, "Failed to process request");
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error processing admin request {value}");
            await this.PublishErrorResponse(consumerName, Guid.Parse(header.Key), value.RequestData, ex.Message);
        }

        return Task.CompletedTask;

    }

    private async Task<Task> PublishErrorResponse(string consumerName, Guid incomingMessageKey, Dictionary<string, string>? responseData, string error)
    {
        var responseKey = Guid.NewGuid();

        var errorData = new Dictionary<string, string>
        {
          { "error", error }
        };

        if (responseData != null)
        {
            foreach (var item in responseData)
            {
                errorData[item.Key] = item.Value;
            }
        }

        var producedReponse = await adminResponseProducer.ProduceAsync(config.KafkaCluster.DIAMAdminOutgoingTopic,
            responseKey.ToString(),
            new AdminResponseModel
            {
                RequestId = incomingMessageKey,
                RequestProcessDateTime = clock.GetCurrentInstant().ToDateTimeUtc(),
                Hostname = Environment.MachineName,
                ResponseData = errorData,
                Success = false
            });

        if (producedReponse.Status != Confluent.Kafka.PersistenceStatus.Persisted)
        {
            throw new DIAMGeneralException($"Failed to publish response {producedReponse} to {config.KafkaCluster.DIAMAdminOutgoingTopic}");
        }

        await context.IdempotentConsumer(messageId: incomingMessageKey.ToString(), consumer: consumerName);

        return Task.CompletedTask;
    }


    private async Task<Task> PublishSuccessResponse(string consumerName, Guid incomingMessageKey, Dictionary<string, string>? responseData)
    {

        var responseKey = Guid.NewGuid();
        var producedReponse = await adminResponseProducer.ProduceAsync(config.KafkaCluster.DIAMAdminOutgoingTopic,
            responseKey.ToString(),
            new AdminResponseModel
            {
                RequestId = incomingMessageKey,
                RequestProcessDateTime = clock.GetCurrentInstant().ToDateTimeUtc(),
                Hostname = Environment.MachineName,
                ResponseData = responseData ?? [],
                Success = true
            });

        if (producedReponse.Status != Confluent.Kafka.PersistenceStatus.Persisted)
        {
            throw new DIAMGeneralException($"Failed to publish response {producedReponse} to {config.KafkaCluster.DIAMAdminOutgoingTopic}");
        }

        await context.IdempotentConsumer(messageId: incomingMessageKey.ToString(), consumer: consumerName);

        return Task.CompletedTask;

    }
}
