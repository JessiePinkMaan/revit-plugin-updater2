using System;
using System.Collections.Generic;

namespace RevitPluginUpdater.Client.Models
{
    /// <summary>
    /// Информация о плагине
    /// </summary>
    public class PluginInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UniqueId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PluginVersionInfo> Versions { get; set; } = new List<PluginVersionInfo>();
        public PluginVersionInfo LatestVersion { get; set; }
    }

    /// <summary>
    /// Информация о версии плагина
    /// </summary>
    public class PluginVersionInfo
    {
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}