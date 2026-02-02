using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevitPluginUpdater.Server.Migrations
{
    /// <inheritdoc />
    public partial class SQLiteInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plugins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UniqueId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plugins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PluginVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PluginId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PluginVersions_Plugins_PluginId",
                        column: x => x.PluginId,
                        principalTable: "Plugins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Plugins",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UniqueId", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2024, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Пример плагина для демонстрации", "Sample Plugin", "sample-plugin-001", new DateTime(2024, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_UniqueId",
                table: "Plugins",
                column: "UniqueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PluginVersions_PluginId",
                table: "PluginVersions",
                column: "PluginId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginVersions_PluginId_Version",
                table: "PluginVersions",
                columns: new[] { "PluginId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginVersions");

            migrationBuilder.DropTable(
                name: "Plugins");
        }
    }
}