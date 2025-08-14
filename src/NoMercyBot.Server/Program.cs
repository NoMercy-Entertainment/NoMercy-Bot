using System.Net;
using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CommandLine;
using Microsoft.AspNetCore;
using NoMercyBot.Globals.Information;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Server.Setup;
using NoMercyBot.Services;

namespace NoMercyBot.Server;

public static class Program
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            Exception exception = (Exception)eventArgs.ExceptionObject;
        };
        
        Console.CancelKeyPress += (_, eventArgs) => 
        {
            eventArgs.Cancel = true;
            Logger.App("Shutting down gracefully...");
            CancellationTokenSource.Cancel();
        };
        
        AppDomain.CurrentDomain.ProcessExit += (_, _) => 
        {
            CancellationTokenSource.Cancel();
        };

        await Parser.Default.ParseArguments<StartupOptions>(args)
            .MapResult(Start, ErrorParsingArguments);
        
        static Task ErrorParsingArguments(IEnumerable<Error> errors)
        {
            Environment.ExitCode = 1;
            return Task.CompletedTask;
        }
    }

    private static async Task Start(StartupOptions options)
    {
        Console.Clear();
        Console.Title = "NoMercyBot Server";

        options.ApplySettings();
        
        Version version = Assembly.GetExecutingAssembly().GetName().Version!;
        Software.Version = version;
        Logger.App($"NoMercyBot version: v{version.Major}.{version.Minor}.{version.Build}");
        
        List<TaskDelegate> startupTasks =
        [
            
        ];

        await Setup.Start.Init(startupTasks);
        
        IWebHost app = CreateWebHostBuilder(options).Build();
        
        try
        {
            await app.RunAsync(CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Logger.App("Application shutdown completed.");
        }
    }

    private static IWebHostBuilder CreateWebHostBuilder(StartupOptions options)
    {
        UriBuilder localhostIPv4Url = new()
        {
            Host = IPAddress.Any.ToString(),
            Port = 6037,
            Scheme = Uri.UriSchemeHttp
        };

        List<string> urls = [localhostIPv4Url.ToString()];

        return WebHost.CreateDefaultBuilder([])
            .ConfigureServices(services =>
            {
                services.AddSingleton<StartupOptions>(options);
                services.AddSingleton<IApiVersionDescriptionProvider, DefaultApiVersionDescriptionProvider>();
                services.AddSingleton<ISunsetPolicyManager, DefaultSunsetPolicyManager>();
                // Add custom logging here to ensure it's available during startup
                services.AddSingleton(typeof(ILogger<>), typeof(CustomLogger<>));
            })
            .ConfigureLogging(logging =>
            {
                // logging.ClearProviders();
            })
            .UseUrls(urls.ToArray())
            .UseKestrel(options =>
            {
                options.AddServerHeader = false;
                options.Limits.MaxRequestBodySize = null;
                options.Limits.MaxRequestBufferSize = null;
                options.Limits.MaxConcurrentConnections = null;
                options.Limits.MaxConcurrentUpgradedConnections = null;
            })
            .UseQuic()
            .UseSockets()
            .UseStartup<Startup>();
    }
}