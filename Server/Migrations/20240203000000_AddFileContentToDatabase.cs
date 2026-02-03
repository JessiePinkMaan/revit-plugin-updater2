using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevitPluginUpdater.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddFileContentToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileContent",
                table: "PluginVersions",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileContent",
                table: "PluginVersions");
        }
    }
}