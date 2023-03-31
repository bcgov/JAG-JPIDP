namespace Pidp.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table(nameof(AgencyRequestAttachment))]
public class AgencyRequestAttachment : BaseAuditable
{
    [Key]
    public int AttachmentId { get; set; }
    [Required]
    public string AttachmentName { get; set; } = string.Empty;
    [Required]
    public string AttachmentType { get; set; } = string.Empty;
    public string UploadStatus { get; set; } = AgencyRequestStatus.Pending;
    public SubmittingAgencyRequest SubmittingAgencyRequest { get; set; } = new SubmittingAgencyRequest();
}
