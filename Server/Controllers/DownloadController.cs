using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Data;
using RevitPluginUpdater.Server.Services;

namespace RevitPluginUpdater.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly InMemoryFileService _fileService;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(ApplicationDbContext context, InMemoryFileService fileService, ILogger<DownloadController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        [HttpGet("by-unique-id/{uniqueId}/{version}")]
        public async Task<ActionResult> DownloadPluginByUniqueId(string uniqueId, string version)
        {
            try
            {
                var plugin = await _context.Plugins.FirstOrDefaultAsync(p => p.UniqueId == uniqueId);
                if (plugin == null) return NotFound(new { message = "Плагин не найден" });

                var pluginVersion = await _context.PluginVersions
                    .FirstOrDefaultAsync(v => v.PluginId == plugin.Id && v.Version == version);
                if (pluginVersion == null) return NotFound(new { message = "Версия не найдена" });

                var fileResult = _fileService.GetFile(pluginVersion.FileName);
                if (fileResult == null) return NotFound(new { message = "Файл не найден" });

                return File(fileResult.Value.fileBytes, fileResult.Value.contentType, pluginVersion.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скачивания");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        [HttpGet("by-unique-id/{uniqueId}/latest")]
        public async Task<ActionResult> DownloadLatestPluginByUniqueId(string uniqueId)
        {
            try
            {
                var plugin = await _context.Plugins.FirstOrDefaultAsync(p => p.UniqueId == uniqueId);
                if (plugin == null) return NotFound(new { message = "Плагин не найден" });

                var latestVersion = await _context.PluginVersions
                    .Where(v => v.PluginId == plugin.Id)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();
                if (latestVersion == null) return NotFound(new { message = "Версии не найдены" });

                var fileResult = _fileService.GetFile(latestVersion.FileName);
                if (fileResult == null) return NotFound(new { message = "Файл не найден" });

                return File(fileResult.Value.fileBytes, fileResult.Value.contentType, latestVersion.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скачивания");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }
    }
}