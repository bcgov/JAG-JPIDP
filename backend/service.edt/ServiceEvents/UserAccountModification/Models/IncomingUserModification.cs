namespace edt.service.ServiceEvents.UserAccountModification.Models;


using System.Text;

public class IncomingUserModification
{
    public string UserID { get; set; } = string.Empty;
    public int ChangeId { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string IdpType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTime ChangeDateTime { get; set; } = DateTime.Now;
    public Dictionary<ChangeType, SingleChangeType> SingleChangeTypes { get; set; } = new Dictionary<ChangeType, SingleChangeType>();
    public Dictionary<ChangeType, ListChangeType> ListChangeTypes { get; set; } = new Dictionary<ChangeType, ListChangeType>();
    public Dictionary<ChangeType, BooleanChangeType> BooleanChangeTypes { get; set; } = new Dictionary<ChangeType, BooleanChangeType>();
    public bool IsAccountActivated() => (this.BooleanChangeTypes.ContainsKey(ChangeType.ACTIVATION) && this.BooleanChangeTypes[ChangeType.ACTIVATION].Equals(true)) || (this.ListChangeTypes.ContainsKey(ChangeType.REGIONS) && this.ListChangeTypes[ChangeType.REGIONS].From.Any() == false && this.ListChangeTypes[ChangeType.REGIONS].To.Any());

    public bool IsAccountDeactivated() => (this.BooleanChangeTypes.ContainsKey(ChangeType.ACTIVATION) && this.BooleanChangeTypes[ChangeType.ACTIVATION].To.Equals(false)) || this.SingleChangeTypes.ContainsKey(ChangeType.EMAIL);
    internal string ToChangeHtml()
    {
        var changes = new StringBuilder();

        foreach (var key in this.SingleChangeTypes.Keys)
        {

            changes.Append("<li>").Append(key.GetChangeTypeInfo().DisplayName).Append(" From: ").Append(this.SingleChangeTypes[key].From).Append(" To: ").Append(this.SingleChangeTypes[key].To).Append("</li>\n");
        }

        foreach (var key in this.BooleanChangeTypes.Keys)
        {
            if (key == ChangeType.ACTIVATION)
            {
                // account re-activated
                if (this.BooleanChangeTypes[ChangeType.ACTIVATION].From == false && this.BooleanChangeTypes[ChangeType.ACTIVATION].To == true)
                {
                    changes.Append("<li>Your account has been <b>reactivated</b></li>\n");
                }
                // deactivated
                else
                {
                    changes.Append("<li>Your account has been <b>disabled</b></li>\n");

                }
            }
            else
            {
                changes.Append("<li>").Append(key.GetChangeTypeInfo().DisplayName).Append(" From: ").Append(this.BooleanChangeTypes[key].From).Append(" To: ").Append(this.BooleanChangeTypes[key].To).Append("</li>\n");
            }
        }

        foreach (var key in this.ListChangeTypes.Keys)
        {
            changes.Append("<h3>").Append(key.GetChangeTypeInfo().DisplayName).Append("</h3><table cellpadding='8px' border='2px #0066CC solid' style='border-collapse: collapse;'><thead><th>From</th><th>To</th></thead><tbody><tr><td>");

            foreach (var oldVal in this.ListChangeTypes[key].From)
            {
                changes.Append(oldVal).Append("<br/>");
            }
            changes.Append("</td><td>");
            foreach (var oldVal in this.ListChangeTypes[key].To)
            {
                changes.Append(oldVal).Append("<br/>");
            }
            changes.Append("</td></tr></tbody></table>");
        }


        return changes.ToString();
    }
}

public enum ChangeType
{
    USERNAME,
    PASSWORD,
    EMAIL,
    REGIONS,
    PHONE,
    ROLES,
    ACTIVATION,
    LASTNAME,
    FIRSTNAME
}

public class ChangeTypeInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsList { get; set; }
    public bool IsBoolean { get; set; }
}

public static class ChangeTypeExtensions
{
    private static readonly Dictionary<ChangeType, ChangeTypeInfo> ChangeTypeMap = new()
    {
        { ChangeType.ACTIVATION, new ChangeTypeInfo { DisplayName = "Account Activated", IsBoolean = true, IsList = false } },
        { ChangeType.EMAIL, new ChangeTypeInfo { DisplayName = "EMail", IsBoolean = false, IsList = false } },
        { ChangeType.FIRSTNAME, new ChangeTypeInfo { DisplayName = "First Name", IsBoolean = false, IsList = false } },
        { ChangeType.LASTNAME, new ChangeTypeInfo { DisplayName = "Last Name", IsBoolean = false, IsList = false } },
        { ChangeType.PASSWORD, new ChangeTypeInfo { DisplayName = "Password", IsBoolean = false, IsList = false } },
        { ChangeType.REGIONS, new ChangeTypeInfo { DisplayName = "Assigned Regions", IsBoolean = false, IsList = true } },
        { ChangeType.ROLES, new ChangeTypeInfo { DisplayName = "Assigned Roles", IsBoolean = false, IsList = true } },
        { ChangeType.USERNAME, new ChangeTypeInfo { DisplayName = "Username", IsBoolean = false, IsList = false } },
    };

    public static ChangeTypeInfo GetChangeTypeInfo(this ChangeType changeType) => ChangeTypeMap[changeType];
}



public class BooleanChangeType
{
    public bool From { get; set; }
    public bool To { get; set; }

    public BooleanChangeType(bool from, bool to)
    {
        this.From = from;
        this.To = to;
    }

}

public class SingleChangeType
{
    public string From = string.Empty;
    public string To = string.Empty;

    public SingleChangeType(string from, string to)
    {
        this.From = from ?? throw new ArgumentNullException(nameof(from));
        this.To = to ?? throw new ArgumentNullException(nameof(to));
    }
}


public class ListChangeType
{

    public IEnumerable<string> From = new List<string>();
    public IEnumerable<string> To = new List<string>();

    public ListChangeType(IEnumerable<string> from, IEnumerable<string> to)
    {
        this.From = from ?? throw new ArgumentNullException(nameof(from));
        this.To = to ?? throw new ArgumentNullException(nameof(to));
    }
}
