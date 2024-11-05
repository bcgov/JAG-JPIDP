namespace JAMService.Infrastructure.HttpClients.JustinParticipant;

using CommonModels.Models.JUSTIN;

public interface IJustinParticipantRoleClient
{
    Task<DbRoles> GetParticipantRolesByApplicationNameAndUPN(string application, string UPN);
    Task<DbRoles> GetParticipantRolesByApplicationNameAndParticipantId(string application, double participantId);

}
