namespace edt.service.ServiceEvents.CaseManagement.Handler;

using System.Threading.Tasks;
using edt.service.Data;
using edt.service.HttpClients.Services.EdtCore;
using edt.service.Kafka.Interfaces;
using edt.service.Kafka.Model;
using edt.service.ServiceEvents.CaseManagement.Models;

public class CaseAccessRequestHandler : IKafkaHandler<string, SubAgencyDomainEvent>
{

    private readonly EdtServiceConfiguration configuration;
    private readonly IEdtClient edtClient;
    private readonly ILogger logger;
    private readonly EdtDataStoreDbContext context;

    public Task<Task> HandleAsync(string consumerName, string key, SubAgencyDomainEvent value) => throw new NotImplementedException();
    public Task<Task> HandleRetryAsync(string consumerName, string key, SubAgencyDomainEvent value, int retryCount, string topicName) => throw new NotImplementedException();
}
