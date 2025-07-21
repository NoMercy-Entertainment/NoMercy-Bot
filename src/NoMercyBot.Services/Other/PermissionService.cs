using Microsoft.Extensions.Logging;
using NoMercyBot.Database;
using NoMercyBot.Services.Interfaces;

namespace NoMercyBot.Services.Other;

public class PermissionService: IService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PermissionService> _logger;
    
    public PermissionService(AppDbContext dbContext, ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public static bool Can(IService service, string permission)
    {
        return true;
    }
}