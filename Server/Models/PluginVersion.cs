using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RevitPluginUpdater.Server.Models
{
    /// <summary>
    /// Модель версии плагина
    /// </summary>
    public class PluginVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PluginId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = string.Empty; // Например: "1.0.0"

        [MaxLength(1000)]
        public string ReleaseNotes { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty; // Имя файла на диске

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty; // Путь к файлу на диске

        public long FileSize { get; set; } // Размер файла в байтах

        [Required]
        [MaxLength(64)]
        public string FileHash { get; set; } = string.Empty; // SHA256 хеш файла

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационное свойство
        [ForeignKey("PluginId")]
        public virtual Plugin Plugin { get; set; } = null!;
    }
}