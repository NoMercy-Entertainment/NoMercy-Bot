using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMercyBot.Database;
using NoMercyBot.Database.Models;
using System.Security.Claims;
using NoMercyBot.Api.Helpers;
using NoMercyBot.Services.Other;

namespace NoMercyBot.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tts")]
    public class TtsVoiceController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly TtsService _ttsService;

        public TtsVoiceController(AppDbContext dbContext, TtsService ttsService)
        {
            _dbContext = dbContext;
            _ttsService = ttsService;
        }

        [HttpGet("voices")]
        public async Task<IActionResult> GetVoices()
        {
            List<TtsVoice> voices = await _dbContext.TtsVoices.ToListAsync();
            return Ok(voices);
        }

        [Authorize]
        [HttpGet("voice")]
        public async Task<IActionResult> GetUserVoice()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            UserTtsVoice? userVoice = await _dbContext.UserTtsVoices
                .Include(x => x.TtsVoice)
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (userVoice == null) return NotFound();
            return Ok(userVoice.TtsVoice);
        }

        [Authorize]
        [HttpPost("voice")]
        public async Task<IActionResult> SetUserVoice([FromBody] SetTtsVoiceDto dto)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            TtsVoice? voice = await _dbContext.TtsVoices
                .FirstOrDefaultAsync(x => x.Id == dto.VoiceId);
            if (voice == null) return NotFound("Voice not found");
            UserTtsVoice? userVoice = await _dbContext.UserTtsVoices
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (userVoice == null)
            {
                userVoice = new() { UserId = userId, TtsVoiceId = voice.Id };
                _dbContext.UserTtsVoices.Add(userVoice);
            }
            else
            {
                userVoice.TtsVoiceId = voice.Id;
            }
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
        
        [HttpPost]
        [Route("speak")]
        public async Task<IActionResult> Speak([FromBody] string request)
        {
            string userId = User.UserId().ToString();
            
            if (string.IsNullOrWhiteSpace(request))
            {
                return BadRequest("Request cannot be empty.");
            }

            try
            {
                // Assuming the request is a simple text string to be spoken
                await _ttsService.SendTts(
                    [new()
                    {
                        Type = "text", 
                        Text = request,
                    }], 
                    userId, 
                    CancellationToken.None);
                
                return Ok("Text sent for TTS processing.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class SetTtsVoiceDto
    {
        public string VoiceId { get; set; } = string.Empty;
    }
}

