using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingPlatform.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentLessonDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                table: "documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "key_takeaway",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "summary_text",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transcript_text",
                table: "documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "duration_minutes",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "key_takeaway",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "summary_text",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "transcript_text",
                table: "documents");
        }
    }
}
