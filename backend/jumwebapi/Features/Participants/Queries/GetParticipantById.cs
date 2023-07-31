using jumwebapi.Features.Participants.Models;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using MediatR;
using Prometheus;

namespace jumwebapi.Features.Participants.Queries;
public record GetParticipantByIdQuery(decimal Id) : IRequest<Participant>;
public class GetParticipantById : IRequestHandler<GetParticipantByIdQuery, Participant>
{
    private readonly IJustinParticipantClient _justinParticipantClient;
    private static readonly Counter JumRequests = Metrics
        .CreateCounter("jum_search_by_id_count", "Number of jum user searches by id");
    private static readonly Histogram JumRequestDuration = Metrics
        .CreateHistogram("jum_search_by_id_duration", "Histogram of jum searches by id.");


    public GetParticipantById(IJustinParticipantClient justinParticipantClient)
    {
        _justinParticipantClient = justinParticipantClient;
    }
    public async Task<Participant> Handle(GetParticipantByIdQuery request, CancellationToken cancellationToken)
    {
        using (JumRequestDuration.NewTimer())
        {
            JumRequests.Inc();
            return await _justinParticipantClient.GetParticipantPartId(request.Id, "");
        }
    }
}

