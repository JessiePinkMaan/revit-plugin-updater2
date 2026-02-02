using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using RevitPluginUpdater.Client.Models;
using RevitPluginUpdater.Client.Services;

namespace RevitPluginUpdater.Client
{
    /// <summary>
    /// Основной класс для управления обновлениями плагина
    /// </summary>
    public class UpdateManager : IDisposable
    {
        private readonly ConfigService _configService;
        private readonly UpdateService _updateService;
        private readonly string _pluginDirectory;
        private bool _disposed = false;

        public UpdateManager(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            
            // Инициализируем сервисы
            _configService = new ConfigService(_pluginDirectory);
            _updateService = new UpdateService(_configService.GetConfig());
        }

        /// <summary>
        /// Событие для уведомления о доступном обновлении
        /// </summary>
        public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

        /// <summary>
        /// Событие для уведомления о прогрессе скачивания
        /// </summary>
        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        /// <summary>
        /// Событие для уведомления об ошибках
        /// </summary>
        public event EventHandler<UpdateErrorEventArgs> UpdateError;

        /// <summary>
        /// Проверяет обновления при запуске плагина
        /// </summary>
        public async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                var config = _configService.GetConfig();

                // Проверяем конфигурацию
                if (!_configService.IsConfigurationValid())
                {
                    LogMessage("Конфигурация обновлений не настроена");
                    return;
                }

                // Проверяем, нужно ли проверять обновления
                if (!_updateService.ShouldCheckForUpdates())
                {
                    LogMessage("Проверка обновлений пропущена (слишком рано)");
                    return;
                }

                LogMessage("Проверка обновлений при запуске...");

                // Проверяем обновления
                var latestVersion = await _updateService.CheckForUpdatesAsync();
                _updateService.UpdateLastCheckTime();
                _configService.UpdateLastCheckTime();

                if (latestVersion != null)
                {
                    LogMessage($"Найдено обновление: {latestVersion.Version}");
                    
                    // Уведомляем о доступном обновлении
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(latestVersion));

                    // Показываем уведомление пользователю
                    if (config.ShowNotifications)
                    {
                        ShowUpdateNotification(latestVersion);
                    }

