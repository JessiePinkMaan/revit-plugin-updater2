using System.Security.Cryptography;

namespace RevitPluginUpdater.Server.Services
{
    /// <summary>
    /// Сервис для работы с файлами плагинов
    /// </summary>
    public class FileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly string _pluginsPath;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _pluginsPath = _configuration["FileStorage:PluginsPath"] ?? "/var/data/plugins";
            
            // Создаем директорию если она не существует
            if (!Directory.Exists(_pluginsPath))
            {
                Directory.CreateDirectory(_pluginsPath);
                _logger.LogInformation("Создана директория для плагинов: {Path}", _pluginsPath);
            }
        }

        /// <summary>
        /// Сохраняет файл плагина на диск
        /// </summary>
        public async Task<(string filePath, string fileName, long fileSize, string fileHash)> SavePluginFileAsync(
            IFormFile file, string pluginUniqueId, string version)
        {
            try
            {
                // Создаем директорию для плагина
                var pluginDir = Path.Combine(_pluginsPath, pluginUniqueId);
                if (!Directory.Exists(pluginDir))
                {
                    Directory.CreateDirectory(pluginDir);
                }

                // Генерируем имя файла с версией
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{pluginUniqueId}_v{version}{fileExtension}";
                var filePath = Path.Combine(pluginDir, fileName);

                // Сохраняем файл
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Вычисляем хеш файла
                var fileHash = await ComputeFileHashAsync(filePath);
                var fileInfo = new FileInfo(filePath);

                _logger.LogInformation("Файл плагина сохранен: {FilePath}, размер: {Size} байт", 
                    filePath, fileInfo.Length);

                return (filePath, fileName, fileInfo.Length, fileHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении файла плагина: {FileName}", file.FileName);
                throw;
            }
        }

        /// <summary>
        /// Удаляет файл плагина с диска
        /// </summary>
        public async Task<bool> DeletePluginFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Файл плагина удален: {FilePath}", filePath);
                    return true;
                }
                
                _logger.LogWarning("Файл для удаления не найден: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении файла плагина: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Получает файл для скачивания
        /// </summary>
        public async Task<(byte[] fileBytes, string fileName, string contentType)?> GetPluginFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Файл для скачивания не найден: {FilePath}", filePath);
                    return null;
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);

                _logger.LogInformation("Файл плагина подготовлен для скачивания: {FilePath}", filePath);
                return (fileBytes, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении файла плагина: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Вычисляет SHA256 хеш файла
        /// </summary>
        private async Task<string> ComputeFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Определяет MIME тип файла
        /// </summary>
        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".dll" => "application/octet-stream",
                ".exe" => "application/octet-stream",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }
    }
}