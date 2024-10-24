namespace CommonModels.Models.Party;
using System;

public class PartyDetailModel
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? Jpdid { get; set; }
    public DateOnly? Birthdate { get; set; }
    public string? Gender { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredFirstName { get; set; }
    public string? PreferredMiddleName { get; set; }
    public string? PreferredLastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
