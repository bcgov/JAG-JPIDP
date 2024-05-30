namespace MessagingAdapter.Models;

public class ConfigOptions
{
    public readonly string PollEventsKey = "PollEvents";

    public Dictionary<string, PollApiConfig> Options { get; set; } = [];
}

public class PollApiConfig
{
    public string Url { get; set; } = string.Empty;
    public string PollCron { get; set; } = string.Empty;
}
