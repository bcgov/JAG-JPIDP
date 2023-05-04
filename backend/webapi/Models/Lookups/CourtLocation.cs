namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("CourtLocation")]
public class CourtLocation
{
    [Key]
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public bool Staffed { get; set; } = true;
    public string? Alias { get; set; } = string.Empty;

    public ICollection<CourtSubLocation> CourtSubLocations { get; set; } = new List<CourtSubLocation>();

    public class CourtLocationDataGenerator : ILookupDataGenerator<CourtLocation>
    {
        public IEnumerable<CourtLocation> Generate() => new[]
        {
             new CourtLocation { City = "Abbotsford", Name = "Abbotsford Court", Code = "ABB", Active = true },
             new CourtLocation { City = "Atlin", Name = "Atlin Court", Code = "ATL", Active = true },
             new CourtLocation { City = "Burns Lake", Name = "Burns Lake Court", Code = "BURN", Active = true },
             new CourtLocation { City = "Campbell River", Name = "Campbell River Court", Code = "CAMP", Active = true },
             new CourtLocation { City = "Chiliwack", Name = "Chiliwack Court", Code = "CHIL", Active = true },
             new CourtLocation { City = "Courtenay", Name = "Courtenay Court", Code = "COUR", Active = true },
             new CourtLocation { City = "Cranbrook", Name = "Cranbrook Court", Code = "CRAN", Active = true },
             new CourtLocation { City = "Daajing Giids", Name = "Courtenay Court", Code = "DAAJ", Active = true },
             new CourtLocation { City = "Dawson Creek", Name = "Dawson Creek Court", Code = "DAWS", Active = true },
             new CourtLocation { City = "Duncan", Name = "Duncan Court", Code = "DUNC", Active = true },
             new CourtLocation { City = "Fort Nelson", Name = "Fort Nelson Court", Code = "FORT", Active = true },
             new CourtLocation { City = "Fort St. John", Name = "Fort St. John Court", Code = "FTSJ", Active = true },
             new CourtLocation { City = "Golden", Name = "Golden Court", Code = "GOLD", Active = true },
             new CourtLocation { City = "Kamloops", Name = "Kamloops Court", Code = "KAM", Active = true },
             new CourtLocation { City = "Kelowna", Name = "Kelowna Court", Code = "KEL", Active = true },
             new CourtLocation { City = "Mackenzie", Name = "Mackenzie Court", Code = "MACK", Active = true },
             new CourtLocation { City = "Nanaimo", Name = "Nanaimo Court", Code = "NANA", Active = true },
             new CourtLocation { City = "Nelson", Name = "Nelson Court", Code = "NELS", Active = true },
             new CourtLocation { City = "New Westminster", Name = "New Westminster Court", Code = "NEWWEST", Active = true },
             new CourtLocation { City = "Vancouver", Name = "North Vancouver Court", Code = "NVAN", Active = true },
             new CourtLocation { City = "Pemberton", Name = "Pemberton Court", Code = "PEMB", Active = true },
             new CourtLocation { City = "Penticton", Name = "Penticton Court", Code = "PENT", Active = true },
             new CourtLocation { City = "Port Alberni", Name = "Port Alberni Court", Code = "PORTA", Active = true },
             new CourtLocation { City = "Port Coquitlam", Name = "Port Coquitlam Court", Code = "POCO", Active = true },
             new CourtLocation { City = "Port Hardy", Name = "Port Hardy Court", Code = "POHA", Active = true },
             new CourtLocation { City = "Powell River", Name = "Powell River Court", Code = "POWE", Active = true },
             new CourtLocation { City = "Prince George", Name = "Prince George Court", Code = "PRGEO", Active = true },
             new CourtLocation { City = "Prince Rupert", Name = "Prince Rupert Court", Code = "PRRUP", Active = true },
             new CourtLocation { City = "Quesnel", Name = "Quesnel Court", Code = "QUES", Active = true },
             new CourtLocation { City = "Richmond", Name = "Richmond Court", Code = "RICH", Active = true },
             new CourtLocation { City = "Rossland", Name = "Rossland Court", Code = "ROSS", Active = true },
             new CourtLocation { City = "Salmon Arm", Name = "Salmon Arm Court", Code = "SALM", Active = true },
             new CourtLocation { City = "Sechelt", Name = "Sechelt Court", Code = "SECH", Active = true },
             new CourtLocation { City = "Smithers", Name = "Smithers Court", Code = "SMITH", Active = true },
             new CourtLocation { City = "Surrey", Name = "Surrey Court", Code = "SURR", Active = true },
             new CourtLocation { City = "Terrace", Name = "Terrace Court", Code = "TERR", Active = true },
             new CourtLocation { City = "Valemount", Name = "Valemount Court", Code = "VALE", Active = true },
             new CourtLocation { City = "Vancouver", Name = "Vancouver Civil Court", Code = "VANCRIM", Active = true },
             new CourtLocation { City = "Vancouver", Name = "Vancouver Criminal Court", Code = "VANCIV", Active = true },
             new CourtLocation { City = "Vernon", Name = "Vernon Court", Code = "VERN", Active = true },
             new CourtLocation { City = "Victoria", Name = "Victoria Court", Code = "VIC", Active = true },
             new CourtLocation { City = "Victoria", Name = "Western Communities Court", Code = "WESTCOM", Active = true },
             new CourtLocation { City = "Williams Lake", Name = "Williams Lake Court", Code = "WILL", Active = true },


        };

    }
}
