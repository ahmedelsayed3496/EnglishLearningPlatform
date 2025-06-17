using EnglishLearningPlatform.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EnglishLearningPlatform.Models
{
    public class Flashcard
    {
       [Key]
        public int FlashcardId { get; set; }

        [Required]
        public string Word { get; set; }

        public string Meaning { get; set; }

        public string ExampleSentence { get; set; }


        // New audio path properties
        public string? WordAudioPath { get; set; }
        public string? MeaningAudioPath { get; set; }
        public string? ExampleAudioPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Spaced repetition properties
        public DateTime? LastReviewedAt { get; set; }
        public DateTime? NextReviewDue { get; set; }
        public int RepetitionLevel { get; set; } = 0; // 0: New, 1-5: Progression levels
        public int ConsecutiveCorrectAnswers { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;
        public int CorrectReviews { get; set; } = 0;

        [NotMapped]
        public double MasteryPercentage => TotalReviews > 0 ? (double)CorrectReviews / TotalReviews * 100 : 0;


        // Foreign key to ApplicationUser
        public string? UserId { get; set; }



        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public int? LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public Lesson? Lesson { get; set; }
    }
}
