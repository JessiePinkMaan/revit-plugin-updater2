using System;
using System.IO;
using Newtonsoft.Json;
using RevitPluginUpdater.Client.Models;

namespace RevitPluginUpdater.Client.Services
{
    /// <summary>
    /// Сервис для работы с конфигурацией обновлений
    /// </summary>
    public class ConfigService
    {
        private readonly string _configFilePath;
        private UpdateConfig _config;

        public ConfigService(string pluginDirectory)
        {
            if (string.IsNullOrEmpty(pluginDirectory))
                throw new ArgumentException("Plugin directory cannot be null or empty", nameof(pluginDirectory));

            _configFilePath = Path.Combine(pluginDirectory, "UpdateConfig.json");
            LoadConfig();
        }

        /// <summary>
        /// Получает текущую конфигурацию
        /// </summary>
        public UpdateConfig GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// Сохраняет конфигурацию
        /// </summary>
        public void SaveConfig(UpdateConfig config)
        {
            try
            {
                _config = config ?? throw new ArgumentNullException(nameof(config));
                
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
                
                LogMessage("Конфигурация сохранена");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при сохранении конфигурации: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Обновляет время последней проверки
        /// </summary>
        public void UpdateLastCheckTime()
        {
            _config.LastCheckTime = DateTime.Now;
            SaveConfig(_config);
        }

        /// <summary>
        /// Обновляет текущую версию плагина
        /// </summary>
        public void UpdateCurrentVersion(string newVersion)
        {
            _config.CurrentVersion = newVersion;
            SaveConfig(_config);
        }

        /// <summary>
        /// Загружает конфигурацию из файла или создает по умолчанию
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    _config = JsonConvert.DeserializeObject<UpdateConfig>(json);
                    
                    // Проверяем и дополняем конфигурацию
                    ValidateAndFixConfig();
                    
                    LogMessage("Конфигурация загружена из файла");
                }
                else
                {
                    // Создаем конфигурацию по умолчанию
                    _config = CreateDefaultConfig();
                    SaveConfig(_config);
                    
                    LogMessage("Создана конфигурация по умолчанию");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка при загрузке конфигурации: {ex.Message}");
                
                // В случае ошибки создаем конфигурацию по умолчанию
                _config = CreateDefaultConfig();
            }
        }

        /// <summary>
        /// Создает конфигурацию по умолчанию
        /// </summary>
        private UpdateConfig CreateDefaultConfig()
        {
            var pluginDir = Path.GetDirectoryName(_configFilePath);
            
            return new UpdateConfig
            {
                ServerUrl = "https://your-app.onrender.com",
                PluginUniqueId = "your-plugin-unique-id", // Должно быть установлено разработчиком
                CurrentVersion = "1.0.0",
                CheckUpdatesOnStartup = true,
                AutoDownload = false,
                AutoInstall = false,
                ShowNotifications = true,
                CheckIntervalHours = 24,
                LastCheckTime = DateTime.MinValue,
                PluginDirectory = pluginDir,
                MainPluginFile = "YourPlugin.dll", // Должно быть установлено разработчиком
                UpdaterPath = Path.Combine(pluginDir, "updater.exe")
            };
        }

        /// <summary>
        /// Проверяет и исправляет конфигурацию
        /// </summary>
        private void ValidateAndFixConfig()
        {
            var needsSave = false;

            // Проверяем обязательные поля
            if (string.IsNullOrEmpty(_config.ServerUrl))
            {
                _config.ServerUrl = "https://your-app.onrender.com";
                needsSave = true;
            }

            if (string.IsNullOrEmpty(_config.CurrentVersion))
            {
                _config.CurrentVersion = "1.0.0";
                needsSave = true;
            }

            if (_config.CheckIntervalHours <= 0)
            {
                _config.CheckIntervalHours = 24;
                needsSave = true;
            }

            if (string.IsNullOrEmpty(_config.PluginDirectory))
            {
                _config.PluginDirectory = Path.GetDirectoryName(_configFilePath);
                needsSave = true;
            }

            if (string.IsNullOrEmpty(_config.UpdaterPath))
            {
                _config.UpdaterPath = Path.Combine(_config.PluginDirectory, "updater.exe");
                needsSave = true;
            }

            // Сохраняем если были изменения
            if (needsSave)
            {
                SaveConfig(_config);
            }
        }

        /// <summary>
        /// Проверяет, правильно ли настроена конфигурация
        /// </summary>
        public bool IsConfigurationValid()
        {
            return !string.IsNullOrEmpty(_config.PluginUniqueId) &&
                   !string.IsNullOrEmpty(_config.MainPluginFile) &&
                   !string.IsNullOrEmpty(_config.ServerUrl) &&
                   _config.PluginUniqueId != "your-plugin-unique-id" &&
                   _config.MainPluginFile != "YourPlugin.dll";
        }

        /// <summary>
        /// Получает сообщения о проблемах конфигурации
        /// </summary>
        public string GetConfigurationIssues()
        {
            var issues = new System.Text.StringBuilder();

            if (string.IsNullOrEmpty(_config.PluginUniqueId) || _config.PluginUniqueId == "your-plugin-unique-id")
            {
                issues.AppendLine("- Не установлен уникальный ID плагина (PluginUniqueId)");
            }

            if (string.IsNullOrEmpty(_config.MainPluginFile) || _config.MainPluginFile == "YourPlugin.dll")
            {
                issues.AppendLine("- Не установлено имя основного файла плагина (MainPluginFile)");
            }

            if (string.IsNullOrEmpty(_config.ServerUrl))
            {
                issues.AppendLine("- Не установлен URL сервера обновлений (ServerUrl)");
            }

            if (!File.Exists(_config.UpdaterPath))
            {
                issues.AppendLine($"- Не найден файл updater.exe: {_config.UpdaterPath}");
            }

            return issues.ToString();
        }

        /// <summary>
        /// Записывает сообщение в лог
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                var logDir = Path.Combine(_config?.PluginDirectory ?? Path.GetDirectoryName(_configFilePath), "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logFile = Path.Combine(logDir, $"config_{DateTime.Now:yyyyMMdd}.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }
    }
}