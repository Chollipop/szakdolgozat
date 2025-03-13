using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace szakdolgozat.Migrations
{
    /// <inheritdoc />
    public partial class AddVulnerabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetTypes",
                columns: table => new
                {
                    TypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTypes", x => x.TypeID);
                });

            migrationBuilder.CreateTable(
                name: "Vulnerabilities",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vulnerabilities", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Subtypes",
                columns: table => new
                {
                    TypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetTypeID = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtypes", x => x.TypeID);
                    table.ForeignKey(
                        name: "FK_Subtypes_AssetTypes_AssetTypeID",
                        column: x => x.AssetTypeID,
                        principalTable: "AssetTypes",
                        principalColumn: "TypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    AssetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssetTypeID = table.Column<int>(type: "int", nullable: false),
                    SubtypeID = table.Column<int>(type: "int", nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.AssetID);
                    table.ForeignKey(
                        name: "FK_Assets_AssetTypes_AssetTypeID",
                        column: x => x.AssetTypeID,
                        principalTable: "AssetTypes",
                        principalColumn: "TypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assets_Subtypes_SubtypeID",
                        column: x => x.SubtypeID,
                        principalTable: "Subtypes",
                        principalColumn: "TypeID");
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignments",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetID = table.Column<int>(type: "int", nullable: false),
                    User = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_Assets_AssetID",
                        column: x => x.AssetID,
                        principalTable: "Assets",
                        principalColumn: "AssetID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetID = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLogs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_AssetLogs_Assets_AssetID",
                        column: x => x.AssetID,
                        principalTable: "Assets",
                        principalColumn: "AssetID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetVulnerabilities",
                columns: table => new
                {
                    AssetID = table.Column<int>(type: "int", nullable: false),
                    VulnerabilityID = table.Column<int>(type: "int", nullable: false),
                    Probability = table.Column<int>(type: "int", nullable: false),
                    Impact = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetVulnerabilities", x => new { x.AssetID, x.VulnerabilityID });
                    table.ForeignKey(
                        name: "FK_AssetVulnerabilities_Assets_AssetID",
                        column: x => x.AssetID,
                        principalTable: "Assets",
                        principalColumn: "AssetID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetVulnerabilities_Vulnerabilities_VulnerabilityID",
                        column: x => x.VulnerabilityID,
                        principalTable: "Vulnerabilities",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_AssetID",
                table: "AssetAssignments",
                column: "AssetID");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_AssignmentID",
                table: "AssetAssignments",
                column: "AssignmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetLogs_AssetID",
                table: "AssetLogs",
                column: "AssetID");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLogs_LogID",
                table: "AssetLogs",
                column: "LogID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetID",
                table: "Assets",
                column: "AssetID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTypeID",
                table: "Assets",
                column: "AssetTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_SubtypeID",
                table: "Assets",
                column: "SubtypeID");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_TypeID",
                table: "AssetTypes",
                column: "TypeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetVulnerabilities_AssetID",
                table: "AssetVulnerabilities",
                column: "AssetID");

            migrationBuilder.CreateIndex(
                name: "IX_AssetVulnerabilities_VulnerabilityID",
                table: "AssetVulnerabilities",
                column: "VulnerabilityID");

            migrationBuilder.CreateIndex(
                name: "IX_Subtypes_AssetTypeID",
                table: "Subtypes",
                column: "AssetTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Subtypes_TypeID",
                table: "Subtypes",
                column: "TypeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vulnerabilities_ID",
                table: "Vulnerabilities",
                column: "ID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "AssetLogs");

            migrationBuilder.DropTable(
                name: "AssetVulnerabilities");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Vulnerabilities");

            migrationBuilder.DropTable(
                name: "Subtypes");

            migrationBuilder.DropTable(
                name: "AssetTypes");
        }
    }
}
