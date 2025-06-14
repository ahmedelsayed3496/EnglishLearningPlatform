using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearningPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatethemodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Lessons",
                newName: "LessonId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Flashcards",
                newName: "FlashcardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LessonId",
                table: "Lessons",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "FlashcardId",
                table: "Flashcards",
                newName: "Id");
        }
    }
}
