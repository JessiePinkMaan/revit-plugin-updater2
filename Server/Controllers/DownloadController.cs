using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Data;
using RevitPluginUpdater.Server.Services;

namespace RevitPluginUpdater.Server.Controllers
{
    /// <summary>
    /// Контроллер для скачивания файлов плагинов
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(ApplicationDbContext context, FileService fileService, ILogger<DownloadController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Скачать файл плагина по ID и версии
        /// GET /api/download/{id}/{version}
        /// </summary>
        [HttpGet("{id}/{version}")]
        public async Task<ActionResult> DownloadPlugin(int id, string version)
        {
            try
            {
                _logger.LogInformation("Запрос на скачивание плагина ID: {PluginId}, версия: {Version}", id, version);

                var pluginVersion = await _context.PluginVersions
                    .Include(v => v.Plugin)
                    .FirstOrDefaultAsync(v => v.PluginId == id && v.Version == version);

                if (pluginVersion == null)
                {
                    _logger.LogWarning("Версия плагина не найдена: ID {PluginId}, версия {Version}", id, version);
                    return NotFound(new { message = "Версия плагина не найдена" });
                }

                var fileResult = _fileService.GetPluginFileFromDatabase(pluginVersion.FileContent, pluginVersion.FileName);
                if (fileResult == null)
                {
                    _logger.LogError("Файл плагина не найден в базе данных: {FileName}", pluginVersion.FileName);
                    return NotFound(new { message = "Файл плагина не найден" });
                }

                _logger.LogInformation("Файл плагина отправлен: {FileName}, размер: {Size} байт", 
                    fileResult.Value.fileName, fileResult.Value.fileBytes.Length);

                return File(fileResult.Value.fileBytes, fileResult.Value.contentType, fileResult.Value.fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при скачивании плагина ID: {PluginId}, версия: {Version}", id, version);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Скачать файл плагина по UniqueId и версии
        /// GET /api/download/by-unique-id/{uniqueId}/{version}
        /// </summary>
        [HttpGet("by-unique-id/{uniqueId}/{version}")]
        public async Task<ActionResult> DownloadPluginByUniqueId(string uniqueId, string version)
        {
            try
            {
                _logger.LogInformation("Запрос на скачивание плагина по UniqueId: {UniqueId}, версия: {Version}", 
                    uniqueId, version);

                var plugin = await _context.Plugins
                    .FirstOrDefaultAsync(p => p.UniqueId == uniqueId);

                if (plugin == null)
                {
                    _logger.LogWarning("Плагин не найден по UniqueId: {UniqueId}", uniqueId);
                    return NotFound(new { message = "Плагин не найден" });
                }

                var pluginVersion = await _context.PluginVersions
                    .FirstOrDefaultAsync(v => v.PluginId == plugin.Id && v.Version == version);

                if (pluginVersion == null)
                {
                    _logger.LogWarning("Версия плагина не найдена: UniqueId {UniqueId}, версия {Version}", 
                        uniqueId, version);
                    return NotFound(new { message = "Версия плагина не найдена" });
                }

                var fileResult = _fileService.GetPluginFileFromDatabase(pluginVersion.FileContent, pluginVersion.FileName);
                if (fileResult == null)
                {
                    _logger.LogError("Файл плагина не найден в базе данных: {FileName}", pluginVersion.FileName);
                    return NotFound(new { message = "Файл плагина не найден" });
                }

                _logger.LogInformation("Файл плагина отправлен по UniqueId: {FileName}, размер: {Size} байт", 
                    fileResult.Value.fileName, fileResult.Value.fileBytes.Length);

                return File(fileResult.Value.fileBytes, fileResult.Value.contentType, fileResult.Value.fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при скачивании плагина по UniqueId: {UniqueId}, версия: {Version}", 
                    uniqueId, version);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Скачать последнюю версию плагина по UniqueId
        /// GET /api/download/by-unique-id/{uniqueId}/latest
        /// </summary>
        [HttpGet("by-unique-id/{uniqueId}/latest")]
        public async Task<ActionResult> DownloadLatestPluginByUniqueId(string uniqueId)
        {
            try
            {
                _logger.LogInformation("Запрос на скачивание последней версии плагина по UniqueId: {UniqueId}", uniqueId);

                var plugin = await _context.Plugins
                    .FirstOrDefaultAsync(p => p.UniqueId == uniqueId);

                if (plugin == null)
                {
                    _logger.LogWarning("Плагин не найден по UniqueId: {UniqueId}", uniqueId);
                    return NotFound(new { message = "Плагин не найден" });
                }

                var latestVersion = await _context.PluginVersions
                    .Where(v => v.PluginId == plugin.Id)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestVersion == null)
                {
                    _logger.LogWarning("Версии плагина не найдены: UniqueId {UniqueId}", uniqueId);
                    return NotFound(new { message = "Версии плагина не найдены" });
                }

                var fileResult = _fileService.GetPluginFileFromDatabase(latestVersion.FileContent, latestVersion.FileName);
                if (fileResult == null)
                {
                    _logger.LogError("Файл плагина не найден в базе данных: {FileName}", latestVersion.FileName);
                    return NotFound(new { message = "Файл плагина не найден" });
                }

                _logger.LogInformation("Последняя версия плагина отправлена по UniqueId: {FileName}, версия: {Version}", 
                    fileResult.Value.fileName, latestVersion.Version);

                return File(fileResult.Value.fileBytes, fileResult.Value.contentType, fileResult.Value.fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при скачивании последней версии плагина по UniqueId: {UniqueId}", uniqueId);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}