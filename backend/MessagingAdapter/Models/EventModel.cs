namespace MessagingAdapter.Models;
using Newtonsoft.Json;


public abstract class EventModel
{
    public int Id { get; set; } = -1;
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public List<NameValuePair> Fields { get; set; } = [];

    public string AsJSON()
    {
        var json = JsonConvert.SerializeObject(this);
        return json;
    }
}

public class NameValuePair
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
