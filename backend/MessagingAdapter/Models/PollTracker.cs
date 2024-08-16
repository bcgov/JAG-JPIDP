namespace MessagingAdapter.Models;

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Represents a poll event with filter values for Id and Time
/// </summary>
public class PollTracker
{
    [Key]
    public int Id { get; set; }
    public DateTime LastPollTime { get; set; }
    public int LastPollMaxId { get; set; }
    public string AdditionalFilters { get; set; } = string.Empty;
    public string PollErrors { get; set; } = string.Empty;
    [JsonConverter(typeof(StringEnumConverter))]
    public TargetPoll Target { get; set; } = TargetPoll.EdtDisclosureEvent;
}

public enum TargetPoll
{
    EdtDisclosureEvent
}
