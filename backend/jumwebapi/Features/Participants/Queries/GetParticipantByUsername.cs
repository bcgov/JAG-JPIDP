using jumwebapi.Features.Participants.Models;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Prometheus;

namespace jumwebapi.Features.Participants.Queries;

public record GetParticipantByUsernameQuery(object Username) : IRequest<Participant>;
public class GetParticipantByUsername : IRequestHandler<GetParticipantByUsernameQuery, Participant>
{
    private readonly IJustinParticipantClient _justineParticipantClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly Histogram LookupDuration = Metrics
    .CreateHistogram("jum_webapi_justin_lookup_duration_seconds", "Historgram of JUSTIN lookup timings");

    public GetParticipantByUsername(IJustinParticipantClient justineParticipantClient, IHttpContextAccessor httpContextAccessor)
    {
        _justineParticipantClient = justineParticipantClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Participant> Handle(GetParticipantByUsernameQuery request, CancellationToken cancellationToken)
    {
        //var accessToken = await _httpContextAccessor.HttpContext?.GetTokenAsync("access_token");//current part endpoint dont have authrotization
        using (LookupDuration.NewTimer())
        {
            return await _justineParticipantClient.GetParticipantByUserName(request?.Username.ToString(), "");
        }
    }
}
