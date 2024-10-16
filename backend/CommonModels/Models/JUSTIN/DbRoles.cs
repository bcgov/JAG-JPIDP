namespace CommonModels.Models.JUSTIN;


public class DbRoles
{
    public List<DbRole> Roles { get; set; } = [];

    public override string ToString() => $"DbRoles: {string.Join(", ", this.Roles)}";
}

public class DbRole
{
    public string? Dbroles { get; set; } = string.Empty;
    public double Part_id { get; set; }

    public override string ToString() => $"DbRole: Dbroles={this.Dbroles}, Part_id={this.Part_id}";
}
