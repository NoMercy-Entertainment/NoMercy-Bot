using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NoMercyBot.Database.Models;
using NoMercyBot.Globals.Information;
using TwitchLib.Client.Enums;
using TwitchLib.EventSub.Core.Models.Chat;
using ChatMessage = NoMercyBot.Database.Models.ChatMessage;

namespace NoMercyBot.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        //
    }

    public AppDbContext()
    {
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder.UseSqlite($"Data Source={AppFiles.DatabaseFile}; Pooling=True",
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<string>()
            .HaveMaxLength(256);

        configurationBuilder
            .Properties<Ulid>()
            .HaveConversion<UlidToStringConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(Ulid))
            .ToList()
            .ForEach(p => p.SetElementType(typeof(string)));
        
        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.Name is "CreatedAt" or "UpdatedAt")
            .ToList()
            .ForEach(p => p.SetDefaultValueSql("CURRENT_TIMESTAMP"));

        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) && p.IsNullable)
            .ToList()
            .ForEach(p => p.SetDefaultValue(null));

        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .ToList()
            .ForEach(p => p.DeleteBehavior = DeleteBehavior.Cascade);
        
        // Make sure to encrypt and decrypt the access and refresh tokens
        modelBuilder.Entity<Service>()
            .Property(e => e.AccessToken)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v =>  TokenStore.DecryptToken(v));
        
        modelBuilder.Entity<Service>()
            .Property(e => e.RefreshToken)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v =>  TokenStore.DecryptToken(v));
        
        modelBuilder.Entity<Service>()
            .Property(e => e.ClientId)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v =>  TokenStore.DecryptToken(v));
        
        modelBuilder.Entity<Service>()
            .Property(e => e.ClientSecret)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v =>  TokenStore.DecryptToken(v));

        
        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.ReplyToMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.BadgeInfo)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(v) ?? new List<KeyValuePair<string, string>>());
        
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.Color)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Color>(v));
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.Badges)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(v) ?? new List<KeyValuePair<string, string>>());

        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.Fragments)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<ChatMessageFragment[]>(v) ?? Array.Empty<ChatMessageFragment>());
        
        // modelBuilder.Entity<ChatMessage>()
        //     .Property(e => e.EmoteSet)
        //     .HasConversion(
        //         v => JsonConvert.SerializeObject(v, new JsonSerializerSettings 
        //         { 
        //             TypeNameHandling = TypeNameHandling.All 
        //         }),
        //         v => JsonConvert.DeserializeObject<MyEmoteSet>(v, new JsonSerializerSettings 
        //         { 
        //             TypeNameHandling = TypeNameHandling.All 
        //         }) ?? new MyEmoteSet());
        
        modelBuilder.Entity<ChatMessage>()
            .Property(e => e.UserType)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<UserType>(v));
    
        // Configure the ChatPresence-User (as Channel) relationship
        modelBuilder.Entity<ChatPresence>()
            .HasOne(cp => cp.Channel)
            .WithMany()
            .HasForeignKey(cp => cp.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure the ChatPresence-User relationship
        modelBuilder.Entity<ChatPresence>()
            .HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure the Channel-ChatPresence relationship
        modelBuilder.Entity<Channel>()
            .HasMany(c => c.UsersInChat)
            .WithOne()
            .HasForeignKey(cp => cp.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BotAccount>()
            .Property(e => e.ClientId)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v => TokenStore.DecryptToken(v));

        modelBuilder.Entity<BotAccount>()
            .Property(e => e.ClientSecret)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v => TokenStore.DecryptToken(v));

        modelBuilder.Entity<BotAccount>()
            .Property(e => e.AccessToken)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v => TokenStore.DecryptToken(v));

        modelBuilder.Entity<BotAccount>()
            .Property(e => e.RefreshToken)
            .HasConversion(
                v => TokenStore.EncryptToken(v),
                v => TokenStore.DecryptToken(v));
        
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<ChannelInfo> ChannelInfo { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatPresence> ChatPresences { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Pronoun> Pronouns { get; set; }
    public DbSet<EventSubscription> EventSubscriptions { get; set; }
    public DbSet<BotAccount> BotAccounts { get; set; }
}