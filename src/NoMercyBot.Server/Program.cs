using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CommandLine;
using Microsoft.AspNetCore;
using NoMercyBot.Globals.Information;
using NoMercyBot.Globals.SystemCalls;
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
            Logger.App("Shutting down gracefully (Ctrl+C)...");
            CancellationTokenSource.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            CancellationTokenSource.Cancel();
        };

        // SetConsoleCtrlHandler catches the close-button / `taskkill` (without /F) /
        // `Stop-Process` (without -Force) cases that Console.CancelKeyPress misses.
        // Without this, the bot is hard-killed before its hosted services'
        // StopAsync runs — meaning the EventSub WS never sends its close frame and
        // Twitch's edge keeps a phantom session for ~30s, which compounds across
        // restarts into the 3-connections-per-client_id limit.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ctrlHandlerKeepAlive = HandleConsoleCtrl;
            SetConsoleCtrlHandler(_ctrlHandlerKeepAlive, true);
        }

        await Parser
            .Default.ParseArguments<StartupOptions>(args)
            .MapResult(Start, ErrorParsingArguments);

        static Task ErrorParsingArguments(IEnumerable<Error> errors)
        {
            Environment.ExitCode = 1;
            return Task.CompletedTask;
        }
    }

    private delegate bool ConsoleCtrlHandler(int ctrlType);
    private static ConsoleCtrlHandler? _ctrlHandlerKeepAlive;

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

    private const int CTRL_C_EVENT = 0;
    private const int CTRL_BREAK_EVENT = 1;
    private const int CTRL_CLOSE_EVENT = 2;
    private const int CTRL_LOGOFF_EVENT = 5;
    private const int CTRL_SHUTDOWN_EVENT = 6;

    private static bool HandleConsoleCtrl(int ctrlType)
    {
        // Second signal while shutdown is already in progress → user is mashing
        // Ctrl+C because the host is hung. Force-exit immediately.
        if (CancellationTokenSource.IsCancellationRequested)
        {
            Logger.App("Force-exiting (second signal received)");
            Environment.Exit(1);
            return true;
        }

        Logger.App($"Shutting down gracefully (signal: {ctrlType})...");
        CancellationTokenSource.Cancel();

        // Schedule a hard-exit fallback. If the host hasn't gracefully returned in
        // 10s (some hosted service hung, or Kestrel waiting on widget WS clients),
        // kill the process anyway so the user isn't stuck pressing Ctrl+C.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (_runTask?.IsCompleted != true)
            {
                Logger.App("Graceful shutdown took too long, force-exiting");
                Environment.Exit(1);
            }
        });

        switch (ctrlType)
        {
            case CTRL_CLOSE_EVENT:
            case CTRL_LOGOFF_EVENT:
            case CTRL_SHUTDOWN_EVENT:
                // Windows force-terminates ~5s after these signals. Block here long
                // enough to let hosted services stop + EventSub WS close-frame fly,
                // then return true so we own the exit instead of being killed mid-flight.
                try
                {
                    _runTask?.Wait(TimeSpan.FromSeconds(4));
                }
                catch { }
                return true;

            default:
                // Ctrl+C / Ctrl+Break: returning true here would suppress .NET's
                // normal exit path. Cancel the CTS (already done above) and return
                // false so the default handler also runs and the process exits.
                return false;
        }
    }

    private static Task? _runTask;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(
        IntPtr hWnd,
        int x,
        int y,
        int width,
        int height,
        bool repaint
    );

    private static async Task Start(StartupOptions options)
    {
        try
        {
            Console.Clear();
            Console.Title = "NoMercyBot Server";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr handle = GetForegroundWindow();
                if (handle != IntPtr.Zero)
                    MoveWindow(handle, 0, 0, 1920, 1080, true);
            }
        }
        catch (IOException)
        {
            // No console attached (e.g. running from IDE or piped output)
        }

        options.ApplySettings();

        Version version = Assembly.GetExecutingAssembly().GetName().Version!;
        Software.Version = version;
        Logger.App($"NoMercyBot version: v{version.Major}.{version.Minor}.{version.Build}");

        IWebHost app = CreateWebHostBuilder(options).Build();

        try
        {
            _runTask = app.RunAsync(CancellationTokenSource.Token);
            await _runTask;
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
            Scheme = Uri.UriSchemeHttp,
        };

        List<string> urls = [localhostIPv4Url.ToString()];

        return WebHost
            .CreateDefaultBuilder([])
            .ConfigureServices(services =>
            {
                services.AddSingleton<StartupOptions>(options);
                services.AddSingleton<
                    IApiVersionDescriptionProvider,
                    DefaultApiVersionDescriptionProvider
                >();
                services.AddSingleton<ISunsetPolicyManager, DefaultSunsetPolicyManager>();
                // Add custom logging here to ensure it's available during startup
                services.AddSingleton(typeof(ILogger<>), typeof(CustomLogger<>));
            })
            .UseUrls(urls.ToArray())
            .UseKestrel(options =>
            {
                options.AddServerHeader = false;
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
                options.Limits.MaxRequestBufferSize = 1024 * 1024; // 1 MB
                options.Limits.MaxConcurrentConnections = 1000;
                options.Limits.MaxConcurrentUpgradedConnections = 200;
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            })
            .UseQuic()
            .UseSockets()
            .UseStartup<Startup>();
    }
}
