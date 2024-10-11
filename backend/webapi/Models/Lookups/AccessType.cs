namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum AccessTypeCode
{
    // needs to move to config
    [Display(Name = "Special Authority eForms")]
    SAEforms = 1,
    [Display(Name = "Obsolete")]
    HcimAccountTransfer,
    [Display(Name = "Obsolete")]
    HcimEnrolment,
    [Display(Name = "Driver Fitness")]
    DriverFitness,
    [Display(Name = "Digital Evidence (DEMS)")]
    DigitalEvidence,
    [Display(Name = "Digital Evidence Case Management")]
    DigitalEvidenceCaseManagement,
    [Display(Name = "Digital Evidence Disclosure")]
    DigitalEvidenceDisclosure,   // defence/duty will be User in Disclosure
    [Display(Name = "Digital Evidence Defence")]
    DigitalEvidenceDefence,      // defence/duty will be Participant in Core
    [Display(Name = "Obsolete")]
    Uci,
    [Display(Name = "Obsolete")]
    MSTeams,
    [Display(Name = "JAM_POR")]
    JAMPOR,
    [Display(Name = "JAM_RCC")]
    JAMRCC,
    [Display(Name = "JAM_LEA")]
    JAMLEA
}

[Table("AccessTypeLookup")]
public class AccessType
{
    [Key]
    public AccessTypeCode Code { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class AccessTypeDataGenerator : ILookupDataGenerator<AccessType>
{
    public IEnumerable<AccessType> Generate() => new[]
    {
        new AccessType { Code = AccessTypeCode.SAEforms,                            Name = "Special Authority eForms"},
        new AccessType { Code = AccessTypeCode.HcimAccountTransfer,                 Name = "HCIMWeb Account Transfer"},
        new AccessType { Code = AccessTypeCode.HcimEnrolment,                       Name = "HCIMWeb Enrolment"       },
        new AccessType { Code = AccessTypeCode.DriverFitness,                       Name = "Driver Medical Fitness"  },
        new AccessType { Code = AccessTypeCode.DigitalEvidence,                     Name = "Digital Evidence Management" },
        new AccessType { Code = AccessTypeCode.DigitalEvidenceCaseManagement,       Name = "Digital Evidence Case Management" },
        new AccessType { Code = AccessTypeCode.Uci,                                 Name = "Fraser Health UCI"         },
        new AccessType { Code = AccessTypeCode.MSTeams,                             Name = "MS Teams for Clinical Use" },
        new AccessType { Code = AccessTypeCode.DigitalEvidenceDisclosure,           Name = "Digital Evidence Disclosure" },
        new AccessType { Code = AccessTypeCode.DigitalEvidenceDefence,              Name = "Digital Evidence Defence" },
                new AccessType { Code = AccessTypeCode.JAMLEA,              Name = "JUSTIN Law Enforcement" },
                                new AccessType { Code = AccessTypeCode.JAMPOR,              Name = "JUSTIN Protection Order" },
                                                                new AccessType { Code = AccessTypeCode.JAMRCC,              Name = "JUSTIN Request for Crown" },

    };
}
