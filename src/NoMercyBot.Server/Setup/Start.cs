using System.Runtime.InteropServices;
using NoMercyBot.Database.Seeds;
using NoMercyBot.Globals.Information;

namespace NoMercyBot.Server.Setup;

public class Start
{
    [DllImport("Kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("User32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

    public static int ConsoleVisible { get; set; } = 1;

    public static void VsConsoleWindow(int i)
    {
        IntPtr hWnd = GetConsoleWindow();
        if (hWnd != IntPtr.Zero)
        {
            ConsoleVisible = i;
            ShowWindow(hWnd, i);
        }
    }

    public static async Task Init(List<TaskDelegate> tasks)
    {
        if (UserSettings.TryGetUserSettings(out Dictionary<string, string>? settings))
            UserSettings.ApplySettings(settings);

        List<TaskDelegate> startupTasks =
        [
            new(AppFiles.CreateAppFolders),
            new(Seed.Init),
            ..tasks,

            new(delegate
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
                    return TrayIcon.Make();
                return Task.CompletedTask;
            }),
            new(delegate
            {
                DesktopIconCreator.CreateDesktopIcon(Info.ApplicationName, AppFiles.ServerExePath,
                    AppFiles.AppIcon);
                return Task.CompletedTask;
            })
        ];

        await RunStartup(startupTasks);

        // Thread queues = new(new Task(() => QueueRunner.Initialize().Wait()).Start)
        // {
        //     Name = "Queue workers",
        //     Priority = ThreadPriority.Lowest,
        //     IsBackground = true
        // };
        // queues.Start();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
        {
            // Logger.App(
            //     "Your server is ready and we will hide the console window in 10 seconds you can show it again by right-clicking the tray icon");
            // Task.Delay(10000)
            //     .ContinueWith(_ => VsConsoleWindow(0));
            VsConsoleWindow(0);
        }
    }

    private static async Task RunStartup(List<TaskDelegate> startupTasks)
    {
        foreach (TaskDelegate task in startupTasks) await task.Invoke();
    }
}