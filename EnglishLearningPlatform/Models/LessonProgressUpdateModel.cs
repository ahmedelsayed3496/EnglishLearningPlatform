namespace EnglishLearningPlatform.Models
{
    public class LessonProgressUpdateModel
    {
        public int LessonId { get; set; }
        public int PercentCompleted { get; set; }
        public bool Completed { get; set; }
    }
}
