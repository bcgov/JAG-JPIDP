namespace NotificationService.Services;

using NotificationService.Exceptions;
using NotificationService.Models;
using Serilog;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

public class LocalEMailTemplateCache : IEmailTemplateCache, IDisposable
{

    private readonly IDictionary<string, EMailTemplateModel> templateCache = new Dictionary<string, EMailTemplateModel>();
    private readonly FileSystemWatcher fileWatcher;
    private readonly NotificationServiceConfiguration config;

    public LocalEMailTemplateCache(NotificationServiceConfiguration config)
    {
        this.config = config;

        // check template folder is valid and readable
        Log.Information($"Starting template file watcher for [{this.config.ChesClient.TemplateFolder}]...");

        Log.Information($"Current folder {Directory.GetCurrentDirectory()}.");

        if (Directory.Exists(this.config.ChesClient.TemplateFolder) && CanRead(this.config.ChesClient.TemplateFolder))
        {
            this.fileWatcher = new FileSystemWatcher(this.config.ChesClient.TemplateFolder)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.LastWrite
            };
            this.fileWatcher.Changed += this.OnTemplateFileChanged;
            this.fileWatcher.EnableRaisingEvents = true;
        }
        else
        {
            Log.Error($"EMail template folder [{this.config.ChesClient.TemplateFolder}] does not exist or is not readable - please resolve and restart");
            Environment.Exit(101);
        }
    }

    private async void OnTemplateFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!this.templateCache.ContainsKey(e.Name))
        {
            return;
        }

        await Task.Delay(5000);

        Log.Information($"Change detectected to email template file [{e.Name}]");

        var templatePath = Path.Combine(this.config.ChesClient.TemplateFolder, e.Name);
        var templateContent = await File.ReadAllTextAsync(templatePath);
        var templateContentStr = await File.ReadAllTextAsync(templatePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        var domainEventName = Path.ChangeExtension(e.Name, null);

        var templateContentObj = deserializer.Deserialize<EMailTemplateModel>(templateContentStr);
        this.templateCache[domainEventName] = templateContentObj;
    }


    public async Task<EMailTemplateModel> GetEmailTemplate(string identifier)
    {
        if (this.templateCache.TryGetValue(identifier, out var templateContent))
        {
            return templateContent;
        }

        var templatePath = Path.Combine(this.config.ChesClient.TemplateFolder, identifier + ".yaml");

        if (!File.Exists(templatePath))
        {
            throw new EMailTemplateException($"No matching template found for event {identifier}");
        }

        var templateContentStr = await File.ReadAllTextAsync(templatePath);
        var deserializer = new DeserializerBuilder()
           .WithNamingConvention(UnderscoredNamingConvention.Instance)
           .Build();

        var templateContentObj = deserializer.Deserialize<EMailTemplateModel>(templateContentStr);
        this.templateCache[identifier] = templateContentObj;
        return templateContentObj;
    }

    private static bool CanRead(string folderPath)
    {
        try
        {
            Directory.GetFiles(folderPath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        this.fileWatcher?.Dispose();
    }
}
