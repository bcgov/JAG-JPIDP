namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum OrganizationCode
{
    JusticeSector = 1,
    LawEnforcement,
    LawSociety,
    CorrectionService,
    HealthAuthority,
    SubmittingAgency,
    BcGovernmentMinistry,
    ICBC,
    Other,
    VicPd,
    DeltaPd,
    SaanichPd
}

[Table("OrganizationLookup")]
public class Organization
{
    [Key]
    public OrganizationCode Code { get; set; }

    public string Name { get; set; } = string.Empty;
    public string IdpHint { get; set; } = string.Empty;
}

//public class OrganizationDataGenerator : ILookupDataGenerator<Organization>
//{
//    public IEnumerable<Organization> Generate() => new[]
//    {
//        new Organization { Code = OrganizationCode.JusticeSector,        Name = "Justice Sector", IdpHint = "ADFS"},
//        new Organization { Code = OrganizationCode.LawEnforcement,       Name = "BC Law Enforcement", IdpHint = ""},
//        new Organization { Code = OrganizationCode.LawSociety,           Name = "BC Law Society",  IdpHint = "vcc"},
//        new Organization { Code = OrganizationCode.CorrectionService,    Name = "BC Corrections Service"},
//        new Organization { Code = OrganizationCode.HealthAuthority,      Name = "Health Authority"       },
//        new Organization { Code = OrganizationCode.BcGovernmentMinistry, Name = "BC Government Ministry", IdpHint = "idir" },
//        new Organization { Code = OrganizationCode.ICBC,                 Name = "ICBC", IdpHint = "icbc"                   },
//        new Organization { Code = OrganizationCode.Other,                Name = "Other" },
//        new Organization { Code = OrganizationCode.VicPd,                Name = "Victoria Police Department", IdpHint = "vicpd"  },
//        new Organization { Code = OrganizationCode.DeltaPd,              Name = "Delta Police Department", IdpHint = "deltapd"  },
//        new Organization { Code = OrganizationCode.SaanichPd,            Name = "Saanich Police Department", IdpHint = "saanichpd"  },

//    };
//}
