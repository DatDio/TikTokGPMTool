using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikTokGPMTool.Migrations
{
    /// <inheritdoc />
    public partial class AddTikTokActionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TikTokActionHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GpmProfileId = table.Column<string>(type: "TEXT", nullable: false),
                    VideoId = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Keyword = table.Column<string>(type: "TEXT", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TikTokActionHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TikTokActionHistories_GpmProfileId_VideoId_ActionType",
                table: "TikTokActionHistories",
                columns: new[] { "GpmProfileId", "VideoId", "ActionType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TikTokActionHistories");
        }
    }
}
