namespace jumwebapi.Infrastructure.HttpClients.JustinUserChangeManagement;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class JustinChangeEvent
{
    [JsonPropertyName("event_message_id")]

    public int EventMessageId { get; set; }
    [JsonPropertyName("appl_application_cd")]
    public string ApplApplicationCd { get; set; }
    [JsonPropertyName("message_event_type_cd")]
    public string MessageEventTypeCd { get; set; }
    [JsonPropertyName("event_dtm")]
    public string EventDtm { get; set; }
    [JsonPropertyName("event_data")]
    public List<EventData> EventData { get; set; }
}

public class EventData
{
    [JsonPropertyName("data_element_nm")]
    public string DataElementNm { get; set; }
    [JsonPropertyName("data_value_txt")]
    public string DataValueTxt { get; set; }
}

public class EventsResponse
{
    [JsonPropertyName("events")]
    public List<JustinChangeEvent> Events { get; set; }
}
