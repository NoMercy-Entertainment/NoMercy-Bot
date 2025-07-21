using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace NoMercyBot.Database.Models.ChatMessage;

[NotMapped]
public class ChatMention
{
    [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)] public string UserId { get; set; } = string.Empty;
    [JsonProperty("user_name", NullValueHandling = NullValueHandling.Ignore)] public string UserName { get; set; } = string.Empty;
    [JsonProperty("user_login", NullValueHandling = NullValueHandling.Ignore)] public string UserLogin { get; set; } = string.Empty;

    public ChatMention()
    {
        
    }
    
    public ChatMention(TwitchLib.EventSub.Core.Models.Chat.ChatMention fragmentMention)
    {
        UserId = fragmentMention.UserId;
        UserName = fragmentMention.UserName;
        UserLogin = fragmentMention.UserLogin;
    }
}
