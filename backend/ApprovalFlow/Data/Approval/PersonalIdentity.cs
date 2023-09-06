namespace ApprovalFlow.Data.Approval;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIAM.Common.Models;

[Table(nameof(PersonalIdentity))]
public class PersonalIdentity : BaseAuditable
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Source { get; set; } = string.Empty;
    [Required]
    public int ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; } // navigation property
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string EMail { get; set; } = string.Empty;
}
