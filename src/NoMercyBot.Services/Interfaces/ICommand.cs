using NoMercyBot.Services.Twitch;
using NoMercyBot.Services.Twitch.Scripting;

namespace NoMercyBot.Services.Interfaces;

public interface ICommand
{
    string Name { get; }
    CommandPermission Permission { get; }
    Task Init(CommandScriptContext ctx);
    Task Callback(CommandScriptContext ctx);
}