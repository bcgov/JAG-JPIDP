namespace Pidp.Models;

using System;

public class UserChangeModel
{
    public string UserID { get; set; }
    public Dictionary<ChangeType, SingleChangeType> SingleChangeTypes { get; set; } =  new Dictionary<ChangeType, SingleChangeType>();
    public Dictionary<ChangeType, ListChangeType> ListChangeTypes { get; set; } = new Dictionary<ChangeType, ListChangeType>();
    public Dictionary<ChangeType, BooleanChangeType> BooleanChangeTypes { get; set; } = new Dictionary<ChangeType, BooleanChangeType>();

}

public enum ChangeType
{
    USERNAME,
    PASSWORD,
    EMAIL,
    REGIONS,
    ROLES,
    ACTIVATION,
    LASTNAME,
    FIRSTNAME
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
