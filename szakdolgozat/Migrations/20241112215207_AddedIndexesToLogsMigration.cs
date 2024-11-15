using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace szakdolgozat.Migrations
{
    /// <inheritdoc />
    public partial class AddedIndexesToLogsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_TypeID",
                table: "AssetTypes",
                column: "TypeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetLogs_LogID",
                table: "AssetLogs",
                column: "LogID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_TypeID",
                table: "AssetTypes");

            migrationBuilder.DropIndex(
                name: "IX_AssetLogs_LogID",
                table: "AssetLogs");
        }
    }
}
