namespace Common.Models.EDT;


public class UpdatePersonContactInfoModel
{

    public string EMail { get; set; } = string.Empty;
    public string Idp { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string KeycloakUserId { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public int PartyId { get; set; }
    public string Phone { get; set; } = string.Empty;

}
