using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingPlatform.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoPlaybackPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_position_seconds",
                table: "document_progress",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_watched_at_utc",
                table: "document_progress",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_position_seconds",
                table: "document_progress");

            migrationBuilder.DropColumn(
                name: "last_watched_at_utc",
                table: "document_progress");
        }
    }
}
