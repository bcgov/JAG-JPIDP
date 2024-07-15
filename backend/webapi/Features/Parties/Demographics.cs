namespace Pidp.Features.Parties;

using System.ComponentModel.DataAnnotations;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Models;
using FluentValidation;
using HybridModelBinding;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pidp.Data;
using Pidp.Helpers.Serializers;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Kafka.Interfaces;
using Pidp.Models;

public class Demographics
{
    public class Query : IQuery<Command>
    {
        [Required]
        public int Id { get; set; }
    }

    public class Command : ICommand
    {
        [JsonIgnore]
        [HybridBindProperty(Source.Route)]
        public int Id { get; set; }

        public string? PreferredFirstName { get; set; }
        public string? PreferredMiddleName { get; set; }
        public string? PreferredLastName { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator() => this.RuleFor(x => x.Id).GreaterThan(0);
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            this.RuleFor(x => x.Id).GreaterThan(0);
            this.RuleFor(x => x.Email).NotEmpty().EmailAddress(); // TODO Custom email validation?
                                                                  // this.RuleFor(x => x.Phone).NotEmpty();
        }
    }

    public class QueryHandler : IQueryHandler<Query, Command>
    {
        private readonly IMapper mapper;
        private readonly PidpDbContext context;

        public QueryHandler(IMapper mapper, PidpDbContext context)
        {
            this.mapper = mapper;
            this.context = context;
        }

        public async Task<Command> HandleAsync(Query query)
        {
            return await this.context.Parties
                .Where(party => party.Id == query.Id)
                .ProjectTo<Command>(this.mapper.ConfigurationProvider)
                .SingleAsync();
        }
    }

    public class CommandHandler : ICommandHandler<Command>
    {
        private readonly PidpDbContext context;
        private readonly PidpConfiguration configuration;
        private readonly IKeycloakAdministrationClient administrationClient;
        private readonly IKafkaProducer<string, UserChangeModel> producer;


        public CommandHandler(PidpDbContext context, IKafkaProducer<string, UserChangeModel> producer, PidpConfiguration configuration, IKeycloakAdministrationClient administrationClient)
        {
            this.context = context;
            this.administrationClient = administrationClient;
            this.producer = producer;
            this.configuration = configuration;

        }

        public async Task HandleAsync(Command command)
        {
            var party = await this.context.Parties.Include(party => party.OrgainizationDetail).Include(org => org.OrgainizationDetail.Organization)
                .AsSplitQuery().SingleAsync(party => party.Id == command.Id);

            var currentEmail = party.Email ?? "";

            party.PreferredFirstName = command.PreferredFirstName;
            party.PreferredMiddleName = command.PreferredMiddleName;
            party.PreferredLastName = command.PreferredLastName;
            party.Email = command.Email;
            party.Phone = command.Phone;

            await this.context.SaveChangesAsync();

            // if user has access requests then we'll send a status update too
            var accessRequests = this.context.AccessRequests.Where(req => req.Party == party).Any();

            if (currentEmail != command.Email)
            {
                Serilog.Log.Information($"Updating {party.Id} email to {command.Email} from {currentEmail}");
                var messageId = Guid.NewGuid().ToString();

                var userInfo = await this.administrationClient.GetUser(Common.Constants.Auth.RealmConstants.BCPSRealm, party.UserId);
                if (userInfo != null)
                {
                    var changeModel = new UserChangeModel
                    {
                        ChangeDateTime = DateTime.Now,
                        Key = party.Jpdid,
                        Organization = party.OrgainizationDetail?.Organization.Name,
                        IdpType = party.OrgainizationDetail.Organization.IdpHint,
                        UserID = party.UserId.ToString()

                    };

                    changeModel.SingleChangeTypes.Add(ChangeType.EMAIL, new SingleChangeType(currentEmail, command.Email));

                    // log change
                    var changeEntry = this.context.UserAccountChanges.Add(new UserAccountChange
                    {
                        Party = party,
                        ChangeData = JsonConvert.SerializeObject(changeModel, new JsonSerializerSettings
                        {
                            ContractResolver = ShouldSerializeContractResolver.Instance
                        }),
                        Reason = "User initiated change",
                        TraceId = messageId,
                        Status = "Pending"

                    });

                    await this.context.SaveChangesAsync();
                    changeModel.ChangeId = changeEntry.Entity.Id;
                    userInfo.Email = command.Email;
                    await this.administrationClient.UpdateUser(Common.Constants.Auth.RealmConstants.BCPSRealm, party.UserId, userInfo);

                    if (accessRequests)
                    {
                        // publish change event
                        var publishCoreResponse = await this.producer.ProduceAsync(this.configuration.KafkaCluster.UserAccountChangeTopicName, messageId, changeModel);
                        if (publishCoreResponse.Status == Confluent.Kafka.PersistenceStatus.NotPersisted)
                        {
                            Serilog.Log.Warning("Failed to send update request for user modification to core");
                        }
                        var publishDisclosureResponse = await this.producer.ProduceAsync(this.configuration.KafkaCluster.DisclosureUserModificationTopic, messageId, changeModel);
                        if (publishDisclosureResponse.Status == Confluent.Kafka.PersistenceStatus.NotPersisted)
                        {
                            Serilog.Log.Warning("Failed to send update request for user modification to disclosure");
                        }
                    }
                }
                else
                {
                    Serilog.Log.Error($"No user was found in Keycloak for UID {party.UserId} ID: {party.Id}");
                }
            }
        }
    }
}
