namespace jumwebapi.Features.UserChangeManagement.Commands;

using System.Threading;
using System.Threading.Tasks;
using jumwebapi.Data;
using jumwebapi.Features.UserChangeManagement.Models;
using jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;
using MediatR;
using NodaTime;

public sealed record UpdateJustinUserUpdateStatusCommand(JustinProcessStatusModel processModel) : IRequest<bool>;

public class UpdateJustinUserUpdateStatusCommandHandler : IRequestHandler<UpdateJustinUserUpdateStatusCommand, bool>
{

    private readonly IJustinUserChangeManagementClient justinClient;
    private readonly JumDbContext context;

    public UpdateJustinUserUpdateStatusCommandHandler(IJustinUserChangeManagementClient justinClient, JumDbContext context)
    {
        this.justinClient = justinClient;
        this.context = context;
    }

    public async Task<bool> Handle(UpdateJustinUserUpdateStatusCommand request, CancellationToken cancellationToken)
    {
        Serilog.Log.Information($"Flagging request as complete {request.processModel.EventMessageId}");

        var successful = await this.justinClient.FlagRequestComplete(request.processModel.EventMessageId, request.processModel.IsSuccess);

        if (successful)
        {
            var justinUserChange = this.context.JustinUserChange.Where(message => message.EventMessageId == request.processModel.EventMessageId).FirstOrDefault();
            if (justinUserChange != null)
            {
                justinUserChange.Completed = SystemClock.Instance.GetCurrentInstant();
                await this.context.SaveChangesAsync(cancellationToken);
            }
        }

        return successful;
    }
}
