namespace Common.Models.AccessRequests;
using System;


public class AccessRequestDTO
{
    public int Id { get; set; }
    public string? Requester { get; set; }
    public DateTime RequestDate { get; set; }
    public string? Status { get; set; }
    public string? RequestType { get; set; }
    public Dictionary<string, string> RequestData { get; set; } = [];
}
