namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum AccessTypeCode
{
    SAEforms = 1,
    HcimAccountTransfer,
    HcimEnrolment,
    DriverFitness,
    DigitalEvidence,
    DigitalEvidenceCaseManagement,
    DigitalEvidenceDisclosure,   // defence/duty will be User in Disclosure
    DigitalEvidenceDefence,      // defence/duty will be Participant in Core
    Uci,
    MSTeams
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

    };
}
