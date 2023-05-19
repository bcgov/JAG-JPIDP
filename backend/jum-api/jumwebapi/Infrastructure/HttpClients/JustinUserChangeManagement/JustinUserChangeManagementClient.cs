namespace jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;

using System;
using System.Web.Http.Controllers;
using jumwebapi.Features.UserChangeManagement.Data;
using jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;
using jumwebapi.Models;
using NodaTime;
using Serilog;


public class JustinUserChangeManagementClient : BaseClient, IJustinUserChangeManagementClient
{
    public JustinUserChangeManagementClient(HttpClient httpClient, ILogger<JustinUserChangeManagementClient> logger) : base(httpClient, logger, PropertySerialization.SnakeCase) { }


    public async Task<IEnumerable<JustinUserChangeEvent>> GetCurrentChangeEvents()
    {
        Log.Information("Getting current JUSTIN user changes");


        var result = await this.PutAsyncForJUSTINNonesense<EventsResponse>($"newEventsBatch");

        if (result.IsSuccess)
        {
            Log.Information($"Got {result.Value.Events.Count} change events");

            // convert the change events to something useful
            return this.ConvertEventResponse(result.Value);
        }
        else
        {
            this.Logger.PollForChangesFailed(string.Join(",", result.Errors));
            return null;

        }

    }

    private IEnumerable<JustinUserChangeEvent> ConvertEventResponse(EventsResponse value)
    {
        var events = new List<JustinUserChangeEvent>();

        value.Events.ForEach(changeEvent =>
        {

            var partId = changeEvent.EventData.Find(data => data.DataElementNm == "PART_ID").DataValueTxt;

            if (partId != null)
            {
                var dateTime = DateTime.ParseExact(changeEvent.EventDtm, "yyyy-MM-dd HH:mm", null);

                var validPartId = decimal.TryParse(partId, out var decimalPartId);

                if (validPartId)
                {

                    var justinChangeEvent = new JustinUserChangeEvent
                    {
                        PartId = decimalPartId,
                        EventMessageId = changeEvent.EventMessageId,
                        EventTime = Instant.FromDateTimeOffset(dateTime),
                        EventType = changeEvent.MessageEventTypeCd
                    };
                    events.Add(justinChangeEvent);
                }
                else
                {
                    Log.Warning($"No valid partID found in event {partId}");
                }

            }
            else
            {
                this.Logger.MissingEventPartId(changeEvent.EventMessageId);
            }
        });

        return events;

    }

    public async Task<bool> RequeueChangeEvent(int eventId)
    {
        Log.Information($"Requeuing event {eventId}");

        var result = await this.PutAsyncForJUSTINNonesense<int>($"requeueEventById?id={eventId}");

        if (result.IsSuccess)
        {
            Log.Information($"JUSTIN change event {eventId} requeued");
            return true;
        }
        else
        {
            this.Logger.RequeueEventFailed(eventId);
            return false;
        }
    }

    public async Task<bool> FlagRequestComplete(int eventId, bool successful)
    {
        Log.Information($"Flagging event complete {eventId}");

        var successFlag = successful ? "T" : "F";
        var result = await this.PutAsyncForJUSTINNonesense<VoidResultConverter>($"eventStatus?event_message_id={eventId}&is_success={successFlag}");

        if ( result.IsSuccess)
        {
            Log.Information($"Event message id {eventId} marked success {successful}");
            return true;
        }
        else
        {
            Log.Error($"Failed to mark event message id {eventId} success {successful} [{string.Join(",",result.Errors)}]");
            return false;
        }

    }
}

public static partial class JustinUserChangeManagementClientLoggingExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Failed to requeue event {eventId}.")]
    public static partial void RequeueEventFailed(this Microsoft.Extensions.Logging.ILogger logger, int eventId);
    [LoggerMessage(2, LogLevel.Warning, "Failed to poll JUSTIN for changes [{errors}]")]
    public static partial void PollForChangesFailed(this Microsoft.Extensions.Logging.ILogger logger, string errors);
    [LoggerMessage(3, LogLevel.Warning, "Missing partId for [{messageId}]")]
    public static partial void MissingEventPartId(this Microsoft.Extensions.Logging.ILogger logger, int messageId);
}

