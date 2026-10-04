using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikTokGPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookGroupScanHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacebookGroupScanHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupKey = table.Column<string>(type: "TEXT", nullable: false),
                    GpmProfileId = table.Column<string>(type: "TEXT", nullable: false),
                    FacebookUid = table.Column<string>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacebookGroupScanHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacebookGroupScanHistories_GroupKey_FacebookUid",
                table: "FacebookGroupScanHistories",
                columns: new[] { "GroupKey", "FacebookUid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacebookGroupScanHistories");
        }
    }
}
