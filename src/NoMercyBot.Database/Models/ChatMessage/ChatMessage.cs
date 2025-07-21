using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets.Core.Models;

namespace NoMercyBot.Database.Models.ChatMessage;

[PrimaryKey(nameof(Id))]
public class ChatMessage: Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [JsonProperty("id")] [MaxLength(255)] public string Id { get; set; } = null!;
    
    [JsonProperty("is_command")] public bool IsCommand { get; set; }
    [JsonProperty("is_cheer")] public bool IsCheer { get; set; }
    [JsonProperty("bits_amount", NullValueHandling = NullValueHandling.Ignore)] public int? BitsAmount { get; set; }
    [JsonProperty("is_highlighted")] public bool IsHighlighted { get; set; }
    
    [JsonProperty("color_hex", NullValueHandling = NullValueHandling.Ignore)] public string ColorHex { get; set; } = null!;
    [JsonProperty("badges")] public List<ChatBadge> Badges { get; set; } = [];
    
    [MaxLength(50)]
    [ForeignKey(nameof(User))]
    [JsonProperty("user_id")] public string UserId { get; set; } = null!;
    [JsonProperty("user_name")] public string Username { get; set; } = null!;
    [JsonProperty("display_name")] public string DisplayName { get; set; } = null!;
    [JsonProperty("user_type")] public string UserType { get; set; } = null!;
    [JsonProperty("user")] public User User { get; set; } = null!;
    
    [JsonProperty("channel_id")] public string ChannelId { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    [JsonProperty("broadcaster")] public virtual User Broadcaster { get; set; } = null!;
    
    [JsonProperty("message")] public string Message { get; set; } = null!;
    [JsonProperty("fragments")] public List<ChatMessageFragment> Fragments { get; set; } = [];
    [JsonProperty("message_node", NullValueHandling = NullValueHandling.Ignore)] public MessageNode? MessageNode { get; set; }
    
    [JsonProperty("tmi_sent_ts")] public string TmiSentTs { get; set; } = null!;
    
    [JsonProperty("is_command_successful_reply", NullValueHandling = NullValueHandling.Ignore)] public string? SuccessfulReply { get; set; }
    [JsonProperty("reply_to_message_id", NullValueHandling = NullValueHandling.Ignore)] public string? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    [JsonProperty("reply_to_message", NullValueHandling = NullValueHandling.Ignore)] public virtual ChatMessage? ReplyToMessage { get; set; }

    [JsonProperty("replies", NullValueHandling = NullValueHandling.Ignore)] public virtual ICollection<ChatMessage> Replies { get; set; } = [];
    [JsonProperty("deleted_at", NullValueHandling = NullValueHandling.Ignore)] public DateTime? DeletedAt { get; set; }
    
    [JsonProperty("stream_id")] public string? StreamId { get; set; }
    [JsonProperty("stream")] public virtual Stream? Stream { get; set; }

    public ChatMessage()
    {
        
    }
    
    public ChatMessage(EventSubNotification<ChannelChatMessage> payloadEvent, Stream? currentStream)
    {
        Id = payloadEvent.Payload.Event.MessageId;
        ChannelId = payloadEvent.Payload.Event.BroadcasterUserId;
        UserId = payloadEvent.Payload.Event.ChatterUserId;
        Username = payloadEvent.Payload.Event.ChatterUserLogin;
        DisplayName = payloadEvent.Payload.Event.ChatterUserName;
        Message = payloadEvent.Payload.Event.Message.Text;
        IsHighlighted = payloadEvent.Payload.Event.MessageType == "channel_points_highlighted";
        IsCheer = payloadEvent.Payload.Event.Cheer != null;
        BitsAmount = payloadEvent.Payload.Event.Cheer?.Bits;
        
        ColorHex = payloadEvent.Payload.Event.Color;
        Badges = GetBadges(payloadEvent);
        Fragments = MakeFragments(payloadEvent);
        Message = payloadEvent.Payload.Event.Message.Text;
        ReplyToMessageId = payloadEvent.Payload.Event.Reply?.ParentMessageId;
        TmiSentTs = payloadEvent.Metadata.MessageTimestamp.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        UserType = GetUserType(payloadEvent);
        
        StreamId = currentStream?.Id;
    }

    private static List<ChatBadge> GetBadges(EventSubNotification<ChannelChatMessage> payloadEvent)
    {
        return payloadEvent.Payload.Event.Badges
            .Select(badge => new ChatBadge
            {
                SetId = badge.SetId,
                Id = badge.Id,
                Info = badge.Info
            })
            .ToList();
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

