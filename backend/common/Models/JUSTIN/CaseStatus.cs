namespace Common.Models.JUSTIN;


/// <summary>
/// Represents a Case Status value for a case in JUSTIN
/// </summary>
public class CaseStatus(string value, bool demsCandidate, string description)
{
    public string Value { get; private set; } = value;
    public bool DemsCandidate { get; private set; } = demsCandidate;
    public string Description { get; private set; } = description;

    public static CaseStatus Build => new("BLD", false, "Build State");
    public static CaseStatus Submit => new("SUB", true, "Submitted");
    public static CaseStatus Accepted => new("ACT", true, "Accepted");
    public static CaseStatus Returned => new("RET", true, "Returned");
    public static CaseStatus Finish => new("FIN", false, "Finish");
    public static CaseStatus Close => new("CLS", false, "Close");
    public static CaseStatus Approval => new("APR", false, "Approval");
    public static CaseStatus Apply => new("APY", false, "Apply (processing)");
    public static CaseStatus Rejected => new("REJ", false, "Rejected");
    public static CaseStatus Deleted => new("DEL", false, "Deleted");
    public static CaseStatus NotFound => new("", false, "Not Found");


    public static CaseStatus GetByValue(string value)
    {
        return value.ToUpper(System.Globalization.CultureInfo.CurrentCulture) switch
        {
            "BLD" => Build,
            "SUB" => Submit,
            "ACT" => Accepted,
            "RET" => Returned,
            "FIN" => Finish,
            "CLS" => Close,
            "APR" => Approval,
            "APY" => Apply,
            "REJ" => Rejected,
            "DEL" => Deleted,
            _ => NotFound,
        };
    }

}

