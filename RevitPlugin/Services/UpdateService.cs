using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;
using RevitPluginUpdater.Client.Models;

namespace RevitPluginUpdater.Client.Services
{
    /// <summary>
    /// Сервис для проверки и загрузки обновлений плагинов
    /// </summary>
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly UpdateConfig _config;
        private readonly string _logFilePath;

        public UpdateService(UpdateConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            // Настраиваем путь к лог файлу
            var logDir = Path.Combine(_config.PluginDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            _logFilePath = Path.Combine(logDir, $"updater_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// Проверяет доступность новых обновлений
        /// </summary>
        public async Task<PluginVersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                LogMessage("Проверка обновлений...");

                var url = $"{_config.ServerUrl}/api/plugins/by-unique-id/{_config.PluginUniqueId}/latest";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    LogMessage($"Ошибка при проверке обновлений: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var latestVersion = JsonConvert.DeserializeObject<PluginVersionInfo>(json);

                if (latestVersion == null)
                {
                    LogMessage("Не удалось получить информацию о последней версии");
                    return null;
                }

                // Сравниваем версии
                if (IsNewerVersion(latestVersion.Version, _config.CurrentVersion))
                {
                    LogMessage($"Найдено обновление: {latestVersion.Version} (текущая: {_config.CurrentVersion})");
                    return latestVersion;
                }
                else
                {
                    LogMessage($"Обновления не найдены. Текущая версия {_config.CurrentVersion} актуальна");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при проверке обновлений: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Скачивает файл обновления
        /// </summary>
        public async Task<string> DownloadUpdateAsync(PluginVersionInfo versionInfo, IProgress<int> progress = null)
        {
            try
            {
                LogMessage($"Начинаем скачивание версии {versionInfo.Version}...");

                var url = $"{_config.ServerUrl}/api/download/by-unique-id/{_config.PluginUniqueId}/{versionInfo.Version}";
                
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        LogMessage($"Ошибка при скачивании: {response.StatusCode}");
                        return null;
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var downloadedBytes = 0L;

                    // Создаем временный файл для скачивания
                    var tempDir = Path.Combine(_config.PluginDirectory, "Temp");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }

                    var tempFilePath = Path.Combine(tempDir, $"update_{versionInfo.Version}_{Guid.NewGuid():N}.tmp");

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            // Обновляем прогресс
                            if (progress != null && totalBytes > 0)
                            {
                                var progressPercentage = (int)((downloadedBytes * 100) / totalBytes);
                                progress.Report(progressPercentage);
                            }
                        }
                    }

                    // Проверяем хеш файла
                    var fileHash = await ComputeFileHashAsync(tempFilePath);
                    if (!string.Equals(fileHash, versionInfo.FileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        LogMessage($"Ошибка: хеш скачанного файла не совпадает. Ожидался: {versionInfo.FileHash}, получен: {fileHash}");
                        File.Delete(tempFilePath);
                        return null;
                    }

                    LogMessage($"Файл успешно скачан: {tempFilePath}");
                    return tempFilePath;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при скачивании обновления: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Запускает процесс установки обновления через updater.exe
        /// </summary>
        public bool InstallUpdate(string downloadedFilePath, PluginVersionInfo versionInfo)
        {
            try
            {
                LogMessage($"Начинаем установку обновления {versionInfo.Version}...");

                if (!File.Exists(_config.UpdaterPath))
                {
                    LogMessage($"Updater не найден: {_config.UpdaterPath}");
                    return false;
                }

                if (!File.Exists(downloadedFilePath))
                {
                    LogMessage($"Скачанный файл не найден: {downloadedFilePath}");
                    return false;
                }

                // Создаем файл с инструкциями для updater.exe
                var instructionsFile = Path.Combine(_config.PluginDirectory, "update_instructions.json");
                var instructions = new
                {
                    SourceFile = downloadedFilePath,
                    TargetDirectory = _config.PluginDirectory,
                    MainPluginFile = _config.MainPluginFile,
                    NewVersion = versionInfo.Version,
                    BackupDirectory = Path.Combine(_config.PluginDirectory, "Backup"),
                    LogFile = _logFilePath
                };

                File.WriteAllText(instructionsFile, JsonConvert.SerializeObject(instructions, Formatting.Indented));

                // Запускаем updater.exe
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _config.UpdaterPath,
                    Arguments = $"\"{instructionsFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                LogMessage($"Запускаем updater: {startInfo.FileName} {startInfo.Arguments}");
                
                var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    LogMessage("Updater запущен успешно");
                    return true;
                }
                else
                {
                    LogMessage("Не удалось запустить updater");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при установке обновления: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, нужно ли проверять обновления
        /// </summary>
        public bool ShouldCheckForUpdates()
        {
            if (!_config.CheckUpdatesOnStartup)
                return false;

            var timeSinceLastCheck = DateTime.Now - _config.LastCheckTime;
            return timeSinceLastCheck.TotalHours >= _config.CheckIntervalHours;
        }

        /// <summary>
        /// Обновляет время последней проверки
        /// </summary>
        public void UpdateLastCheckTime()
        {
            _config.LastCheckTime = DateTime.Now;
        }

        /// <summary>
        /// Сравнивает версии (простое сравнение строк)
        /// </summary>
        private bool IsNewerVersion(string newVersion, string currentVersion)
        {
            try
            {
                var newVer = new Version(newVersion);
                var currentVer = new Version(currentVersion);
                return newVer > currentVer;
            }
            catch
            {
                // Если не удается распарсить как Version, сравниваем как строки
                return string.Compare(newVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        /// <summary>
        /// Вычисляет SHA256 хеш файла
        /// </summary>
        private async Task<string> ComputeFileHashAsync(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Записывает сообщение в лог
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                
                // Также выводим в консоль для отладки
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}