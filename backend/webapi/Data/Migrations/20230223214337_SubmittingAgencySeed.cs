using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidp.Data.Migrations
{
    public partial class SubmittingAgencySeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "IdpHint",
                table: "SubmittingAgencyLookup",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "SubmittingAgencyLookup",
                columns: new[] { "Code", "IdpHint", "Name" },
                values: new object[,]
                {
                    { "100", "", "Southeast District Kelowna - RCMP" },
                    { "101", "", "North Okanagan Rural RCMP" },
                    { "105", "", "Kelowna RCMP" },
                    { "105C", "", "Kelowna RCMP - Daytime/Records" },
                    { "106", "", "Keremeos RCMP" },
                    { "109", "", "Oliver RCMP" },
                    { "110", "", "Osoyoos RCMP" },
                    { "112", "", "Penticton RCMP" },
                    { "113", "", "Princeton RCMP" },
                    { "116", "", "Salmon Arm RCMP" },
                    { "117", "", "Sicamous RCMP" },
                    { "119", "", "Vernon RCMP" },
                    { "121", "", "Summerland RCMP" },
                    { "124", "", "Revelstoke RCMP" },
                    { "124A", "", "BCHP - Revelstoke - RCMP" },
                    { "201", "", "Alexis Creek RCMP" },
                    { "202", "", "Anahim Lake RCMP" },
                    { "203", "", "Ashcroft RCMP" },
                    { "204", "", "Barriere RCMP" },
                    { "206", "", "Chase RCMP" },
                    { "207", "", "Clearwater RCMP" },
                    { "208", "", "Clinton RCMP" },
                    { "209", "", "Kamloops - Tk'emlups RCMP" },
                    { "210", "", "Kamloops RCMP" },
                    { "211", "", "Lillooet RCMP" },
                    { "212", "", "Logan Lake RCMP" },
                    { "213", "", "Lytton RCMP" },
                    { "215", "", "Merritt RCMP" },
                    { "216", "", "100 Mile House RCMP" },
                    { "218", "", "Valemount RCMP" },
                    { "221", "", "Williams Lake RCMP" },
                    { "301", "", "Castlegar RCMP" },
                    { "303", "", "Cranbrook RCMP" },
                    { "305", "", "Creston RCMP" },
                    { "309", "", "Golden RCMP" },
                    { "310", "", "Grand Forks RCMP" },
                    { "311", "", "Columbia Valley RCMP" },
                    { "312", "", "Kaslo RCMP" },
                    { "314", "", "Kimberley RCMP" },
                    { "315", "", "Midway RCMP" },
                    { "316", "", "Nakusp RCMP" },
                    { "317", "", "Central Kootenay Regional RCMP" },
                    { "318", "", "Slocan Lake RCMP" },
                    { "321", "", "Salmo RCMP" },
                    { "322", "", "Elk Valley RCMP" },
                    { "324", "", "Trail and Greater District RCMP" },
                    { "40", "", "Island District Victoria - RCMP" },
                    { "400", "", "Vancouver Police- DNA" },
                    { "401", "", "Vancouver Police Department" },
                    { "401A", "", "Vancouver Police Department - Fingerprinting" },
                    { "401X", "", "ZZzVancouver Police Department" },
                    { "402", "vicpd", "Victoria Police Department" },
                    { "403", "saanichpd", "Saanich Police Department" },
                    { "404", "", "Central Saanich Police Service" },
                    { "405", "esquimaltpd", "Esquimalt Police Department" },
                    { "406", "", "Oak Bay Police Department" },
                    { "407", "deltapd", "Delta Police Department" },
                    { "408", "", "Abbotsford Police Department" },
                    { "409", "", "New Westminster Police Department" },
                    { "410", "", "West Vancouver Police Department" },
                    { "411", "", "Nelson Police Department" },
                    { "412", "", "Port Moody Police Department" },
                    { "488", "", "Metro Vancouver Transit Police" },
                    { "5", "", "Vancouver Police Department Reserves" },
                    { "501", "", "Burns Lake RCMP" },
                    { "502", "", "Chetwynd RCMP" },
                    { "504", "", "Dawson Creek RCMP" },
                    { "505", "", "Northern Rockies RCMP" },
                    { "506", "", "Fort St James RCMP" },
                    { "508", "", "Fort St John RCMP" },
                    { "509", "", "Fraser Lake RCMP" },
                    { "510", "", "Hudson's Hope RCMP" },
                    { "512", "", "McBride RCMP" },
                    { "515", "", "Quesnel RCMP" },
                    { "517", "", "Vanderhoof RCMP" },
                    { "521", "", "Prince George RCMP" },
                    { "522", "", "Mackenzie RCMP" },
                    { "524", "", "Tumbler Ridge RCMP" },
                    { "525", "", "Tsay Keh Dene RCMP" },
                    { "6", "", "Forensics Laboratory - RCMP" },
                    { "601", "", "Atlin RCMP" },
                    { "602", "", "Bella Coola RCMP" },
                    { "604", "", "New Hazelton RCMP" },
                    { "605", "", "Houston RCMP" },
                    { "607", "", "Kitimat RCMP" },
                    { "608", "", "Masset RCMP" },
                    { "611", "", "Prince Rupert RCMP" },
                    { "612", "", "Queen Charlotte RCMP" },
                    { "614", "", "Stewart RCMP" },
                    { "615", "", "Dease Lake RCMP" },
                    { "617", "", "Terrace RCMP" },
                    { "620", "", "Bella Bella RCMP" },
                    { "621", "", "Lisims / Nass Valley RCMP" },
                    { "622", "", "Smithers RCMP" },
                    { "7004", "", "Burnaby RCMP- DNA" },
                    { "703", "", "Agassiz RCMP" },
                    { "704", "", "Burnaby RCMP" },
                    { "704B", "", "Burnaby RCMP - Bail Hearings" },
                    { "707", "", "Chilliwack RCMP" },
                    { "710", "", "Coquitlam RCMP" },
                    { "713", "", "Ridge Meadows RCMP" },
                    { "716", "", "Langley RCMP" },
                    { "719", "", "Mission RCMP" },
                    { "720", "", "North Vancouver RCMP" },
                    { "722", "", "Richmond RCMP" },
                    { "725", "", "Squamish RCMP" },
                    { "726", "", "Surrey RCMP" },
                    { "727", "", "University of British Columbia RCMP" },
                    { "728", "", "SPS - LENS ONLY" },
                    { "729", "", "White Rock RCMP" },
                    { "734", "", "BCHP - Deas Island - RCMP" },
                    { "735", "", "BCHP - Burnaby - RCMP" },
                    { "738", "", "Whistler/Pemberton RCMP" },
                    { "742", "", "Bowen Island RCMP" },
                    { "743", "", "Sunshine Coast RCMP" },
                    { "745", "", "Hope RCMP" },
                    { "804", "", "North Cowichan - Duncan RCMP" },
                    { "805", "", "Gabriola Island RCMP" },
                    { "806", "", "Saltspring RCMP" },
                    { "808", "", "Lake Cowichan RCMP" },
                    { "810", "", "Nanaimo RCMP" },
                    { "810A", "", "Nanaimo RCMP - Traffic" },
                    { "811", "", "Outer Gulf Islands RCMP" },
                    { "812", "", "Shawnigan Lake RCMP" },
                    { "814", "", "Sidney / North Saanich RCMP" },
                    { "815", "", "Sooke RCMP" },
                    { "821", "", "West Shore RCMP" },
                    { "821B", "", "West Shore RCMP - URGENT Fax" },
                    { "824", "", "Ladysmith RCMP" },
                    { "901", "", "Alert Bay RCMP" },
                    { "903", "", "Campbell River RCMP" },
                    { "906", "", "Comox Valley RCMP" },
                    { "907", "", "Nootka Sound RCMP" },
                    { "910", "", "Port Alberni RCMP" },
                    { "911", "", "Port Alice RCMP" },
                    { "913", "", "Port McNeill RCMP" },
                    { "915", "", "Powell River RCMP" },
                    { "916", "", "Quadra Island RCMP" },
                    { "917", "", "Sayward RCMP" },
                    { "919", "", "Tofino RCMP (Ahousaht)" },
                    { "919A", "", "Tofino RCMP - After Hours/Cells" },
                    { "920", "", "Ucluelet RCMP" },
                    { "922", "", "Parksville / Oceanside RCMP" },
                    { "923", "", "Port Hardy RCMP" },
                    { "924", "", "Texada Island RCMP" },
                    { "ATF", "", "Major Crime Section - Integrated Municipal Provincial Auto Crime Team - RCMP" },
                    { "BCHV", "", "BCHP - VAN ISLAND - RCMP" },
                    { "CBID", "", "Canada Border Services Agency - Investigations Division" },
                    { "CBIF", "", "Canada Border Services Agency - Investigations Division - FingerprintingDivi" },
                    { "CBSO", "", "Canada Border Services Agency - Port of Osoyoos" },
                    { "CFBE", "", "Canadian Forces Branch - Military Police - Esquimalt" },
                    { "CFKE", "", "CFSEU Kelowna - RCMP" },
                    { "CFMP", "", "Canadian Forces Branch - Military Police Comox" },
                    { "CFNP", "", "Canadian Forces National Invest. Service - Pacific Region" },
                    { "CFPG", "", "CFSEU Prince George - RCMP" },
                    { "CFSE", "", "CFSEU Vancouver - RCMP" },
                    { "CITA", "", "BCHP - Ashcroft - RCMP" },
                    { "CITC", "", "BCHP - Clearwater - RCMP" },
                    { "CITM", "", "BCHP - Merritt - RCMP" },
                    { "CITS", "", "BCHP - Parksville - RCMP" },
                    { "CNKA", "", "Canadian National Police Services - Kamloops" },
                    { "CNNV", "", "Canadian National Police Services - North Van" },
                    { "CNPC", "", "Canadian National Police Services - Chilliwack" },
                    { "CNPG", "", "Canadian National Police Services - Prince George" },
                    { "CNPR", "", "Canadian National Police Services - Prince Rupert" },
                    { "CNPS", "", "Canadian National Police Services - Surrey" },
                    { "CNPT", "", "Canadian National Police Services - Terrace" },
                    { "CNSQ", "", "Canadian National Police Services - Squamish" },
                    { "COTS", "", "BCHP - Kelowna - RCMP" },
                    { "CPGL", "", "Canadian Pacific Police - Golden" },
                    { "CPKA", "", "Canadian Pacific Police - Kamloops" },
                    { "CPSC", "", "Canadian Pacific Police Service - Cranbrook" },
                    { "DEO", "", "E Division Emergency Ops RCMP" },
                    { "EHQ", "", "E Division Headquarters - RCMP" },
                    { "EOCC", "", "EHQ OCC RCMP - Warrant Cancellations" },
                    { "GOTS", "", "BCHP - Golden - RCMP" },
                    { "HRCA", "", "Human Resources & Skills Dev. Canada - Abbotsford" },
                    { "IGTF", "", "CFSEU LMD 6000 - RCMP" },
                    { "IHI", "", "Integrated Homicide Investigation Team - RCMP" },
                    { "IMCJ", "", "Major Crime Section - Vancouver Island Integrated - RCMP" },
                    { "IMET", "", "FSOC Integrated Market Enforcement Team - RCMP" },
                    { "INSE", "", "Integrated National Security Enforcement Team - RCMP" },
                    { "IRGV", "", "Integrated Road Safety Unit Greater Vancouver - RCMP" },
                    { "IRNA", "", "BCHP - Central Island IRSU - RCMP" },
                    { "IRNI", "", "BCHP - North Island IRSU - RCMP" },
                    { "IRSL", "", "BCHP - Fraser Coast IRSU - RCMP" },
                    { "IRSU", "", "BCHP - CRD IRSU - RCMP" },
                    { "IRSW", "", "BCHP - Nelson IRSU - RCMP" },
                    { "ITC", "", "Digital Forensics Services - RCMP" },
                    { "LNTP", "", "Stl' atl' imx Tribal Police Lillooet - RCMP" },
                    { "NCMD", "", "North Coast Marine Services - RCMP" },
                    { "NITS", "", "BCHP - Campbell River - RCMP" },
                    { "NOTS", "", "BCHP - Falkland - RCMP" },
                    { "RECI", "", "Criminal Intelligence Section - RCMP" },
                    { "RFD", "", "Federal Serious & Organized Crime EHQ - RCMP" },
                    { "RFDI", "", "Federal Serious & Organized Crime Island - RCMP" },
                    { "RFDN", "", "Federal Serious & Organized Crime North - RCMP" },
                    { "RFDS", "", "Federal Serious & Organized Crime South - RCMP" },
                    { "RFFG", "", "BCHP - Prince George - RCMP" },
                    { "RID2", "", "Victoria 5001 - RCMP" },
                    { "RKSO", "", "BCHP - Kamloops - RCMP" },
                    { "RLMD", "", "Lower Mainland District Surrey - RCMP" },
                    { "RMCS", "", "Major Crime Section - RCMP" },
                    { "RND2", "", "E Division Prince George 2100 - RCMP" },
                    { "RND5", "", "Major Crime Section - North District - RCMP" },
                    { "RNDO", "", "North District Prince George - RCMP" },
                    { "RPR2", "", "E Division Prince Rupert 2100 - RCMP" },
                    { "RPRO", "", "Prince Rupert  (Do Not Use)  N.District RCMP" },
                    { "RPSU", "", "E Division Professional Responsibility Unit - RCMP" },
                    { "RSE2", "", "E Division Kelowna 2100 - RCMP" },
                    { "RSE5", "", "Major Crime Section - Southeast District - RCMP" },
                    { "RSEC", "", "BCHP - Cranbrook - RCMP" },
                    { "RSEN", "", "BCHP - Nelson - RCMP" },
                    { "RTRF", "", "BCHP - RCMP" },
                    { "SIHP", "", "BCHP - Chemainus - RCMP" },
                    { "SOHP", "", "BCHP - Keremeos - RCMP" },
                    { "SUHP", "", "BCHP - Chilliwack - RCMP" },
                    { "SWHQ", "", "ZZ RCMP (Do Not Use) SW District HQ" },
                    { "SXTP", "", "Stl' atl' imx Tribal Police Mount Currie - RCMP" },
                    { "TAKL", "", "Takla Landing RCMP" },
                    { "VOCA", "", "CFSEU Victoria - RCMP" },
                    { "VPED", "", "Victoria, City of - Parking Enforcement Detachment" },
                    { "WCMD", "", "West Coast Marine Services - RCMP" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "100");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "101");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "105");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "105C");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "106");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "109");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "110");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "112");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "113");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "116");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "117");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "119");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "121");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "124");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "124A");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "201");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "202");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "203");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "204");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "206");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "207");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "208");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "209");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "210");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "211");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "212");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "213");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "215");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "216");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "218");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "221");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "301");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "303");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "305");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "309");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "310");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "311");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "312");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "314");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "315");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "316");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "317");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "318");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "321");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "322");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "324");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "40");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "400");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "401");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "401A");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "401X");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "402");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "403");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "404");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "405");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "406");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "407");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "408");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "409");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "410");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "411");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "412");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "488");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "5");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "501");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "502");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "504");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "505");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "506");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "508");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "509");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "510");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "512");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "515");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "517");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "521");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "522");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "524");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "525");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "6");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "601");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "602");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "604");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "605");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "607");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "608");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "611");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "612");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "614");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "615");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "617");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "620");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "621");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "622");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "7004");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "703");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "704");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "704B");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "707");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "710");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "713");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "716");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "719");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "720");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "722");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "725");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "726");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "727");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "728");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "729");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "734");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "735");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "738");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "742");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "743");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "745");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "804");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "805");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "806");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "808");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "810");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "810A");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "811");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "812");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "814");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "815");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "821");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "821B");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "824");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "901");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "903");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "906");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "907");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "910");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "911");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "913");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "915");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "916");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "917");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "919");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "919A");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "920");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "922");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "923");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "924");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "ATF");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "BCHV");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CBID");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CBIF");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CBSO");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CFBE");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CFKE");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CFMP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CFNP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CFPG");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CFSE");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CITA");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CITC");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CITM");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CITS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNKA");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNNV");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNPC");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNPG");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNPR");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNPS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNPT");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CNSQ");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "COTS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CPGL");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CPKA");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "CPSC");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "DEO");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "EHQ");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "EOCC");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "GOTS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "HRCA");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IGTF");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IHI");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IMCJ");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IMET");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "INSE");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IRGV");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IRNA");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IRNI");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IRSL");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IRSU");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "IRSW");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "ITC");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "LNTP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "NCMD");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "NITS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "NOTS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RECI");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RFD");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RFDI");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RFDN");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RFDS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RFFG");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RID2");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RKSO");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RLMD");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RMCS");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RND2");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RND5");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RNDO");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RPR2");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RPRO");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RPSU");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RSE2");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RSE5");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RSEC");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RSEN");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "RTRF");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "SIHP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "SOHP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "SUHP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "SWHQ");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "SXTP");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "TAKL");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "VOCA");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "VPED");

            migrationBuilder.DeleteData(
                table: "SubmittingAgencyLookup",
                keyColumn: "Code",
                keyValue: "WCMD");

            migrationBuilder.DropColumn(
                name: "IdpHint",
                table: "SubmittingAgencyLookup");

            migrationBuilder.AlterColumn<int>(
                name: "Code",
                table: "SubmittingAgencyLookup",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

        }
    }
}
