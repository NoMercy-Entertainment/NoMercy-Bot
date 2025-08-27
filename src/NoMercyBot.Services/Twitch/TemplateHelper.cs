using System.Text.RegularExpressions;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Services.Twitch.Scripting;

namespace NoMercyBot.Services.Twitch;

public static class TemplateHelper
{
    public static string ReplaceTemplatePlaceholders(string template, CommandScriptContext ctx, bool? isLive = null,
        string? gameName = null, string? title = null)
    {
        ChatMessage message = ctx.Message;
        string result = template;

        // Basic name replacement
        result = Regex.Replace(result, @"\{name\}", message.DisplayName, RegexOptions.IgnoreCase);

        // Get pronouns (assuming these are available on the user object)
        string subjectPronoun = message.User?.Pronoun?.Subject ?? "They";
        string objectPronoun = message.User?.Pronoun?.Object ?? "them";

        // Determine verb forms based on pronouns
        string beVerb = subjectPronoun.ToLower() switch
        {
            "he" or "she" => "is",
            "they" => "are",
            _ => "is"
        };

        string wasVerb = subjectPronoun.ToLower() switch
        {
            "he" or "she" => "was",
            "they" => "were",
            _ => "was"
        };

        string genderedTerm = subjectPronoun.ToLower() switch
        {
            "he" => "dude",
            "she" => "dudette",
            _ => "friend"
        };

        // Verb tense replacements
        result = Regex.Replace(result, @"\{presentTense\}", beVerb.ToLower(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{PresentTense\}", beVerb, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{pastTense\}", wasVerb.ToLower(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{PastTense\}", wasVerb, RegexOptions.IgnoreCase);

        if (isLive.HasValue)
        {
            result = Regex.Replace(result, @"\{tense\}", isLive.Value ? beVerb.ToLower() : wasVerb.ToLower(),
                RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{Tense\}", isLive.Value ? beVerb : wasVerb, RegexOptions.IgnoreCase);
        }

        // Pronoun replacements
        result = Regex.Replace(result, @"\{subject\}", subjectPronoun.ToLower(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{Subject\}", subjectPronoun, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{object\}", objectPronoun.ToLower(), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{Object\}", char.ToUpper(objectPronoun[0]) + objectPronoun.Substring(1),
            RegexOptions.IgnoreCase);

        // Gendered term replacements
        result = Regex.Replace(result, @"\{GenderedTerm\}", char.ToUpper(genderedTerm[0]) + genderedTerm.Substring(1),
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{genderedTerm\}", genderedTerm.ToLower(), RegexOptions.IgnoreCase);

        // Game and stream info
        result = Regex.Replace(result, @"\{game\}", gameName ?? "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{title\}", title ?? "", RegexOptions.IgnoreCase);

        // User info replacements
        result = Regex.Replace(result, @"\{link\}", $"https://www.twitch.tv/{message.User.Username}",
            RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{username\}", message.User.Username, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{displayname\}", message.User.DisplayName, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{id\}", message.User.Id, RegexOptions.IgnoreCase);

        if (isLive.HasValue)
        {
            result = Regex.Replace(result, @"\{status\}", isLive.Value ? "live" : "offline", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\{Status\}", isLive.Value ? "Live" : "Offline", RegexOptions.IgnoreCase);
        }

        return result;
    }

    public static string ReplaceTemplatePlaceholders(string template, RewardScriptContext ctx, bool? isLive = null,
        string? gameName = null, string? title = null)
    {
        CommandScriptContext commandCtx = new()
        {
            Message = new()
            {
                User = ctx.User,
                DisplayName = ctx.UserDisplayName,
                Id = ctx.UserId
            },
            DatabaseContext = ctx.DatabaseContext,
            TwitchApiService = ctx.TwitchApiService,
            TwitchChatService = ctx.TwitchChatService,
            ServiceProvider = ctx.ServiceProvider
        };

        return ReplaceTemplatePlaceholders(template, commandCtx, isLive, gameName, title);
    }
}