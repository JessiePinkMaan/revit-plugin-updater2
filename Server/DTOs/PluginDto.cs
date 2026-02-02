namespace RevitPluginUpdater.Server.DTOs
{
    /// <summary>
    /// DTO для отображения информации о плагине
    /// </summary>
    public class PluginDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UniqueId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PluginVersionDto> Versions { get; set; } = new();
        public PluginVersionDto? LatestVersion { get; set; }
    }

    /// <summary>
    /// DTO для отображения информации о версии плагина
    /// </summary>
    public class PluginVersionDto
    {
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO для создания нового плагина
    /// </summary>
    public class CreatePluginRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UniqueId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO для загрузки новой версии плагина
    /// </summary>
    public class CreateVersionRequest
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
    }
}