                    // Автоматическое скачивание
                    if (config.AutoDownload)
                    {
                        await DownloadAndInstallUpdateAsync(latestVersion, config.AutoInstall);
                    }
                }
                else
                {
                    LogMessage("Обновления не найдены");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при проверке обновлений: {ex.Message}");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex));
            }
        }

        /// <summary>
        /// Принудительно проверяет обновления
        /// </summary>
        public async Task<PluginVersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                if (!_configService.IsConfigurationValid())
                {
                    throw new InvalidOperationException("Конфигурация обновлений не настроена");
                }

                LogMessage("Принудительная проверка обновлений...");
                
                var latestVersion = await _updateService.CheckForUpdatesAsync();
                _updateService.UpdateLastCheckTime();
                _configService.UpdateLastCheckTime();

                return latestVersion;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при проверке обновлений: {ex.Message}");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex));
                throw;
            }
        }

        /// <summary>
        /// Скачивает и устанавливает обновление
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync(PluginVersionInfo versionInfo, bool autoInstall = false)
        {
            try
            {
                LogMessage($"Начинаем скачивание обновления {versionInfo.Version}...");

                // Создаем прогресс для отслеживания скачивания
                var progress = new Progress<int>(percentage =>
                {
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(percentage));
                });

                // Скачиваем файл
                var downloadedFilePath = await _updateService.DownloadUpdateAsync(versionInfo, progress);
                
                if (string.IsNullOrEmpty(downloadedFilePath))
                {
                    throw new Exception("Не удалось скачать файл обновления");
                }

                LogMessage($"Файл скачан: {downloadedFilePath}");

                // Устанавливаем обновление
                if (autoInstall)
                {
                    LogMessage("Автоматическая установка обновления...");
                    
                    var installResult = _updateService.InstallUpdate(downloadedFilePath, versionInfo);
                    if (installResult)
                    {
                        LogMessage("Обновление установлено успешно");
                        
                        // Обновляем версию в конфигурации
                        _configService.UpdateCurrentVersion(versionInfo.Version);
                        
                        return true;
                    }
                    else
                    {
                        throw new Exception("Не удалось установить обновление");
                    }
                }
                else
                {
                    LogMessage("Файл скачан, ожидается ручная установка");
                    
                    // Показываем диалог для ручной установки
                    ShowInstallDialog(downloadedFilePath, versionInfo);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при скачивании/установке обновления: {ex.Message}");
                UpdateError?.Invoke(this, new UpdateErrorEventArgs(ex));
                return false;
            }
        }

        /// <summary>
        /// Показывает уведомление о доступном обновлении
        /// </summary>
        private void ShowUpdateNotification(PluginVersionInfo versionInfo)
        {
            try
            {
                var message = $"Доступно обновление плагина до версии {versionInfo.Version}.\n\n" +
                             $"Описание изменений:\n{versionInfo.ReleaseNotes}\n\n" +
                             $"Размер файла: {FormatFileSize(versionInfo.FileSize)}\n\n" +
                             "Хотите скачать и установить обновление?";

                var result = MessageBox.Show(message, "Обновление плагина", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    // Запускаем скачивание в фоновом режиме
                    Task.Run(async () =>
                    {
                        await DownloadAndInstallUpdateAsync(versionInfo, false);
                    });
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при показе уведомления: {ex.Message}");
            }
        }

        /// <summary>
        /// Показывает диалог для ручной установки
        /// </summary>
        private void ShowInstallDialog(string downloadedFilePath, PluginVersionInfo versionInfo)
        {
            try
            {
                var message = $"Обновление {versionInfo.Version} скачано успешно.\n\n" +
                             $"Файл сохранен: {downloadedFilePath}\n\n" +
                             "Для завершения установки необходимо перезапустить Revit.\n" +
                             "Установить обновление сейчас?";

                var result = MessageBox.Show(message, "Установка обновления", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var installResult = _updateService.InstallUpdate(downloadedFilePath, versionInfo);
                    if (installResult)
                    {
                        _configService.UpdateCurrentVersion(versionInfo.Version);
                        
                        MessageBox.Show("Обновление будет установлено при следующем запуске Revit.", 
                            "Установка запланирована", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось запустить процесс установки.", 
                            "Ошибка установки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при показе диалога установки: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает текущую конфигурацию
        /// </summary>
        public UpdateConfig GetConfig()
        {
            return _configService.GetConfig();
        }

        /// <summary>
        /// Сохраняет конфигурацию
        /// </summary>
        public void SaveConfig(UpdateConfig config)
        {
            _configService.SaveConfig(config);
        }

        /// <summary>
        /// Проверяет, правильно ли настроена конфигурация
        /// </summary>
        public bool IsConfigurationValid()
        {
            return _configService.IsConfigurationValid();
        }

        /// <summary>
        /// Получает описание проблем конфигурации
        /// </summary>
        public string GetConfigurationIssues()
        {
            return _configService.GetConfigurationIssues();
        }

        /// <summary>
        /// Форматирует размер файла
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Записывает сообщение в лог
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                var logDir = Path.Combine(_pluginDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logFile = Path.Combine(logDir, $"update_manager_{DateTime.Now:yyyyMMdd}.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _updateService?.Dispose();
                _disposed = true;
            }
        }
    }

    // Классы для событий
    public class UpdateAvailableEventArgs : EventArgs
    {
        public PluginVersionInfo VersionInfo { get; }

        public UpdateAvailableEventArgs(PluginVersionInfo versionInfo)
        {
            VersionInfo = versionInfo;
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }

        public DownloadProgressEventArgs(int progressPercentage)
        {
            ProgressPercentage = progressPercentage;
        }
    }

    public class UpdateErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public UpdateErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}