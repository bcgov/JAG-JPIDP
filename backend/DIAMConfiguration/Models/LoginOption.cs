namespace DIAMConfiguration.Models;

using DIAMConfiguration.Data;

public class LoginOption
{
    //    "formControl": "selectedAgency",
    //    "formList": "filteredAgencies",
    //    "idp": "submitting_agencies",
    //    "name": "Agency",
    //    "type": "AUTOCOMPLETE"

    public string FormControl { get; set; } = string.Empty;
    public string FormList { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Idp { get; set; } = string.Empty;

    public static IEnumerable<LoginOption> FromHostConfig(HostConfig hostConfig)
    {
        var options = new List<LoginOption>();

        var configs = hostConfig.HostLoginConfigs;
        foreach (var item in configs)
        {
            options.Add(new LoginOption
            {
                Idp = item.Idp,
                Name = item.Name,
                FormControl = item.FormControl,
                FormList = item.FormList,
                Type = item.Type.ToString()
            });
        }
        return options;
    }
}
