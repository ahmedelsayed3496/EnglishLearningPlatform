using EnglishLearningPlatform.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EnglishLearningPlatform.Models
{
    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string TextContent { get; set; }

        public string AudioFilePath { get; set; }
        public string ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to ApplicationUser
        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        // Navigation property for related flashcards
        public virtual ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
    }
}
