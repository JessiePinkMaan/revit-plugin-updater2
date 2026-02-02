using Microsoft.EntityFrameworkCore;
using RevitPluginUpdater.Server.Models;

namespace RevitPluginUpdater.Server.Data
{
    /// <summary>
    /// Контекст базы данных для Entity Framework
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Plugin> Plugins { get; set; }
        public DbSet<PluginVersion> PluginVersions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка индексов для оптимизации запросов
            modelBuilder.Entity<Plugin>()
                .HasIndex(p => p.UniqueId)
                .IsUnique();

            modelBuilder.Entity<PluginVersion>()
                .HasIndex(pv => new { pv.PluginId, pv.Version })
                .IsUnique();

            // Настройка связей
            modelBuilder.Entity<PluginVersion>()
                .HasOne(pv => pv.Plugin)
                .WithMany(p => p.Versions)
                .HasForeignKey(pv => pv.PluginId)
                .OnDelete(DeleteBehavior.Cascade);

            // Начальные данные для тестирования
            modelBuilder.Entity<Plugin>().HasData(
                new Plugin
                {
                    Id = 1,
                    Name = "Sample Plugin",
                    Description = "Пример плагина для демонстрации",
                    UniqueId = "sample-plugin-001",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}