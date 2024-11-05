namespace edt.casemanagement.Models;

public class KeyIdPair
{
    public string Key { get; set; } = string.Empty;
    public int Id { get; set; }


    public override string ToString() => $"Key: {this.Key}, Id: {this.Id}";
}
