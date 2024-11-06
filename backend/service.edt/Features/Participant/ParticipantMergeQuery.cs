namespace edt.service.Features.Participant;

using System.Threading;
using System.Threading.Tasks;
using CommonModels.Models.Party;
using MediatR;
using Prometheus;

public record ParticipantByPartId(string PartId) : IRequest<ParticipantMergeListingModel>;

public class ParticipantMergeQuery(IParticipantLookupService participantLookupService) : IRequestHandler<ParticipantByPartId, ParticipantMergeListingModel>
{
    // time the lookup
    private static readonly Histogram ParticipantMergeLookupDuration = Metrics.CreateHistogram("edt_person_merge_search_duration", "Histogram of edt participant merge lookups.");

    public async Task<ParticipantMergeListingModel> Handle(ParticipantByPartId request, CancellationToken cancellationToken)
    {
        using (ParticipantMergeLookupDuration.NewTimer())
        {
            var mergeDetails = await participantLookupService.GetParticipantMergeDetails(request.PartId);

            return mergeDetails;
        }

    }
}
