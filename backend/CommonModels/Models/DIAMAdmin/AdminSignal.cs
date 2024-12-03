namespace CommonModels.Models.DIAMAdmin;


public class AdminSignal
{
    public string Msg { get; set; }
    public AdminCommandSet Command { get; set; }
    public string UserId { get; set; }
}
