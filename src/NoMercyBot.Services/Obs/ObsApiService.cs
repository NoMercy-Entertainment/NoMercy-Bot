using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoMercyBot.Database.Models;

namespace NoMercyBot.Services.Obs;

public class ObsApiService
{
    private readonly IConfiguration _conf;
    private readonly ILogger<ObsApiService> _logger;

    public Service Service => ObsConfig.Service();
    
    public string ClientId => Service.ClientId ?? throw new InvalidOperationException("OBS ClientId is not set.");

    public ObsApiService(IConfiguration conf, ILogger<ObsApiService> logger)
    {
        _conf = conf;
        _logger = logger;
    }
}