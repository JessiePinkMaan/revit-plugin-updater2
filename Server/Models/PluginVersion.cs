using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RevitPluginUpdater.Server.Models
{
    /// <summary>
    /// Модель версии плагина (упрощенная)
    /// </summary>
    public class PluginVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PluginId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string ReleaseNotes { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        [MaxLength(64)]
        public string FileHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PluginId")]
        public virtual Plugin Plugin { get; set; } = null!;
    }
}