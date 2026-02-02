using System;

namespace RevitPluginUpdater.Client.Models
{
    /// <summary>
    /// Конфигурация системы обновлений
    /// </summary>
    public class UpdateConfig
    {
        /// <summary>
        /// URL сервера обновлений
        /// </summary>
        public string ServerUrl { get; set; } = "https://your-app.onrender.com";

        /// <summary>
        /// Уникальный ID плагина
        /// </summary>
        public string PluginUniqueId { get; set; } = string.Empty;

        /// <summary>
        /// Текущая версия плагина
        /// </summary>
        public string CurrentVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Проверять обновления при запуске Revit
        /// </summary>
        public bool CheckUpdatesOnStartup { get; set; } = true;

        /// <summary>
        /// Автоматически скачивать обновления
        /// </summary>
        public bool AutoDownload { get; set; } = false;

        /// <summary>
        /// Автоматически устанавливать обновления
        /// </summary>
        public bool AutoInstall { get; set; } = false;

        /// <summary>
        /// Показывать уведомления о доступных обновлениях
        /// </summary>
        public bool ShowNotifications { get; set; } = true;

        /// <summary>
        /// Интервал проверки обновлений в часах
        /// </summary>
        public int CheckIntervalHours { get; set; } = 24;

        /// <summary>
        /// Последняя проверка обновлений
        /// </summary>
        public DateTime LastCheckTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Путь к директории плагина
        /// </summary>
        public string PluginDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Имя основного файла плагина
        /// </summary>
        public string MainPluginFile { get; set; } = string.Empty;

        /// <summary>
        /// Путь к updater.exe
        /// </summary>
        public string UpdaterPath { get; set; } = string.Empty;
    }
}