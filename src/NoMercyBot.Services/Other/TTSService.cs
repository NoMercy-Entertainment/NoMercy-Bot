using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Database;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Globals.Information;
using NoMercyBot.Globals.SystemCalls;
using NoMercyBot.Services.Widgets;
using RestSharp;
using Serilog.Events;

namespace NoMercyBot.Services.Other
{
	public class TtsService
	{
		private readonly RestClient _client;
		private readonly IWidgetEventService _widgetEventService;
		private readonly IServiceScope _scope;
		private readonly AppDbContext _dbContext;

		public TtsService(IServiceScopeFactory serviceScopeFactory, IWidgetEventService widgetEventService)
		{
			_scope = serviceScopeFactory.CreateScope();
			_dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
			_widgetEventService = widgetEventService;
			_client = new("http://localhost:6040");
		}

		public async Task SendTts(List<ChatMessageFragment> chatMessageFragments, string chatMessageUserId, CancellationToken ctsToken)
		{
			if (!Config.UseTts) return;
			
			try
			{
				StringBuilder textBuilder = new();
				foreach (ChatMessageFragment fragment in chatMessageFragments.Where(fragment => fragment.Type == "text"))
				{
					textBuilder.Append(fragment.Text);
				}

				string text = textBuilder.ToString();
				if (string.IsNullOrWhiteSpace(text)) return;
				
				// Get the user's selected TTS voice (implement this as needed)
				string speakerId = await GetSpeakerIdForUserAsync(chatMessageUserId, ctsToken);
				if (string.IsNullOrWhiteSpace(speakerId)) return;
			
				byte[] audioBytes = await SynthesizeAsync(text, speakerId, ctsToken);
				if (audioBytes == null || audioBytes.Length == 0) return;

				string audioBase64 = $"data:audio/wav;base64,{Convert.ToBase64String(audioBytes)}";
				
				// save tts as wav file
				if (Config.SaveTtsToDisk)
				{
					string filePath = Path.Combine(AppFiles.CachePath, "tts", $"{chatMessageUserId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.wav");
					Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
					await File.WriteAllBytesAsync(filePath, audioBytes, ctsToken);
				}

				var ttsEvent = new
				{
					text = text,
					user = new { id = chatMessageUserId },
					audioBase64 = audioBase64
				};

				await _widgetEventService.PublishEventAsync("channel.chat.message.tts", ttsEvent);
			}
			catch (Exception e)
			{
				Logger.Twitch(e.Message, LogEventLevel.Error);
			}
		}

		private async Task<string> GetSpeakerIdForUserAsync(string chatMessageUserId, CancellationToken ctsToken)
		{
			string? id = await _dbContext.UserTtsVoices
				.AsNoTracking()
				.Where(u => u.UserId == chatMessageUserId)
				.Select(u => u.TtsVoiceId)
				.FirstOrDefaultAsync(ctsToken);
			
			if (id is not null) return id;
			
			id = await _dbContext.TtsVoices
				.AsNoTracking()
				.Select(u => u.Id)
				.OrderBy(tv => EF.Functions.Random())
				.FirstOrDefaultAsync(cancellationToken: ctsToken);
			
			await _dbContext.UserTtsVoices.Upsert(new()
			{
				UserId = chatMessageUserId,
				TtsVoiceId = id ?? "ED\n"
			})
				.On(u => u.UserId)
				.WhenMatched((e, incoming) => new()
				{
					TtsVoiceId = incoming.TtsVoiceId,
					SetAt = DateTime.UtcNow
				})
				.RunAsync(ctsToken);
			
			return id ?? "ED\n";
		}
		
		private async Task<byte[]> SynthesizeAsync(string text, string speakerId, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new ArgumentException("Text cannot be empty.", nameof(text));
			if (string.IsNullOrWhiteSpace(speakerId))
				throw new ArgumentException("SpeakerId cannot be empty.", nameof(speakerId));

			text = text.Replace("\"", "&quot;")
				.Replace("&", "&amp;")
				.Replace("'", "&apos;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;");
			
			RestRequest request = new("api/tts");
			request.AddParameter("text", text, ParameterType.QueryString);
			request.AddParameter("speaker_id", speakerId, ParameterType.QueryString);
			request.AddParameter("style_wav", null, ParameterType.QueryString);
			request.AddParameter("language_id", null, ParameterType.QueryString);
			
			RestResponse response = await _client.ExecuteAsync(request, cancellationToken);
			
			if (!response.IsSuccessful || response.Content == null)
				throw new($"Failed to synthesize TTS: {speakerId} {response.ErrorMessage}");
			
			// Assuming the response content is the audio data in byte array format
			return response.RawBytes ?? [];
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}
