namespace Common.Models.Authorization;
using System;

public class TokenModel
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public string Subject { get; set; } = string.Empty;
}
