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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to ApplicationUser
        public string? UserId { get; set; }



        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public int? LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public Lesson? Lesson { get; set; }
    }
}
