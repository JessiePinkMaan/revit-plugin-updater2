using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace RevitPluginUpdater.Server.Services
{
    /// <summary>
    /// Простое хранение файлов в памяти сервера (для тестирования)
    /// </summary>
    public class InMemoryFileService
    {
        private readonly ConcurrentDictionary<string, byte[]> _files = new();
        private readonly ILogger<InMemoryFileService> _logger;

        public InMemoryFileService(ILogger<InMemoryFileService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Сохраняет файл в памяти
        /// </summary>
        public async Task<(string fileName, long fileSize, string fileHash)> SaveFileAsync(IFormFile file, string pluginUniqueId, string version)
        {
            try
            {
                // Генерируем имя файла
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{pluginUniqueId}_v{version}{fileExtension}";

                // Читаем содержимое файла
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                // Вычисляем хеш
                var fileHash = ComputeFileHash(fileContent);

                // Сохраняем в памяти
                _files[fileName] = fileContent;

                _logger.LogInformation("Файл сохранен в памяти: {FileName}, размер: {Size} байт", fileName, fileContent.Length);

                return (fileName, fileContent.Length, fileHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении файла: {FileName}", file.FileName);
                throw;
            }
        }

        /// <summary>
        /// Получает файл из памяти
        /// </summary>
        public (byte[] fileBytes, string contentType)? GetFile(string fileName)
        {
            try
            {
                if (_files.TryGetValue(fileName, out var fileContent))
                {
                    var contentType = GetContentType(fileName);
                    _logger.LogInformation("Файл получен из памяти: {FileName}, размер: {Size} байт", fileName, fileContent.Length);
                    return (fileContent, contentType);
                }

                _logger.LogWarning("Файл не найден в памяти: {FileName}", fileName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении файла: {FileName}", fileName);
                return null;
            }
        }

        /// <summary>
        /// Удаляет файл из памяти
        /// </summary>
        public bool DeleteFile(string fileName)
        {
            var removed = _files.TryRemove(fileName, out _);
            if (removed)
            {
                _logger.LogInformation("Файл удален из памяти: {FileName}", fileName);
            }
            return removed;
        }

        /// <summary>
        /// Получает список всех файлов
        /// </summary>
        public List<string> GetAllFileNames()
        {
            return _files.Keys.ToList();
        }

        /// <summary>
        /// Вычисляет SHA256 хеш
        /// </summary>
        private string ComputeFileHash(byte[] fileContent)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileContent);
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