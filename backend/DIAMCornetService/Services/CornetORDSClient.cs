namespace DIAMCornetService.Services;

using System.Threading.Tasks;
using DIAMCornetService.Exceptions;

public class CornetORDSClient(HttpClient httpClient, ILogger<CornetORDSClient> logger) : BaseClient(httpClient, logger), ICornetORDSClient
{
    public async Task<string> GetCSNumberForParticipant(string participantId)
    {

        var response = await GetAsync<string>($"api/v1/Participant/{participantId}/CSNumber");


        if (response.IsSuccess)
        {
            return response.Value;
        }
        else
        {
            throw new CornetException($"Failed to get CS Number for participant {participantId}");
        }
    }
}
