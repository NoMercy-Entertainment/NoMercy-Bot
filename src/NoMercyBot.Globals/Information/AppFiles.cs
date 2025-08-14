using NoMercyBot.Globals.SystemCalls;

namespace NoMercyBot.Globals.Information;

public static class AppFiles
{
    public static readonly string AppDataPath = Environment.OSVersion.Platform == PlatformID.Unix
        ? Path.Combine(
            Environment.GetEnvironmentVariable("HOME") ?? "/home/current",
            ".local/share")
        : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string AppPath => Path.Combine(AppDataPath, "NoMercyBot");

    public static string ConfigPath => Path.Combine(AppPath, "config");
    public static string TokenFile => Path.Combine(ConfigPath, "token.json");

    public static string DataPath => Path.Combine(AppPath, "data");
    public static string LogPath => Path.Combine(AppPath, "log");
    public static string CommandsPath => Path.Combine(AppPath, "commands");
    public static string WidgetsPath => Path.Combine(AppPath, "widgets");

    public static string CachePath => Path.Combine(AppPath, "cache");
    public static string ServerExePath => Path.Combine(AppPath, "NoMercyBot" + Info.ExecSuffix);
    
    public static string AppIcon => Path.Combine(Directory.GetCurrentDirectory(), "Assets/icon" + Info.IconSuffix);

    public static string DatabaseFile => Path.Combine(DataPath, "database.sqlite");

    public static IEnumerable<string> AllPaths()
    {
        return
        [
            AppDataPath,
            AppPath,
            CachePath,
            ConfigPath,
            DataPath,
            LogPath,
            CommandsPath,
            WidgetsPath,
        ];
    }

    public static Task CreateAppFolders()
    {
        if (!Directory.Exists(AppPath))
            Directory.CreateDirectory(AppPath);

        foreach (string path in AllPaths().Where(path => !Directory.Exists(path)))
        {
            Logger.Setup($"Creating directory: {path}");
            Directory.CreateDirectory(path);
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Set appropriate Unix permissions (755)
                DirectoryInfo dirInfo = new(path)
                {
                    UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                   UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                   UnixFileMode.OtherRead | UnixFileMode.OtherExecute
                };
            }
        }

        return Task.CompletedTask;
    }
}