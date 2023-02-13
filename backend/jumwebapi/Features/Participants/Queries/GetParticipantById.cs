﻿using jumwebapi.Features.Participants.Models;
using jumwebapi.Infrastructure.HttpClients.JustinParticipant;
using MediatR;

namespace jumwebapi.Features.Participants.Queries;
public record GetParticipantByIdQuery(decimal Id) : IRequest<Participant>;
public class GetParticipantById : IRequestHandler<GetParticipantByIdQuery, Participant>
{
    private readonly IJustinParticipantClient _justineParticipantClient;
    public GetParticipantById(IJustinParticipantClient justinParticipantClient)
    {
        _justineParticipantClient = justinParticipantClient;
    }
    public async Task<Participant> Handle(GetParticipantByIdQuery request, CancellationToken cancellationToken)
    {
        return await _justineParticipantClient.GetParticipantPartId(request.Id, "");
    }
}

