using System.ComponentModel.DataAnnotations;

namespace RevitPluginUpdater.Server.Models
{
    /// <summary>
    /// Модель плагина в системе
    /// </summary>
    public class Plugin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UniqueId { get; set; } = string.Empty; // Уникальный идентификатор для Revit

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационное свойство для версий
        public virtual ICollection<PluginVersion> Versions { get; set; } = new List<PluginVersion>();
    }
}