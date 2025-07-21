using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets.Core.Models;

// ReSharper disable MemberCanBePrivate.Global

namespace NoMercyBot.Database.Models.ChatMessage;

[PrimaryKey(nameof(Id))]
public class ChatMessage: Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)] [MaxLength(255)] public string Id { get; set; }
    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)] public Color? Color { get; set; }
    [JsonProperty("badges", NullValueHandling = NullValueHandling.Ignore)] public List<KeyValuePair<string, string>>? Badges { get; set; }
    
    [JsonProperty("fragments", NullValueHandling = NullValueHandling.Ignore)] public List<ChatMessageFragment> Fragments  { get; set; }
    [JsonProperty("message_node", NullValueHandling = NullValueHandling.Ignore)] public MessageNode MessageNode { get; set; }
    
    [JsonProperty("is_highlighted", NullValueHandling = NullValueHandling.Ignore)] public bool IsHighlighted { get; set; }
    [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)] public string Message { get; set; }
    [JsonProperty("tmi_sent_ts", NullValueHandling = NullValueHandling.Ignore)] public string TmiSentTs { get; set; }
    
    [JsonProperty("color_hex", NullValueHandling = NullValueHandling.Ignore)] public string ColorHex { get; set; }
    [JsonProperty("display_name", NullValueHandling = NullValueHandling.Ignore)] public string DisplayName { get; set; }
    [JsonProperty("user_name", NullValueHandling = NullValueHandling.Ignore)] public string Username { get; set; }
    [JsonProperty("user_type", NullValueHandling = NullValueHandling.Ignore)] public string UserType { get; set; }
    
    [MaxLength(50)]
    [ForeignKey(nameof(User))]
    [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)] public string UserId { get; set; } = string.Empty;
    [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)] public User User { get; set; } = null!;
    
    [JsonProperty("channel_id", NullValueHandling = NullValueHandling.Ignore)] public string ChannelId { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    [JsonProperty("broadcaster", NullValueHandling = NullValueHandling.Ignore)] public virtual User Broadcaster { get; set; } = null!;

    [JsonProperty("reply_to_message_id", NullValueHandling = NullValueHandling.Ignore)] public string? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    [JsonProperty("reply_to_message", NullValueHandling = NullValueHandling.Ignore)] public virtual ChatMessage? ReplyToMessage { get; set; }

    [JsonProperty("replies", NullValueHandling = NullValueHandling.Ignore)] public virtual ICollection<ChatMessage> Replies { get; set; } = [];

    public ChatMessage()
    {
        
    }
    
    public ChatMessage(EventSubNotification<ChannelChatMessage> payloadEvent)
    {
        Id = payloadEvent.Payload.Event.MessageId;
        ChannelId = payloadEvent.Payload.Event.BroadcasterUserId;
        UserId = payloadEvent.Payload.Event.ChatterUserId;
        Username = payloadEvent.Payload.Event.ChatterUserLogin;
        DisplayName = payloadEvent.Payload.Event.ChatterUserName;
        Message = payloadEvent.Payload.Event.Message.Text;
        
        ColorHex = payloadEvent.Payload.Event.Color;
        Color = !string.IsNullOrEmpty(payloadEvent.Payload.Event.Color) 
            ? ColorTranslator.FromHtml(payloadEvent.Payload.Event.Color) 
            : null;
        Badges = payloadEvent.Payload.Event.Badges.Select(b => new KeyValuePair<string, string>(b.Id, b.SetId)).ToList();
        Fragments = MakeFragments(payloadEvent);
        Message = payloadEvent.Payload.Event.Message.Text;
        ReplyToMessageId = payloadEvent.Payload.Event.Reply?.ParentMessageId;
        TmiSentTs = payloadEvent.Metadata.MessageTimestamp.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        UserType = GetUserType(payloadEvent);
    }

    private List<ChatMessageFragment> MakeFragments(EventSubNotification<ChannelChatMessage> payloadEvent)
    {
        List<ChatMessageFragment> fragments = payloadEvent.Payload.Event.Message.Fragments
                .Select(fragment => new ChatMessageFragment(fragment))
                .ToList();

        return fragments;
    }

    private string GetUserType(EventSubNotification<ChannelChatMessage> payloadEvent)
    {
        if (payloadEvent.Payload.Event.IsBroadcaster) return "Broadcaster";
        if (payloadEvent.Payload.Event.IsStaff) return "Staff";
        if (payloadEvent.Payload.Event.IsModerator) return "Moderator";
        if (payloadEvent.Payload.Event.IsVip) return "Vip";
        if (payloadEvent.Payload.Event.IsSubscriber) return "Subscriber";
        return "Viewer";
    }
    
}

