using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database.Models;
using NoMercyBot.Services.Interfaces;
using NoMercyBot.Services.Other;
using NoMercyBot.Services.Spotify;
using NoMercyBot.Services.Spotify.Dto;
using NoMercyBot.Services.Twitch.Scripting;

public class SkipCommand: IBotCommand
{
    public string Name => "skip";
    public CommandPermission Permission => CommandPermission.Everyone;

    private const string STORAGE_KEY = "Spotify";

    public Task Init(CommandScriptContext ctx) => Task.CompletedTask;

    public async Task Callback(CommandScriptContext ctx)
    {
        string userId = ctx.Message.UserId;
        string broadcasterLogin = ctx.Message.Broadcaster.Username;

        try
        {
            SpotifyApiService spotify = ctx.ServiceProvider.GetRequiredService<SpotifyApiService>();
            CurrentlyPlaying? currentSong = await spotify.GetCurrentlyPlaying();

            if (currentSong?.Item == null)
            {
                await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                    "No song is currently playing!", ctx.Message.Id);
                return;
            }

            string currentTrackId = currentSong.Item.Id;

            // Mods/broadcaster can skip anything; everyone else can only skip their own request.
            PermissionService perms = ctx.ServiceProvider.GetRequiredService<PermissionService>();
            bool isMod = perms.UserHasMinLevel(userId, ctx.Message.UserType ?? "everyone", "moderator");

            if (!isMod)
            {
                string songIdNeedle = $"\"SongId\":\"{currentTrackId}\"";
                Record? matchingRecord = await ctx.DatabaseContext.Records
                    .Where(r => r.UserId == userId && r.RecordType == STORAGE_KEY)
                    .Where(r => r.Data.Contains(songIdNeedle))
                    .FirstOrDefaultAsync(ctx.CancellationToken);

                if (matchingRecord == null)
                {
                    await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                        "You can only skip songs you requested yourself.", ctx.Message.Id);
                    return;
                }
            }

            await spotify.NextTrack();

            string text = isMod
                ? "I know right, Stoney's song choices are always on point! Skipped to the next track."
                : "Skipped your song.";
            await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin, text, ctx.Message.Id);
        }
        catch (Exception ex)
        {
            await ctx.TwitchChatService.SendReplyAsBot(broadcasterLogin,
                $"Failed to skip: {ex.Message}", ctx.Message.Id);
        }
    }
}

return new SkipCommand();
