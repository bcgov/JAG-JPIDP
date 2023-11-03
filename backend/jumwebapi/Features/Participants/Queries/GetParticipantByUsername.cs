using System.Diagnostics.Metrics;
using jumwebapi.Features.Participants.Models;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Prometheus;

namespace jumwebapi.Features.Participants.Queries;

public record GetParticipantByUsernameQuery(object Username) : IRequest<Participant>;
public class GetParticipantByUsername : IRequestHandler<GetParticipantByUsernameQuery, Participant>
{
    private readonly IJustinParticipantClient _justinParticipantClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly Counter JumRequests = Metrics
    .CreateCounter("jum_search_by_name_count_total", "Number of jum user searches by name"); // prometheus 8.0.1 breaks if counter doesnt end in total in some cases
    private static readonly Histogram JumRequestDuration = Metrics
    .CreateHistogram("jum_search_by_name_duration", "Histogram of jum searches by name.");

    public GetParticipantByUsername(IJustinParticipantClient justinParticipantClient, IHttpContextAccessor httpContextAccessor)
    {
        _justinParticipantClient = justinParticipantClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Participant> Handle(GetParticipantByUsernameQuery request, CancellationToken cancellationToken)
    {
        using (JumRequestDuration.NewTimer())
        {
            JumRequests.Inc();
            //var accessToken = await _httpContextAccessor.HttpContext?.GetTokenAsync("access_token");//current part endpoint dont have authrotization
            return await _justinParticipantClient.GetParticipantByUserName(request?.Username.ToString(), "");
        }
    }
}
