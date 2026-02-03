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
        /// Сохраняет файл плагина в базу данных (для тестирования)
        /// </summary>
        public async Task<(string filePath, string fileName, long fileSize, string fileHash, byte[] fileContent)> SavePluginFileAsync(
            IFormFile file, string pluginUniqueId, string version)
        {
            try
            {
                _logger.LogInformation("Сохранение файла плагина в базу данных: {FileName}", file.FileName);

                // Генерируем имя файла с версией
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{pluginUniqueId}_v{version}{fileExtension}";
                var filePath = $"database://{fileName}"; // Виртуальный путь

                // Читаем содержимое файла
                byte[] fileContent;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileContent = memoryStream.ToArray();
                }

                // Вычисляем хеш файла
                var fileHash = ComputeFileHash(fileContent);

                _logger.LogInformation("Файл плагина подготовлен для сохранения в БД: {FileName}, размер: {Size} байт", 
                    fileName, fileContent.Length);

                return (filePath, fileName, fileContent.Length, fileHash, fileContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подготовке файла плагина: {FileName}", file.FileName);
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
        /// Получает файл из базы данных для скачивания
        /// </summary>
        public (byte[] fileBytes, string fileName, string contentType)? GetPluginFileFromDatabase(byte[] fileContent, string fileName)
        {
            try
            {
                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning("Содержимое файла пустое: {FileName}", fileName);
                    return null;
                }

                var contentType = GetContentType(fileName);

                _logger.LogInformation("Файл плагина подготовлен для скачивания из БД: {FileName}, размер: {Size} байт", 
                    fileName, fileContent.Length);
                
                return (fileContent, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении файла из БД: {FileName}", fileName);
                return null;
            }
        }

        /// <summary>
        /// Вычисляет SHA256 хеш массива байтов
        /// </summary>
        private string ComputeFileHash(byte[] fileContent)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileContent);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
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