using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Data;

namespace RevitPluginUpdater.Server.Controllers
{
    /// <summary>
    /// Контроллер для проверки состояния сервера
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Проверка состояния сервера
        /// GET /api/health
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetHealth()
        {
            try
            {
                // Проверяем подключение к базе данных
                var canConnect = await _context.Database.CanConnectAsync();
                
                var healthStatus = new
                {
                    status = canConnect ? "healthy" : "unhealthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    database = canConnect ? "connected" : "disconnected",
                    uptime = Environment.TickCount64 / 1000 // секунды с момента запуска
                };

                if (canConnect)
                {
                    _logger.LogInformation("Проверка здоровья сервера: OK");
                    return Ok(healthStatus);
                }
                else
                {
                    _logger.LogWarning("Проверка здоровья сервера: Проблемы с базой данных");
                    return StatusCode(503, healthStatus); // Service Unavailable
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке здоровья сервера");
                
                var errorStatus = new
                {
                    status = "error",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    database = "error",
                    error = ex.Message
                };

                return StatusCode(500, errorStatus);
            }
        }

        /// <summary>
        /// Детальная проверка состояния сервера
        /// GET /api/health/detailed
        /// </summary>
        [HttpGet("detailed")]
        public async Task<ActionResult> GetDetailedHealth()
        {
            try
            {
                var checks = new Dictionary<string, object>();

                // Проверка базы данных
                try
                {
                    var canConnect = await _context.Database.CanConnectAsync();
                    var pluginCount = canConnect ? await _context.Plugins.CountAsync() : 0;
                    var versionCount = canConnect ? await _context.PluginVersions.CountAsync() : 0;

                    checks["database"] = new
                    {
                        status = canConnect ? "healthy" : "unhealthy",
                        pluginCount,
                        versionCount
                    };
                }
                catch (Exception ex)
                {
                    checks["database"] = new
                    {
                        status = "error",
                        error = ex.Message
                    };
                }

                // Проверка файловой системы
                try
                {
                    var pluginsPath = "/var/data/plugins";
                    var directoryExists = Directory.Exists(pluginsPath);
                    var diskSpace = directoryExists ? new DriveInfo(Path.GetPathRoot(pluginsPath)!).AvailableFreeSpace : 0;

                    checks["filesystem"] = new
                    {
                        status = directoryExists ? "healthy" : "unhealthy",
                        pluginsPath,
                        directoryExists,
                        availableSpaceGB = diskSpace / (1024 * 1024 * 1024)
                    };
                }
                catch (Exception ex)
                {
                    checks["filesystem"] = new
                    {
                        status = "error",
                        error = ex.Message
                    };
                }

                var overallStatus = checks.Values.All(check => 
                    ((dynamic)check).status == "healthy") ? "healthy" : "degraded";

                var detailedHealth = new
                {
                    status = overallStatus,
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    uptime = Environment.TickCount64 / 1000,
                    checks
                };

                _logger.LogInformation("Детальная проверка здоровья сервера: {Status}", overallStatus);
                return Ok(detailedHealth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при детальной проверке здоровья сервера");
                return StatusCode(500, new { status = "error", error = ex.Message });
            }
        }
    }
}