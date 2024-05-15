namespace edt.notifications.Models;
using Newtonsoft.Json;


public abstract class EventModel
{
    public int Id { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public string AsJSON()
    {
        var json = JsonConvert.SerializeObject(this);
        return json;
    }
}
