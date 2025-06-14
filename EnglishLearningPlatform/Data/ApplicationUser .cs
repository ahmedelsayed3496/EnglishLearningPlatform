using EnglishLearningPlatform.Models;
using Microsoft.AspNetCore.Identity;

namespace EnglishLearningPlatform.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int LessonsCompleted { get; set; }
        public int FlashcardsCreated { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalLoginDays { get; set; }

        public virtual ICollection<Lesson> Lessons { get; set; }
        public virtual ICollection<Flashcard> Flashcards { get; set; }
    }
}
