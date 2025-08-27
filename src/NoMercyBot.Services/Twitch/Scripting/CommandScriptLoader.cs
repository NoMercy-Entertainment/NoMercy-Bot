using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Globals.Information;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Other;

namespace NoMercyBot.Services.Twitch.Scripting;

public class CommandScriptLoader
{
    private readonly TwitchCommandService _commandService;
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchApiService _twitchApiService;
    private readonly TtsService _ttsService;
    private readonly ILogger<CommandScriptLoader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppDbContext _appDbContext;

    public CommandScriptLoader(
        TwitchCommandService commandService,
        TwitchChatService twitchChatService,
        TwitchApiService twitchApiService,
        TtsService ttsService,
        AppDbContext appDbContext,
        ILogger<CommandScriptLoader> logger,
        IServiceScopeFactory scopeFactory)
    {
        _commandService = commandService;
        _twitchChatService = twitchChatService;
        _twitchApiService = twitchApiService;
        _ttsService = ttsService;
        _appDbContext = appDbContext;
        _logger = logger;
        IServiceScope scope = scopeFactory.CreateScope();
        _serviceProvider = scope.ServiceProvider;
    }

    public async Task LoadAllAsync()
    {
        await Parallel.ForEachAsync(Directory.GetFiles(AppFiles.CommandsPath, "*.cs"),
            async (file, _) => { await LoadScriptAsync(file); });
    }

    private async Task LoadScriptAsync(string filePath)
    {
        string scriptCode = await File.ReadAllTextAsync(filePath);
        string commandName = Path.GetFileNameWithoutExtension(filePath);
        try
        {
            ScriptOptions options = ScriptOptions.Default;

            IEnumerable<string> assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location);

            options = options.AddReferences(assemblies);

            ICommand command = await CSharpScript.EvaluateAsync<ICommand>(scriptCode, options);

            ChatCommand chatCommand = new()
            {
                Name = command.Name,
                Permission = command.Permission,
                Callback = async ctx =>
                {
                    CommandScriptContext scriptCtx = new()
                    {
                        Channel = ctx.Message.Broadcaster.Username,
                        BroadcasterId = ctx.BroadcasterId,
                        CommandName = ctx.CommandName,
                        Arguments = ctx.Arguments,
                        Message = ctx.Message,
                        ReplyAsync = ctx.ReplyAsync,
                        CancellationToken = ctx.CancellationToken,
                        DatabaseContext = ctx.DatabaseContext,
                        ServiceProvider = ctx.ServiceProvider,
                        TwitchChatService = ctx.TwitchChatService,
                        TwitchApiService = ctx.TwitchApiService,
                        TtsService = ctx.TtsService
                    };

                    await command.Callback(scriptCtx);
                }
            };

            CommandScriptContext scriptCtx = new()
            {
                DatabaseContext = new(),
                ServiceProvider = _serviceProvider,
                TwitchChatService = _twitchChatService,
                TwitchApiService = _twitchApiService,
                TtsService = _ttsService
            };

            await command.Init(scriptCtx);

            _commandService.RegisterCommand(chatCommand);

            _logger.LogInformation($"Loaded command script: {commandName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load command script: {filePath}");
        }
    }
}