namespace jumwebapi.Features.UserChangeManagement.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using global::Common.Kafka;
using jumwebapi.Core.Extension;
using jumwebapi.Data;
using jumwebapi.Features.Participants.Queries;
using jumwebapi.Features.UserChangeManagement.Data;
using jumwebapi.Infrastructure.Auth;
using jumwebapi.Models;
using MediatR;
using NodaTime;
using Prometheus;
using Serilog;

public class JustinUserChangeService : IJustinUserChangeService
{
    private readonly JumDbContext context;
    private readonly IClock clock;
    private readonly IKafkaProducer<string, JustinUserChangeEvent> changeEventProducer;
    private readonly JumWebApiConfiguration config;
    private IMediator mediator;
    private static readonly Counter UserUpdateCounter = Metrics.CreateCounter("justin_user_update_total", "Number of user updates from JUSTIN");


    public JustinUserChangeService() { }

    public JustinUserChangeService(JumDbContext context, IClock clock, IKafkaProducer<string, JustinUserChangeEvent> changeEventProducer, JumWebApiConfiguration config, IMediator mediator)
    {
        this.context = context;
        this.clock = clock;
        this.changeEventProducer = changeEventProducer;
        this.config = config;
        this.mediator = mediator;
    }

    public async Task<Task> ProcessChangeEvents(IEnumerable<JustinUserChangeEvent> events)
    {

        // store all events in the Db
        Log.Information($"Processing received change events");

        // we'll treat each request as a single transaction

        events.ForEach(async userChangeEvent =>
        {

            // see if we've already processed this change event
            var existing = this.context.JustinUserChange.Any(changeEvent => changeEvent.EventMessageId == userChangeEvent.EventMessageId);

            if (existing)
            {
                Log.Warning($"Change event {userChangeEvent.EventMessageId} has already been received");
            }
            else
            {

                using (var txn = this.context.Database.BeginTransaction())
                {
                    try
                    {
                        Log.Information($"Processing change request id {userChangeEvent.EventMessageId} for event {userChangeEvent.EventType} and user {userChangeEvent.PartId}");

                        // track metric
                        UserUpdateCounter.Inc();

                        // get the latest user info from JUSTIN
                        var participant = await this.mediator.Send(new GetParticipantByIdQuery(userChangeEvent.PartId));

                        if (participant == null)
                        {
                            Log.Warning($"Failed to find JUSTIN user with partId {userChangeEvent.PartId} request {userChangeEvent.EventMessageId} will be ignored");
                        }
                        else
                        {

                            // get the current JUSTIN status
                            var details = participant.participantDetails.First();

                            if (details != null)
                            {

                                // store the entry in the Db
                                var dbChangeEvent = new JustinUserChange
                                {
                                    Created = this.clock.GetCurrentInstant(),
                                    EventMessageId = userChangeEvent.EventMessageId,
                                    PartId = "" + userChangeEvent.PartId,
                                    EventType = userChangeEvent.EventType,
                                    EventTime = userChangeEvent.EventTime,
                                };

                                var edtUpdate = new JustinUserChangeTarget
                                {
                                    JustinUserChange = dbChangeEvent,
                                    ServiceName = "EDT",
                                    ChangeStatus = "created",
                                };

                                this.context.JustinUserChange.Add(dbChangeEvent);
                                var response = await this.context.SaveChangesAsync();
                                Log.Information($"New change event {response} created");

                                var topicEntryKey = Guid.NewGuid().ToString();

                                // publish the event to a topic
                                await this.changeEventProducer.ProduceAsync(this.config.KafkaCluster.UserChangeEventTopic, topicEntryKey, userChangeEvent);


                            }

                            await txn.CommitAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to store user change event {userChangeEvent.EventMessageId} [{string.Join(",", ex.Message)}]");

                        // requeue the event

                    }
                }
            }
        });



        // publish change events to topic

        return Task.CompletedTask;


    }
}
