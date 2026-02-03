using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Data;
using RevitPluginUpdater.Server.DTOs;
using RevitPluginUpdater.Server.Models;
using RevitPluginUpdater.Server.Services;

namespace RevitPluginUpdater.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly InMemoryFileService _fileService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, InMemoryFileService fileService, ILogger<AdminController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

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

                return Ok(pluginDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка плагинов");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("plugins")]
        public async Task<ActionResult<PluginDto>> CreatePlugin([FromForm] CreatePluginRequest request, IFormFile? file)
        {
            try
            {
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

                if (file != null && file.Length > 0)
                {
                    var (fileName, fileSize, fileHash) = await _fileService.SaveFileAsync(file, plugin.UniqueId, "1.0.0");

                    var version = new PluginVersion
                    {
                        PluginId = plugin.Id,
                        Version = "1.0.0",
                        ReleaseNotes = "Первая версия",
                        FileName = fileName,
                        FileSize = fileSize,
                        FileHash = fileHash,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PluginVersions.Add(version);
                    await _context.SaveChangesAsync();
                }

                var pluginDto = new PluginDto
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    UniqueId = plugin.UniqueId,
                    CreatedAt = plugin.CreatedAt,
                    UpdatedAt = plugin.UpdatedAt
                };

                return CreatedAtAction(nameof(GetPlugins), pluginDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании плагина");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("plugins/{id}/versions")]
        public async Task<ActionResult<PluginVersionDto>> CreateVersion(int id, [FromForm] CreateVersionRequest request, IFormFile file)
        {
            try
            {
                var plugin = await _context.Plugins.FindAsync(id);
                if (plugin == null)
                {
                    return NotFound(new { message = "Плагин не найден" });
                }

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

                var (fileName, fileSize, fileHash) = await _fileService.SaveFileAsync(file, plugin.UniqueId, request.Version);

                var version = new PluginVersion
                {
                    PluginId = id,
                    Version = request.Version,
                    ReleaseNotes = request.ReleaseNotes,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileHash = fileHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PluginVersions.Add(version);
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

                return CreatedAtAction(nameof(GetPlugins), versionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании версии плагина");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}