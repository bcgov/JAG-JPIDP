namespace CommonModels.Models.DIAMAdmin;


public class ChartDataModel
{
    public string DataName { get; set; } = "No name";
    public List<ChartDataInstance> Data { get; set; } = [];


    public List<string> GetLabels()
    {
        if (this.Data == null || this.Data.Count == 0)
        {
            return [];
        }
        return [.. this.Data.Select(d => d.Label).Distinct().OrderBy(x => x)];
    }

    public ChartDataInstance GetDataForLabel(string label)
    {
        if (this.Data == null || this.Data.Count == 0 || !this.Data.Any(d => d.Label == label))
        {
            return new ChartDataInstance();
        }
        return this.Data.FirstOrDefault(d => d.Label == label);
    }
}

public class ChartDataInstance
{
    public double Value { get; set; }
    public string Label { get; set; } = "No label";
    public string Colour { get; set; } = string.Empty;
}
