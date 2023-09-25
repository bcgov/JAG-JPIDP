namespace Pidp.Models.Lookups;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("CourtLocation")]
public class CourtLocation
{
    [Key]
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public bool Staffed { get; set; } = true;

    public ICollection<CourtSubLocation> CourtSubLocations { get; set; } = new List<CourtSubLocation>();


    public class CourtLocationDataGenerator : ILookupDataGenerator<CourtLocation>
    {

        public IEnumerable<CourtLocation> Generate() => new[]
        {

     new CourtLocation { Name = "Abbotsford Law Courts", Code = "3561", Active = true },
     new CourtLocation { Name = "Anahim Lake Provincial Court", Code = "5681", Active = true },
     new CourtLocation { Name = "Anvil Centre", Code = "ACNW", Active = true },
     new CourtLocation { Name = "Atlin Provincial Court", Code = "5691", Active = true },
     new CourtLocation { Name = "BC Court of Appeal", Code = "COA", Active = true },
     new CourtLocation { Name = "Bella Bella Provincial Court", Code = "2007", Active = true },
     new CourtLocation { Name = "Bella Coola Provincial Court", Code = "2008", Active = true },
     new CourtLocation { Name = "Burns Lake Provincial Court", Code = "5701", Active = true },
     new CourtLocation { Name = "Campbell River Law Courts", Code = "1031", Active = true },
     new CourtLocation { Name = "Castlegar Provincial Court", Code = "4681", Active = true },
     new CourtLocation { Name = "Chetwynd Provincial Court", Code = "5721", Active = true },
     new CourtLocation { Name = "Chilliwack Law Courts", Code = "3521", Active = true },
     new CourtLocation { Name = "Clearwater Provincial Court", Code = "4701", Active = true },
     new CourtLocation { Name = "Court of Appeal of BC - Kamloops", Code = "CAKA", Active = true },
     new CourtLocation { Name = "Court of Appeal of BC - Kelowna", Code = "CAKE", Active = true },
     new CourtLocation { Name = "Court of Appeal of BC - Vancouver", Code = "CAVA", Active = true },
     new CourtLocation { Name = "Court of Appeal of BC - Victoria", Code = "CAVI", Active = true },
     new CourtLocation { Name = "Courtenay Law Courts", Code = "1041", Active = true },
     new CourtLocation { Name = "Cranbrook Law Courts", Code = "4711", Active = true },
     new CourtLocation { Name = "Creston Law Courts", Code = "4721", Active = true },
     new CourtLocation { Name = "Daajing Giids Provincial Court", Code = "5911", Active = true },
     new CourtLocation { Name = "Dawson Creek Law Courts", Code = "5731", Active = true },
     new CourtLocation { Name = "Dease Lake Provincial Court", Code = "5741", Active = true },
     new CourtLocation { Name = "Downtown Community Court", Code = "2042", Active = true },
     new CourtLocation { Name = "Duncan Law Courts", Code = "1051", Active = true },
     new CourtLocation { Name = "Fernie Law Courts", Code = "4731", Active = true },
     new CourtLocation { Name = "Fort Nelson Law Courts", Code = "5751", Active = true },
     new CourtLocation { Name = "Fort St James Provincial Court", Code = "5761", Active = true },
     new CourtLocation { Name = "Fort St John Law Courts", Code = "5771", Active = true },
     new CourtLocation { Name = "Fraser Lake Provincial Court", Code = "5781", Active = true },
     new CourtLocation { Name = "Ganges Provincial Court", Code = "1061", Active = true },
     new CourtLocation { Name = "Gold River Provincial Court", Code = "1071", Active = true },
     new CourtLocation { Name = "Golden Law Courts", Code = "4741", Active = true },
     new CourtLocation { Name = "Good Hope Lake Provincial Court", Code = "5711", Active = true },
     new CourtLocation { Name = "Grand Forks Law Courts", Code = "4751", Active = true },
     new CourtLocation { Name = "Hazelton Provincial Court", Code = "5861", Active = true },
     new CourtLocation { Name = "Houston Provincial Court", Code = "5791", Active = true },
     new CourtLocation { Name = "Inn at the Quay", Code = "ITQ", Active = true },
     new CourtLocation { Name = "Invermere Law Courts", Code = "4771", Active = true },
     new CourtLocation { Name = "Justice Centre (Judicial)", Code = "2041", Active = true },
     new CourtLocation { Name = "Kamloops Law Courts", Code = "4781", Active = true },
     new CourtLocation { Name = "Kelowna Law Courts", Code = "4801", Active = true },
     new CourtLocation { Name = "Kitimat Law Courts", Code = "5811", Active = true },
     new CourtLocation { Name = "Klemtu Provincial Court", Code = "2009", Active = true },
     new CourtLocation { Name = "Kwadacha Provincial Court", Code = "5775", Active = true },
     new CourtLocation { Name = "Lillooet Law Courts", Code = "4821", Active = true },
     new CourtLocation { Name = "Lower Post Provincial Court", Code = "5821", Active = true },
     new CourtLocation { Name = "Mackenzie Provincial Court", Code = "5831", Active = true },
     new CourtLocation { Name = "Masset Provincial Court", Code = "5841", Active = true },
     new CourtLocation { Name = "McBride Provincial Court", Code = "5845", Active = true },
     new CourtLocation { Name = "Merritt Civic Centre", Code = "MCC", Active = true },
     new CourtLocation { Name = "Merritt Law Courts", Code = "4851", Active = true },
     new CourtLocation { Name = "Nakusp Provincial Court", Code = "4861", Active = true },
     new CourtLocation { Name = "Nanaimo Law Courts", Code = "1091", Active = true },
     new CourtLocation { Name = "Nelson Law Courts", Code = "4871", Active = true },
     new CourtLocation { Name = "New Aiyansh Provincial Court", Code = "5851", Active = true },
     new CourtLocation { Name = "New Westminster Law Courts", Code = "3581", Active = true },
     new CourtLocation { Name = "100 Mile House Law Courts", Code = "5871", Active = true },
     new CourtLocation { Name = "North Vancouver Provincial Court", Code = "2011", Active = true },
     new CourtLocation { Name = "Pemberton Provincial Court", Code = "2021", Active = true },
     new CourtLocation { Name = "Penticton Law Courts", Code = "4891", Active = true },
     new CourtLocation { Name = "Port Alberni Law Courts", Code = "1121", Active = true },
     new CourtLocation { Name = "Port Coquitlam Law Courts", Code = "3531", Active = true },
     new CourtLocation { Name = "Port Hardy Law Courts", Code = "1141", Active = true },
     new CourtLocation { Name = "Powell River Law Courts", Code = "1145", Active = true },
     new CourtLocation { Name = "Prince George Law Courts", Code = "5891", Active = true },
     new CourtLocation { Name = "Prince Rupert Law Courts", Code = "5901", Active = true },
     new CourtLocation { Name = "Princeton Law Courts", Code = "4901", Active = true },
     new CourtLocation { Name = "Quesnel Law Courts", Code = "5921", Active = true },
     new CourtLocation { Name = "Revelstoke Law Courts", Code = "4911", Active = true },
     new CourtLocation { Name = "Richmond Provincial Court", Code = "2025", Active = true },
     new CourtLocation { Name = "Robson Square Provincial Court", Code = "2045", Active = true },
     new CourtLocation { Name = "Rossland Law Courts", Code = "4921", Active = true },
     new CourtLocation { Name = "Salmon Arm Law Courts", Code = "4941", Active = true },
     new CourtLocation { Name = "Sechelt Provincial Court", Code = "2031", Active = true },
     new CourtLocation { Name = "Si'em' Lelum Gymnasium", Code = "SLG", Active = true },
     new CourtLocation { Name = "Sidney Provincial Court", Code = "1151", Active = true },
     new CourtLocation { Name = "Smithers Law Courts", Code = "5931", Active = true },
     new CourtLocation { Name = "Sparwood Provincial Court", Code = "4951", Active = true },
     new CourtLocation { Name = "Stewart Provincial Court", Code = "5941", Active = true },
     new CourtLocation { Name = "Surrey Provincial Court", Code = "3585", Active = true },
     new CourtLocation { Name = "Terrace Law Courts", Code = "5951", Active = true },
     new CourtLocation { Name = "Tofino Provincial Court", Code = "1181", Active = true },
     new CourtLocation { Name = "Tsay Keh Dene Provincial Court", Code = "5805", Active = true },
     new CourtLocation { Name = "Tumbler Ridge Provincial Court", Code = "5955", Active = true },
     new CourtLocation { Name = "Ucluelet Provincial Court", Code = "1191", Active = true },
     new CourtLocation { Name = "Valemount Provincial Court", Code = "5959", Active = true },
     new CourtLocation { Name = "Vancouver Law Courts", Code = "6011", Active = true },
     new CourtLocation { Name = "Vancouver Provincial Court", Code = "2040", Active = true },
     new CourtLocation { Name = "Vanderhoof Law Courts", Code = "5961", Active = true },
     new CourtLocation { Name = "Vernon Law Courts", Code = "4971", Active = true },
     new CourtLocation { Name = "Western Communities Provincial Court", Code = "1211", Active = true },
     new CourtLocation { Name = "Williams Lake Law Courts", Code = "5971", Active = true },
     new CourtLocation { Name = "Williams Lake MacKinnon Hall", Code = "WLMH", Active = true },
     new CourtLocation { Name = "Victoria Law Courts", Code = "1201", Active = true },


        };

    }
}
