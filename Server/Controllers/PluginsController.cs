using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Data;
using RevitPluginUpdater.Server.DTOs;
using RevitPluginUpdater.Server.Services;

namespace RevitPluginUpdater.Server.Controllers
{
    /// <summary>
    /// Контроллер для публичного API плагинов (для клиентов Revit)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PluginsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;
        private readonly ILogger<PluginsController> _logger;

        public PluginsController(ApplicationDbContext context, FileService fileService, ILogger<PluginsController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Получить информацию о плагине по ID
        /// GET /api/plugins/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PluginDto>> GetPlugin(int id)
        {
            try
            {
                var plugin = await _context.Plugins
                    .Include(p => p.Versions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (plugin == null)
                {
                    return NotFound(new { message = "Плагин не найден" });
                }

                var pluginDto = new PluginDto
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    UniqueId = plugin.UniqueId,
                    CreatedAt = plugin.CreatedAt,
                    UpdatedAt = plugin.UpdatedAt,
                    Versions = plugin.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).ToList(),
                    LatestVersion = plugin.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).FirstOrDefault()
                };

                _logger.LogInformation("Получена информация о плагине ID: {PluginId}", id);
                return Ok(pluginDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о плагине ID: {PluginId}", id);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить информацию о плагине по UniqueId
        /// GET /api/plugins/by-unique-id/{uniqueId}
        /// </summary>
        [HttpGet("by-unique-id/{uniqueId}")]
        public async Task<ActionResult<PluginDto>> GetPluginByUniqueId(string uniqueId)
        {
            try
            {
                var plugin = await _context.Plugins
                    .Include(p => p.Versions)
                    .FirstOrDefaultAsync(p => p.UniqueId == uniqueId);

                if (plugin == null)
                {
                    return NotFound(new { message = "Плагин не найден" });
                }

                var pluginDto = new PluginDto
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    UniqueId = plugin.UniqueId,
                    CreatedAt = plugin.CreatedAt,
                    UpdatedAt = plugin.UpdatedAt,
                    Versions = plugin.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).ToList(),
                    LatestVersion = plugin.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).FirstOrDefault()
                };

                _logger.LogInformation("Получена информация о плагине по UniqueId: {UniqueId}", uniqueId);
                return Ok(pluginDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении информации о плагине по UniqueId: {UniqueId}", uniqueId);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить последнюю версию плагина
        /// GET /api/plugins/{id}/latest
        /// </summary>
        [HttpGet("{id}/latest")]
        public async Task<ActionResult<PluginVersionDto>> GetLatestVersion(int id)
        {
            try
            {
                var latestVersion = await _context.PluginVersions
                    .Where(v => v.PluginId == id)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestVersion == null)
                {
                    return NotFound(new { message = "Версии плагина не найдены" });
                }

                var versionDto = new PluginVersionDto
                {
                    Id = latestVersion.Id,
                    Version = latestVersion.Version,
                    ReleaseNotes = latestVersion.ReleaseNotes,
                    FileName = latestVersion.FileName,
                    FileSize = latestVersion.FileSize,
                    FileHash = latestVersion.FileHash,
                    CreatedAt = latestVersion.CreatedAt
                };

                _logger.LogInformation("Получена последняя версия плагина ID: {PluginId}, версия: {Version}", 
                    id, latestVersion.Version);
                return Ok(versionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении последней версии плагина ID: {PluginId}", id);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить последнюю версию плагина по UniqueId
        /// GET /api/plugins/by-unique-id/{uniqueId}/latest
        /// </summary>
        [HttpGet("by-unique-id/{uniqueId}/latest")]
        public async Task<ActionResult<PluginVersionDto>> GetLatestVersionByUniqueId(string uniqueId)
        {
            try
            {
                var plugin = await _context.Plugins
                    .FirstOrDefaultAsync(p => p.UniqueId == uniqueId);

                if (plugin == null)
                {
                    return NotFound(new { message = "Плагин не найден" });
                }

                var latestVersion = await _context.PluginVersions
                    .Where(v => v.PluginId == plugin.Id)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestVersion == null)
                {
                    return NotFound(new { message = "Версии плагина не найдены" });
                }

                var versionDto = new PluginVersionDto
                {
                    Id = latestVersion.Id,
                    Version = latestVersion.Version,
                    ReleaseNotes = latestVersion.ReleaseNotes,
                    FileName = latestVersion.FileName,
                    FileSize = latestVersion.FileSize,
                    FileHash = latestVersion.FileHash,
                    CreatedAt = latestVersion.CreatedAt
                };

                _logger.LogInformation("Получена последняя версия плагина по UniqueId: {UniqueId}, версия: {Version}", 
                    uniqueId, latestVersion.Version);
                return Ok(versionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении последней версии плагина по UniqueId: {UniqueId}", uniqueId);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}