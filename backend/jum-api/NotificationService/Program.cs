namespace NotificationService;


using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;
using System.Reflection;

public class Program
{
    public static int Main(string[] args)
    {
        CreateLogger();

        try
        {
            Log.Information("Starting web host");
            var builder = CreateHostBuilder(args);
            builder
                .Build()
                .Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            // Ensure buffered logs are written to their target sink
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
            .UseSerilog();

    private static void CreateLogger()
    {

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var path = Environment.GetEnvironmentVariable("LogFilePath") ?? "logs";

        var config = new ConfigurationBuilder()
         .AddJsonFile("appsettings.json", optional: true)
         .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
         .Build();

        var seqEndpoint = Environment.GetEnvironmentVariable("Seq__Url");
        seqEndpoint ??= config.GetValue<string>("Seq:Url");

        var splunkHost = Environment.GetEnvironmentVariable("SplunkConfig__Host");
        splunkHost ??= config.GetValue<string>("SplunkConfig:Host");
        var splunkToken = Environment.GetEnvironmentVariable("SplunkConfig__CollectorToken");
        splunkToken ??= config.GetValue<string>("SplunkConfig:CollectorToken");


        if (string.IsNullOrEmpty(seqEndpoint))
        {
            Console.WriteLine("SEQ Log Host is not configured - check Seq environment");
            Environment.Exit(100);
        }

        try
        {
            if (NotificationServiceConfiguration.IsDevelopment())
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Creating the logging directory failed: {0}", e.ToString());
        }

        var name = Assembly.GetExecutingAssembly().GetName();
        var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Assembly", $"{name.Name}")
            .Enrich.WithProperty("Version", $"{name.Version}")
            .WriteTo.Console(
                outputTemplate: outputTemplate,
                theme: AnsiConsoleTheme.Code)
            .WriteTo.Async(a => a.File(
                $@"{path}/notification.log",
                outputTemplate: outputTemplate,
                rollingInterval: RollingInterval.Day,
                shared: true))
            .WriteTo.Async(a => a.File(
                new JsonFormatter(),
                $@"{path}/notification.json",
                rollingInterval: RollingInterval.Day));

        if (!string.IsNullOrEmpty(splunkHost))
        {
            loggerConfiguration.WriteTo.EventCollector(splunkHost, splunkToken);
        }


        Log.Logger = loggerConfiguration.CreateLogger();

        if (string.IsNullOrEmpty(splunkHost))
        {
            Log.Warning("*** Splunk Host is not configured - check Splunk environment *** ");
        }
        else
        {
            Log.Information($"*** Splunk logging to {splunkHost} ***");

        }
    }
}
