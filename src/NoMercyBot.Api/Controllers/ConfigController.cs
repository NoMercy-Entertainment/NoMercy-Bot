using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using NoMercyBot.Globals.Information;
using NoMercyBot.Database;
using Microsoft.EntityFrameworkCore;

namespace NoMercyBot.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public ConfigController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Microsoft.AspNetCore.Mvc.HttpGet]
        public IActionResult GetConfig()
        {
            var config = new
            {
                Config.DnsServer,
                Config.ProxyServer,
                Config.InternalServerPort,
                Config.InternalClientPort,
                Config.InternalTtsPort,
                QueueWorkers = Config.QueueWorkers.Value,
                CronWorkers = Config.CronWorkers.Value,
                Config.UseTts,
                Config.SaveTtsToDisk,
                Config.UseFrankerfacezEmotes,
                Config.UseBttvEmotes,
                Config.UseSevenTvEmotes,
                Config.UseChatCodeSnippets,
                Config.UseChatHtmlParser,
                Config.UseChatOgParser
            };
            
            return Ok(config);
        }

        public class ConfigUpdateRequest
        {
            public int? InternalServerPort { get; set; }
            public int? InternalClientPort { get; set; }
            public int? InternalTtsPort { get; set; }
            public bool? Swagger { get; set; }
            public int? QueueWorkers { get; set; }
            public int? CronWorkers { get; set; }
            public bool? UseTts { get; set; }
            public bool? SaveTtsToDisk { get; set; }
            public bool? UseFrankerfacezEmotes { get; set; }
            public bool? UseBttvEmotes { get; set; }
            public bool? UseSevenTvEmotes { get; set; }
            public bool? UseChatCodeSnippets { get; set; }
            public bool? UseChatHtmlParser { get; set; }
            public bool? UseChatOgParser { get; set; }
        }

        [Microsoft.AspNetCore.Mvc.HttpPut]
        public async Task<IActionResult> UpdateConfig([Microsoft.AspNetCore.Mvc.FromBody] ConfigUpdateRequest request)
        {
            if (request.QueueWorkers is not null)
            {
                Config.QueueWorkers = new(Config.QueueWorkers.Key, (int)request.QueueWorkers);
                // await QueueRunner.SetWorkerCount(Config.QueueWorkers.Key, (int)request.QueueWorkers);
                await _dbContext.Configurations.Upsert(new()
                        { Key = "QueueWorkers", Value = request.QueueWorkers.ToString()! })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.CronWorkers is not null)
            {
                Config.CronWorkers = new(Config.CronWorkers.Key, (int)request.CronWorkers);
                // await QueueRunner.SetWorkerCount(Config.CronWorkers.Key, (int)request.CronWorkers);
                await _dbContext.Configurations.Upsert(new()
                        { Key = "CronWorkers", Value = request.CronWorkers.ToString()! })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseTts is not null)
            {
                Config.UseTts = request.UseTts.Value;
                await _dbContext.Configurations
                    .Upsert(new() { Key = "UseTts", Value = request.UseTts.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.SaveTtsToDisk is not null)
            {
                Config.SaveTtsToDisk = request.SaveTtsToDisk.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "SaveTtsToDisk", Value = request.SaveTtsToDisk.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseFrankerfacezEmotes is not null)
            {
                Config.UseFrankerfacezEmotes = request.UseFrankerfacezEmotes.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "UseFrankerfacezEmotes", Value = request.UseFrankerfacezEmotes.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseBttvEmotes is not null)
            {
                Config.UseBttvEmotes = request.UseBttvEmotes.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "UseBttvEmotes", Value = request.UseBttvEmotes.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseSevenTvEmotes is not null)
            {
                Config.UseSevenTvEmotes = request.UseSevenTvEmotes.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "UseSevenTvEmotes", Value = request.UseSevenTvEmotes.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseChatCodeSnippets is not null)
            {
                Config.UseChatCodeSnippets = request.UseChatCodeSnippets.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "UseChatCodeSnippets", Value = request.UseChatCodeSnippets.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseChatHtmlParser is not null)
            {
                Config.UseChatHtmlParser = request.UseChatHtmlParser.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "UseChatHtmlParser", Value = request.UseChatHtmlParser.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.UseChatOgParser is not null)
            {
                Config.UseChatOgParser = request.UseChatOgParser.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "UseChatOgParser", Value = request.UseChatOgParser.Value.ToString().ToLower() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.InternalServerPort is not null)
            {
                Config.InternalServerPort = request.InternalServerPort.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "InternalServerPort", Value = request.InternalServerPort.Value.ToString() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.InternalClientPort is not null)
            {
                Config.InternalClientPort = request.InternalClientPort.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "InternalClientPort", Value = request.InternalClientPort.Value.ToString() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.InternalTtsPort is not null)
            {
                Config.InternalTtsPort = request.InternalTtsPort.Value;
                await _dbContext.Configurations.Upsert(new()
                        { Key = "InternalTtsPort", Value = request.InternalTtsPort.Value.ToString() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            if (request.Swagger is not null)
            {
                Config.Swagger = request.Swagger.Value;
                await _dbContext.Configurations
                    .Upsert(new() { Key = "Swagger", Value = request.Swagger.Value.ToString() })
                    .On(c => c.Key)
                    .WhenMatched((oldConfig, newConfig) => new() { Value = newConfig.Value })
                    .RunAsync();
            }

            return NoContent();
        }
    }
}