using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearningPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpacedRepetitionToFlashcards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCorrectAnswers",
                table: "Flashcards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CorrectReviews",
                table: "Flashcards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewedAt",
                table: "Flashcards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDue",
                table: "Flashcards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepetitionLevel",
                table: "Flashcards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "Flashcards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveCorrectAnswers",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "CorrectReviews",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "LastReviewedAt",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "NextReviewDue",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "RepetitionLevel",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "Flashcards");
        }
    }
}
