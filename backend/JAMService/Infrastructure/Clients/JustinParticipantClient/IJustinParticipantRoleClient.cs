namespace JAMService.Infrastructure.HttpClients.JustinParticipant;


public interface IJustinParticipantRoleClient
{
    Task<List<string>> GetParticipantRolesByApplicationNameAndUPN(string application, string UPN);
    Task<List<string>> GetParticipantRolesByApplicationNameAndParticipantId(string application, double participantId);

}
