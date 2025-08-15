using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Services.Other;

namespace NoMercyBot.Services.Twitch;

public enum CommandPermission
{
    Broadcaster,
    Moderator,
    Vip,
    Subscriber,
    Everyone
}

public enum CommandType
{
    Command,
    Event,
    Message
}

public class CommandContext
{
    public string Channel { get; set; } = null!;
    public string BroadcasterId { get; set; } = null!;
    public string CommandName { get; set; } = null!;
    public string[] Arguments { get; set; } = [];
    public ChatMessage Message { get; set; } = null!;
    public Func<string, Task> ReplyAsync { get; set; } = null!;
    public required AppDbContext DatabaseContext { get; init; } = null!;
    public required TwitchCommandService CommandService { get; set; } = null!;
    public required TwitchApiService TwitchApiService { get; set; } = null!;
    public required IServiceProvider ServiceProvider { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
}

public class ChatCommand
{
    public string Name { get; set; } = null!;
    public CommandPermission Permission { get; set; } = CommandPermission.Everyone;
    public CommandType Type { get; set; } = CommandType.Command;
    public object? Storage { get; set; }
    public Func<CommandContext, Task> Callback { get; set; } = null!;
}

public class TwitchCommandService
{
    private static readonly ConcurrentDictionary<string, ChatCommand> Commands = new();
    private readonly ILogger<TwitchCommandService> _logger;
    private readonly AppDbContext _appDbContext;
    private readonly TwitchChatService _twitchChatService;
    private readonly TwitchApiService _twitchApiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PermissionService _permissionService;

    public TwitchCommandService(
        AppDbContext appDbContext, 
        TwitchChatService twitchChatService, 
        TwitchApiService twitchApiService,
        PermissionService permissionService,
        IServiceScopeFactory scopeFactory,
        ILogger<TwitchCommandService> logger)
    {
        _logger = logger;
        _appDbContext = appDbContext;
        _twitchChatService = twitchChatService;
        _twitchApiService = twitchApiService;
        IServiceScope scope = scopeFactory.CreateScope();
        _serviceProvider = scope.ServiceProvider;
        _permissionService = permissionService;
        LoadCommandsFromDatabase();
    }

    private void LoadCommandsFromDatabase()
    {
        List<Command> dbCommands = _appDbContext.Commands.Where(c => c.IsEnabled).ToList();
        foreach (Command dbCommand in dbCommands)
        {
            RegisterCommand(new()
            {
                Name = dbCommand.Name,
                Permission = Enum.TryParse<CommandPermission>(dbCommand.Permission, true, out CommandPermission perm) ? perm : CommandPermission.Everyone,
                Type = Enum.TryParse<CommandType>(dbCommand.Type, true, out CommandType type) ? type : CommandType.Command,
                Callback = async ctx => await ctx.ReplyAsync(dbCommand.Response)
            });
        }
    }

    public bool RegisterCommand(ChatCommand command)
    {
        Commands.TryAdd(command.Name.ToLowerInvariant(), command);
        
        _logger.LogInformation($"Registered/Updated command: {command.Name}");
        return true;
    }

    public bool RemoveCommand(string commandName)
    {
        return Commands.TryRemove(commandName.ToLowerInvariant(), out _);
    }

    public bool UpdateCommand(ChatCommand command)
    {
        Commands[command.Name.ToLowerInvariant()] = command;
        return true;
    }

    public IEnumerable<ChatCommand> ListCommands() => Commands.Values;

    public async Task ExecuteCommand(ChatMessage message)
    {
        if (!message.IsCommand || string.IsNullOrWhiteSpace(message.Message))
            return;
        
        ChatMessageFragment commandFragment = message.Fragments.First();

        if (Commands.TryGetValue(commandFragment.Command!, out ChatCommand? command))
        {
            if(!_permissionService.HasMinLevel(message.UserType, command.Permission.ToString().ToLowerInvariant())) return;
            
            CommandContext context = new()
            {
                Channel = message.BroadcasterId,
                BroadcasterId = message.BroadcasterId,
                CommandName = commandFragment.Command!,
                Arguments = commandFragment.Args!,
                Message = message,
                CommandService = this,
                ServiceProvider = _serviceProvider,
                TwitchApiService = _twitchApiService,
                DatabaseContext = _appDbContext,
                ReplyAsync = async (reply) =>
                {
                    _logger.LogInformation($"Reply to {message.Username}: {reply}");
                    await _twitchChatService.SendMessageAsBot(message.Broadcaster.Username, reply);
                    await Task.CompletedTask;
                }
            };
            await command.Callback(context);
        }
        else
        {
            _logger.LogDebug($"Unknown command: {commandFragment.Command!}");
        }
    }

    public async Task AddOrUpdateUserCommandAsync(string name, string response, string permission = "everyone", string type = "command", bool isEnabled = true, string? description = null)
    {
        Command? dbCommand = await _appDbContext.Commands.FirstOrDefaultAsync(c => c.Name == name);
        if (dbCommand == null)
        {
            dbCommand = new()
            {
                Name = name,
                Response = response,
                Permission = permission,
                Type = type,
                IsEnabled = isEnabled,
                Description = description
            };
            await _appDbContext.Commands.AddAsync(dbCommand);
        }
        else
        {
            dbCommand.Name = name;
            dbCommand.Response = response;
            dbCommand.Permission = permission;
            dbCommand.Type = type;
            dbCommand.IsEnabled = isEnabled;
            dbCommand.Description = description;
            _appDbContext.Commands.Update(dbCommand);
        }
        
        await _appDbContext.SaveChangesAsync();
        
        RegisterCommand(new()
        {
            Name = dbCommand.Name,
            Permission = Enum.TryParse<CommandPermission>(dbCommand.Permission, true, out CommandPermission perm) ? perm : CommandPermission.Everyone,
            Type = Enum.TryParse<CommandType>(dbCommand.Type, true, out CommandType commandType) ? commandType : CommandType.Command,
            Callback = async ctx => await ctx.ReplyAsync(dbCommand.Response)
        });
    }

    public async Task<bool> RemoveUserCommandAsync(string name)
    {
        Command? dbCommand = await _appDbContext.Commands.FirstOrDefaultAsync(c => c.Name == name);
        if (dbCommand == null) return false;
        _appDbContext.Commands.Remove(dbCommand);
        await _appDbContext.SaveChangesAsync();
        RemoveCommand(name);
        return true;
    }
}
