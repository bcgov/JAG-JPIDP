namespace Common.Utils;
public class AppInfo
{

    public string GetAssemblyVersion() => this.GetType().Assembly.GetName().Version.ToString();

}
