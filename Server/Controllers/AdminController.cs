using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Data;
using RevitPluginUpdater.Server.DTOs;
using RevitPluginUpdater.Server.Models;
using RevitPluginUpdater.Server.Services;

namespace RevitPluginUpdater.Server.Controllers
{
    /// <summary>
    /// Контроллер для административных функций
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Требует JWT авторизации
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, FileService fileService, ILogger<AdminController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Получить список всех плагинов
        /// GET /api/admin/plugins
        /// </summary>
        [HttpGet("plugins")]
        public async Task<ActionResult<List<PluginDto>>> GetPlugins()
        {
            try
            {
                var plugins = await _context.Plugins
                    .Include(p => p.Versions)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var pluginDtos = plugins.Select(p => new PluginDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UniqueId = p.UniqueId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Versions = p.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).ToList(),
                    LatestVersion = p.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).FirstOrDefault()
                }).ToList();

                _logger.LogInformation("Получен список плагинов, количество: {Count}", pluginDtos.Count);
                return Ok(pluginDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка плагинов");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Создать новый плагин
        /// POST /api/admin/plugins
        /// </summary>
        [HttpPost("plugins")]
        public async Task<ActionResult<PluginDto>> CreatePlugin([FromForm] CreatePluginRequest request, IFormFile? file)
        {
            try
            {
                _logger.LogInformation("Создание нового плагина: {Name}", request.Name);

                // Проверяем уникальность ID
                var existingPlugin = await _context.Plugins
                    .FirstOrDefaultAsync(p => p.UniqueId == request.UniqueId);

                if (existingPlugin != null)
                {
                    return BadRequest(new { message = "Плагин с таким UniqueId уже существует" });
                }

                var plugin = new Plugin
                {
                    Name = request.Name,
                    Description = request.Description,
                    UniqueId = request.UniqueId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Plugins.Add(plugin);
                await _context.SaveChangesAsync();

                // Если загружен файл, создаем первую версию
                if (file != null && file.Length > 0)
                {
                    var (filePath, fileName, fileSize, fileHash, fileContent) = await _fileService.SavePluginFileAsync(
                        file, plugin.UniqueId, "1.0.0");

                    var version = new PluginVersion
                    {
                        PluginId = plugin.Id,
                        Version = "1.0.0",
                        ReleaseNotes = "Первая версия",
                        FileName = fileName,
                        FilePath = filePath,
                        FileSize = fileSize,
                        FileHash = fileHash,
                        FileContent = fileContent, // Сохраняем содержимое в БД
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PluginVersions.Add(version);
                    await _context.SaveChangesAsync();

                    plugin.Versions.Add(version);
                }

                var pluginDto = new PluginDto
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    UniqueId = plugin.UniqueId,
                    CreatedAt = plugin.CreatedAt,
                    UpdatedAt = plugin.UpdatedAt,
                    Versions = plugin.Versions.Select(v => new PluginVersionDto
                    {
                        Id = v.Id,
                        Version = v.Version,
                        ReleaseNotes = v.ReleaseNotes,
                        FileName = v.FileName,
                        FileSize = v.FileSize,
                        FileHash = v.FileHash,
                        CreatedAt = v.CreatedAt
                    }).ToList()
                };

                _logger.LogInformation("Плагин создан успешно: {Name} (ID: {Id})", plugin.Name, plugin.Id);
                return CreatedAtAction(nameof(GetPlugins), new { id = plugin.Id }, pluginDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании плагина: {Name}", request.Name);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Загрузить новую версию плагина
        /// POST /api/admin/plugins/{id}/versions
        /// </summary>
        [HttpPost("plugins/{id}/versions")]
        public async Task<ActionResult<PluginVersionDto>> CreateVersion(int id, [FromForm] CreateVersionRequest request, IFormFile file)
        {
            try
            {
                _logger.LogInformation("Загрузка новой версии для плагина ID: {PluginId}, версия: {Version}", id, request.Version);

                var plugin = await _context.Plugins.FindAsync(id);
                if (plugin == null)
                {
                    return NotFound(new { message = "Плагин не найден" });
                }

                // Проверяем уникальность версии
                var existingVersion = await _context.PluginVersions
                    .FirstOrDefaultAsync(v => v.PluginId == id && v.Version == request.Version);

                if (existingVersion != null)
                {
                    return BadRequest(new { message = "Версия уже существует" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Файл не загружен" });
                }

                // Сохраняем файл
                var (filePath, fileName, fileSize, fileHash, fileContent) = await _fileService.SavePluginFileAsync(
                    file, plugin.UniqueId, request.Version);

                var version = new PluginVersion
                {
                    PluginId = id,
                    Version = request.Version,
                    ReleaseNotes = request.ReleaseNotes,
                    FileName = fileName,
                    FilePath = filePath,
                    FileSize = fileSize,
                    FileHash = fileHash,
                    FileContent = fileContent, // Сохраняем содержимое в БД
                    CreatedAt = DateTime.UtcNow
                };

                _context.PluginVersions.Add(version);
                plugin.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var versionDto = new PluginVersionDto
                {
                    Id = version.Id,
                    Version = version.Version,
                    ReleaseNotes = version.ReleaseNotes,
                    FileName = version.FileName,
                    FileSize = version.FileSize,
                    FileHash = version.FileHash,
                    CreatedAt = version.CreatedAt
                };

                _logger.LogInformation("Версия плагина создана успешно: {Version} для плагина ID: {PluginId}", 
                    version.Version, id);
                return CreatedAtAction(nameof(GetPlugins), versionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании версии плагина ID: {PluginId}, версия: {Version}", 
                    id, request.Version);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Удалить версию плагина
        /// DELETE /api/admin/plugins/{id}/versions/{version}
        /// </summary>
        [HttpDelete("plugins/{id}/versions/{version}")]
        public async Task<ActionResult> DeleteVersion(int id, string version)
        {
            try
            {
                _logger.LogInformation("Удаление версии {Version} плагина ID: {PluginId}", version, id);

                var pluginVersion = await _context.PluginVersions
                    .FirstOrDefaultAsync(v => v.PluginId == id && v.Version == version);

                if (pluginVersion == null)
                {
                    return NotFound(new { message = "Версия не найдена" });
                }

                // Удаляем файл с диска
                await _fileService.DeletePluginFileAsync(pluginVersion.FilePath);

                // Удаляем запись из базы данных
                _context.PluginVersions.Remove(pluginVersion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Версия плагина удалена успешно: {Version} для плагина ID: {PluginId}", 
                    version, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении версии {Version} плагина ID: {PluginId}", version, id);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}