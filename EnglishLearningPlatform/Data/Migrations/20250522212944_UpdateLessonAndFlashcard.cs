using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearningPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLessonAndFlashcard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Example",
                table: "Flashcards",
                newName: "Meaning");

            migrationBuilder.RenameColumn(
                name: "Definition",
                table: "Flashcards",
                newName: "ExampleSentence");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Lessons",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Flashcards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Flashcards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flashcards_LessonId",
                table: "Flashcards",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flashcards_Lessons_LessonId",
                table: "Flashcards",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flashcards_Lessons_LessonId",
                table: "Flashcards");

            migrationBuilder.DropIndex(
                name: "IX_Flashcards_LessonId",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Flashcards");

            migrationBuilder.RenameColumn(
                name: "Meaning",
                table: "Flashcards",
                newName: "Example");

            migrationBuilder.RenameColumn(
                name: "ExampleSentence",
                table: "Flashcards",
                newName: "Definition");
        }
    }
}
