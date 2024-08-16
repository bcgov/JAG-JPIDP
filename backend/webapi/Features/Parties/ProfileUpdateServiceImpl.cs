namespace Pidp.Features.Parties;

using Common.Kafka;
using Common.Models;
using Common.Models.EDT;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pidp.Data;
using Pidp.Helpers.Serializers;
using Pidp.Infrastructure.HttpClients.Keycloak;
using Pidp.Models;

/// <summary>
/// Handle profile updates
/// </summary>
public class ProfileUpdateServiceImpl : IProfileUpdateService
{

    private readonly IKeycloakAdministrationClient keycloakAdministrationClient;
    private readonly IKafkaProducer<string, UserChangeModel> kafkaProducer;
    private readonly PidpDbContext dbContext;
    private readonly PidpConfiguration configuration;

    public ProfileUpdateServiceImpl(IKeycloakAdministrationClient keycloakAdministrationClient, IKafkaProducer<string, UserChangeModel> kafkaProducer, PidpDbContext dbContext, PidpConfiguration configuration)
    {
        this.keycloakAdministrationClient = keycloakAdministrationClient;
        this.kafkaProducer = kafkaProducer;
        this.dbContext = dbContext;
        this.configuration = configuration;
    }

    public async Task<bool> UpdateUserProfile(UpdatePersonContactInfoModel updatePerson)
    {
        var changesDetected = false;
        var response = false;
        // we'll handle updating keycloak and then publish a message for any other service to consume
        var party = this.dbContext.Parties.Include(req => req.AccessRequests).FirstOrDefault(party => party.Id == updatePerson.PartyId);
        var changes = new Dictionary<ChangeType, SingleChangeType>();
        if (party == null)
        {
            Serilog.Log.Error($"Request up updated unknown user {updatePerson.PartyId}");
            return response;
        }

        // get the user from keycloak
        var keycloakUserInfo = await this.keycloakAdministrationClient.GetUser(party.UserId);
        if (keycloakUserInfo == null)
        {
            Serilog.Log.Error($"Keycloak user not found for {updatePerson.PartyId} {party.UserId} - likely deleted from Keycloak");
            return response;
        }



        if (!string.IsNullOrEmpty(updatePerson.EMail) && keycloakUserInfo.Email != updatePerson.EMail)
        {
            Serilog.Log.Information($"Updating {party.UserId} email to [{updatePerson.EMail}] from [{keycloakUserInfo.Email}]");
            changes.Add(ChangeType.EMAIL, new SingleChangeType(keycloakUserInfo.Email != null ? keycloakUserInfo.Email : "", updatePerson.EMail));
            keycloakUserInfo.Email = updatePerson.EMail;
            changesDetected = true;
        }

        if (keycloakUserInfo.Attributes.ContainsKey("phone") && keycloakUserInfo.Attributes["phone"].Length > 0)
        {
            Serilog.Log.Information($"Updating {party.UserId} phone to [{updatePerson.Phone}] from [{keycloakUserInfo.Attributes["phone"][0]}]");
            var fromPhone = keycloakUserInfo.Attributes.ContainsKey("phone") ? keycloakUserInfo.Attributes["phone"][0] : "";
            if (fromPhone != updatePerson.Phone && !string.IsNullOrEmpty(updatePerson.Phone))
            {
                keycloakUserInfo.Attributes["phone"][0] = updatePerson.Phone;
                changes.Add(ChangeType.PHONE, new SingleChangeType(fromPhone, updatePerson.Phone));
                changesDetected = true;
            }

        }
        else if (!string.IsNullOrEmpty(updatePerson.Phone))
        {
            Serilog.Log.Information($"Updating {party.UserId} adding phone to [{updatePerson.Phone}]");
            keycloakUserInfo.Attributes["phone"] = new string[] { updatePerson.Phone };
            changes.Add(ChangeType.PHONE, new SingleChangeType("", updatePerson.Phone));
            changesDetected = true;
        }

        if (changesDetected)
        {
            Serilog.Log.Information($"Updating keycloak info for user {party.UserId}");
            var updated = await this.keycloakAdministrationClient.UpdateUser(party.UserId, keycloakUserInfo);
            response = updated;

            if (party.AccessRequests.Any())
            {
                var msgKey = Guid.NewGuid().ToString();
                // store user account change
                var changeInfo = this.dbContext.UserAccountChanges.Add(new UserAccountChange
                {
                    Party = party,
                    ChangeData = JsonConvert.SerializeObject(updatePerson, new JsonSerializerSettings
                    {
                        ContractResolver = ShouldSerializeContractResolver.Instance
                    }),
                    Reason = "User initiated change",
                    TraceId = msgKey,
                    Status = "Pending"
                });

                await this.dbContext.SaveChangesAsync();

                var userChangeModel = new UserChangeModel
                {
                    Key = updatePerson.Key,
                    ChangeDateTime = DateTime.UtcNow,
                    UserID = updatePerson.KeycloakUserId,
                    SingleChangeTypes = changes,
                    IdpType = updatePerson.Idp,
                    ChangeId = changeInfo.Entity.Id
                };




                if (updated)
                {
                    Serilog.Log.Information($"Successfully updated user {party.LastName} {party.Id} {party.UserId}");

                    // publish a message to the disclosure update topic
                    var producedToDisclosure = await this.kafkaProducer.ProduceAsync(this.configuration.KafkaCluster.DisclosureUserModificationTopic, msgKey, userChangeModel);
                    Serilog.Log.Information($"Published msg key {msgKey} to {this.configuration.KafkaCluster.DisclosureUserModificationTopic}");

                    var producedToCore = await this.kafkaProducer.ProduceAsync(this.configuration.KafkaCluster.UserAccountChangeTopicName, msgKey, userChangeModel);
                    Serilog.Log.Information($"Published msg key {msgKey} to {this.configuration.KafkaCluster.UserAccountChangeTopicName}");

                }
                else
                {
                    Serilog.Log.Warning($"Problem occurred updating keycloak user {party.UserId} - check keycloak logs");
                }
            }
        }

        return response;
    }
}
