using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Client.Enums;
using TwitchLib.EventSub.Core.Models.Chat;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets.Core.Models;

// ReSharper disable MemberCanBePrivate.Global

namespace NoMercyBot.Database.Models;

[PrimaryKey(nameof(Id))]
public class ChatMessage: Timestamps
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(255)] public string Id { get; set; }
    public List<KeyValuePair<string, string>>? BadgeInfo { get; set; }
    public Color? Color { get; set; }
    public List<KeyValuePair<string, string>>? Badges { get; set; }
    public int Bits { get; set; }
    public double BitsInDollars { get; set; }
    // public CheerBadge? CheerBadge  { get; set; }
    
    public ChatMessageFragment[] Fragments  { get; set; }
    public string? CustomRewardId { get; set; }
    
    public bool IsBroadcaster { get; set; }
    public bool IsFirstMessage { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsMe { get; set; }
    public bool IsModerator { get; set; }
    public bool IsSkippingSubMode { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsVip { get; set; }
    public bool IsStaff { get; set; }
    public bool IsPartner { get; set; }
    public string Message { get; set; }
    public Noisy Noisy { get; set; }
    public int SubscribedMonthCount { get; set; }
    public string TmiSentTs { get; set; }
    
    public string? BotUsername { get; set; }
    public string ColorHex { get; set; }
    public string DisplayName { get; set; }
    public bool IsTurbo { get; set; }
    public string Username { get; set; }
    public UserType UserType { get; set; }
    
    [MaxLength(50)]
    [ForeignKey(nameof(Moderator))]
    public string UserId { get; set; } = string.Empty;
    public User Moderator { get; set; } = null!;
    
    public bool IsReturningChatter { get; set; }
    public string? RewardId { get; set; }
    
    public string ChannelId { get; set; } = null!;
    [ForeignKey(nameof(ChannelId))]
    public virtual User Broadcaster { get; set; } = null!;

    public string? ReplyToMessageId { get; set; }
    [ForeignKey(nameof(ReplyToMessageId))]
    public virtual ChatMessage? ReplyToMessage { get; set; }

    public virtual ICollection<ChatMessage> Replies { get; set; } = [];

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
        IsMe = payloadEvent.Payload.Event.IsBroadcaster;
        IsModerator = payloadEvent.Payload.Event.IsModerator;
        IsSubscriber = payloadEvent.Payload.Event.IsSubscriber;
        IsVip = payloadEvent.Payload.Event.IsVip;
        IsBroadcaster = payloadEvent.Payload.Event.IsBroadcaster;
        IsStaff = payloadEvent.Payload.Event.IsStaff;
        ColorHex = payloadEvent.Payload.Event.Color;
        Color = !string.IsNullOrEmpty(payloadEvent.Payload.Event.Color) 
            ? ColorTranslator.FromHtml(payloadEvent.Payload.Event.Color) 
            : null;
        Badges = payloadEvent.Payload.Event.Badges.Select(b => new KeyValuePair<string, string>(b.Id, b.SetId)).ToList();
        BadgeInfo = payloadEvent.Payload.Event.SourceBadges?.Select(b => new KeyValuePair<string, string>(b.Id, b.SetId)).ToList();
        Fragments = payloadEvent.Payload.Event.Message.Fragments;
        Message = payloadEvent.Payload.Event.Message.Text;
        ReplyToMessageId = payloadEvent.Payload.Event.Reply?.ParentMessageId;
        TmiSentTs = payloadEvent.Metadata.MessageTimestamp.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        UserType = payloadEvent.Payload.Event.IsBroadcaster 
            ? UserType.Broadcaster
            : payloadEvent.Payload.Event.IsModerator 
                ? UserType.Moderator
                : payloadEvent.Payload.Event.IsStaff 
                    ? UserType.Staff 
                    : UserType.Viewer;
        
    }
}
