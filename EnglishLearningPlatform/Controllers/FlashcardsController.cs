using EnglishLearningPlatform.Data;
using EnglishLearningPlatform.Models;
using EnglishLearningPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearningPlatform.Controllers
{
    [Authorize]
    public class FlashcardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TextToSpeechService _textToSpeechService;

        public FlashcardsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        TextToSpeechService textToSpeechService)
        {
            _context = context;
            _userManager = userManager;
            _textToSpeechService = textToSpeechService;
        }

        // GET: Flashcards
        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = _userManager.GetUserId(User);

            // Set page size
            int pageSize = 12;

            // Get total count for pagination
            var totalCards = await _context.Flashcards
                .Where(f => f.UserId == userId)
                .CountAsync();

            // Get current page of flashcards
            var flashcards = await _context.Flashcards
                .Include(f => f.Lesson)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Set ViewBag values for pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalCards = totalCards;

            return View(flashcards);
        }


        public async Task<IActionResult> Create(int? lessonId = null)
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.Lessons = await _context.Lessons
                .Where(l => l.UserId == userId)
                .ToListAsync();
            ViewBag.SelectedLessonId = lessonId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Flashcard flashcard)
        {
            flashcard.UserId = _userManager.GetUserId(User);
            flashcard.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.Lessons = await _context.Lessons
                    .Where(l => l.UserId == userId)
                    .ToListAsync();
                ViewBag.SelectedLessonId = flashcard.LessonId;
                return View(flashcard);
            }

            try
            {
                flashcard.WordAudioPath = await _textToSpeechService.GenerateFlashcardAudioAsync(flashcard.Word);

                if (!string.IsNullOrEmpty(flashcard.Meaning))
                    flashcard.MeaningAudioPath = await _textToSpeechService.GenerateFlashcardAudioAsync(flashcard.Meaning);

                if (!string.IsNullOrEmpty(flashcard.ExampleSentence))
                    flashcard.ExampleAudioPath = await _textToSpeechService.GenerateFlashcardAudioAsync(flashcard.ExampleSentence);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating audio: {ex.Message}");
            }

            _context.Add(flashcard);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([FromBody] Flashcard flashcard)
        {
            if (string.IsNullOrWhiteSpace(flashcard.Word) || string.IsNullOrWhiteSpace(flashcard.Meaning))
                return BadRequest();

            flashcard.UserId = _userManager.GetUserId(User);
            flashcard.CreatedAt = DateTime.UtcNow;

            try
            {
                flashcard.WordAudioPath = await _textToSpeechService.GenerateFlashcardAudioAsync(flashcard.Word);

                if (!string.IsNullOrEmpty(flashcard.Meaning))
                    flashcard.MeaningAudioPath = await _textToSpeechService.GenerateFlashcardAudioAsync(flashcard.Meaning);

                if (!string.IsNullOrEmpty(flashcard.ExampleSentence))
                    flashcard.ExampleAudioPath = await _textToSpeechService.GenerateFlashcardAudioAsync(flashcard.ExampleSentence);
            }
            catch (Exception)
            {
                // Silent catch for audio generation issues
            }

            if (flashcard.LessonId.HasValue)
            {
                var lesson = await _context.Lessons
                     .FirstOrDefaultAsync(l => l.LessonId == flashcard.LessonId && l.UserId == flashcard.UserId);
                if (lesson == null)
                    return BadRequest("Invalid lesson.");
            }

            _context.Add(flashcard);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var flashcard = await _context.Flashcards.FindAsync(id);
            if (flashcard?.UserId == _userManager.GetUserId(User))
            {
                _context.Flashcards.Remove(flashcard);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAudio([FromBody] TextToSpeechRequest request)
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest(new { error = "Text is required" });
            }

            try
            {
                // Create a deterministic filename based on the text's hash
                string hash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(request.Text)
                    )
                ).Replace('/', '_').Replace('+', '-').Substring(0, 20);

                string fileName = $"cache_{hash}";
                string audioFilePath = Path.Combine("wwwroot/audio", fileName + ".wav");
                string relativePath = $"/audio/{fileName}.wav";

                // Check if the audio file already exists (cache hit)
                if (!System.IO.File.Exists(audioFilePath))
                {
                    // File doesn't exist, generate new audio
                    await _textToSpeechService.GenerateAudioFromTextAsync(request.Text, fileName);
                }

                return Json(new { audioPath = relativePath, cached = System.IO.File.Exists(audioFilePath) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // SPACED REPETITION IMPLEMENTATION
        // GET: Flashcards/Study
        public async Task<IActionResult> Study()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var today = DateTime.UtcNow.Date;

                // Set default values
                ViewBag.TotalCards = 0;
                ViewBag.NewCards = 0;
                ViewBag.DueForReview = 0;
                ViewBag.Mastered = 0;
                ViewBag.StudyProgress = 0.0;

                // If there's a user ID, then get the actual data
                if (!string.IsNullOrEmpty(userId))
                {
                    // Get total cards
                    var totalCards = await _context.Flashcards
                        .Where(f => f.UserId == userId)
                        .CountAsync();

                    // Get new cards (never reviewed)
                    var newCards = await _context.Flashcards
                        .Where(f => f.UserId == userId && f.RepetitionLevel == 0)
                        .CountAsync();

                    // Get cards due for review
                    var dueForReview = await _context.Flashcards
                        .Where(f => f.UserId == userId &&
                                f.RepetitionLevel > 0 &&
                                f.NextReviewDue <= today)
                        .CountAsync();

                    // Get mastered cards (level 4 or higher)
                    var mastered = await _context.Flashcards
                        .Where(f => f.UserId == userId && f.RepetitionLevel >= 4)
                        .CountAsync();

                    // Calculate study progress based on mastery
                    double studyProgress = totalCards > 0 ? (double)mastered / totalCards * 100 : 0;

                    // Update ViewBag values
                    ViewBag.TotalCards = totalCards;
                    ViewBag.NewCards = newCards;
                    ViewBag.DueForReview = dueForReview;
                    ViewBag.Mastered = mastered;
                    ViewBag.StudyProgress = studyProgress;
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in Study method: {ex.Message}");

                // Provide safe defaults
                ViewBag.TotalCards = 0;
                ViewBag.NewCards = 0;
                ViewBag.DueForReview = 0;
                ViewBag.Mastered = 0;
                ViewBag.StudyProgress = 0.0;

                TempData["Message"] = "There was an error loading your study data. Please try again later.";
                return View();
            }
        }

        // GET: Flashcards/StudySession
        public async Task<IActionResult> StudySession(string mode = "all")
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var today = DateTime.UtcNow.Date;
                List<Flashcard> cardsToStudy;

                switch (mode.ToLower())
                {
                    case "new":
                        // Only new cards
                        cardsToStudy = await _context.Flashcards
                            .Where(f => f.UserId == userId && f.RepetitionLevel == 0)
                            .OrderBy(f => f.CreatedAt)
                            .Take(20)
                            .ToListAsync();
                        break;

                    case "due":
                        // Only cards due for review
                        cardsToStudy = await _context.Flashcards
                            .Where(f => f.UserId == userId &&
                                  f.RepetitionLevel > 0 &&
                                  f.NextReviewDue <= today)
                            .OrderBy(f => f.NextReviewDue)
                            .Take(20)
                            .ToListAsync();
                        break;

                    case "all":
                    default:
                        // Mix of new and review cards
                        var dueCards = await _context.Flashcards
                            .Where(f => f.UserId == userId &&
                                  f.RepetitionLevel > 0 &&
                                  f.NextReviewDue <= today)
                            .OrderBy(f => f.NextReviewDue)
                            .Take(15)
                            .ToListAsync();

                        var newCards = await _context.Flashcards
                            .Where(f => f.UserId == userId && f.RepetitionLevel == 0)
                            .OrderBy(f => Guid.NewGuid())
                            .Take(10)
                            .ToListAsync();

                        cardsToStudy = new List<Flashcard>();
                        cardsToStudy.AddRange(dueCards);
                        cardsToStudy.AddRange(newCards);
                        break;
                }

                // If no cards to study, redirect with message
                if (cardsToStudy.Count == 0)
                {
                    TempData["Message"] = "No flashcards are available to study at this time.";
                    return RedirectToAction(nameof(Study));
                }

                // Shuffle the cards
                cardsToStudy = cardsToStudy.OrderBy(c => Guid.NewGuid()).ToList();

                ViewBag.Mode = mode;
                ViewBag.CardCount = cardsToStudy.Count;
                return View(cardsToStudy);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in StudySession method: {ex.Message}");
                TempData["Message"] = "There was an error loading the study session.";
                return RedirectToAction(nameof(Study));
            }
        }

        // POST: Flashcards/SubmitAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAnswer([FromBody] AnswerRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                var userId = _userManager.GetUserId(User);
                var flashcard = await _context.Flashcards
                    .FirstOrDefaultAsync(f => f.FlashcardId == request.FlashcardId && f.UserId == userId);

                if (flashcard == null)
                {
                    return NotFound(new { success = false, message = "Flashcard not found" });
                }

                // Update flashcard statistics
                flashcard.LastReviewedAt = DateTime.UtcNow;
                flashcard.TotalReviews++;

                if (request.IsCorrect)
                {
                    flashcard.CorrectReviews++;
                    flashcard.ConsecutiveCorrectAnswers++;

                    // Update repetition level using spaced repetition algorithm
                    if (flashcard.ConsecutiveCorrectAnswers >= 2)
                    {
                        // Progress to next level (max 5)
                        flashcard.RepetitionLevel = Math.Min(5, flashcard.RepetitionLevel + 1);
                        flashcard.ConsecutiveCorrectAnswers = 0; // Reset consecutive counter
                    }
                }
                else
                {
                    // Reset consecutive counter on wrong answer
                    flashcard.ConsecutiveCorrectAnswers = 0;

                    // Demote to previous level, but not below 1 if already reviewed
                    if (flashcard.RepetitionLevel > 0)
                    {
                        flashcard.RepetitionLevel = Math.Max(1, flashcard.RepetitionLevel - 1);
                    }
                    else
                    {
                        // New card (level 0) becomes level 1 even if wrong to start tracking
                        flashcard.RepetitionLevel = 1;
                    }
                }

                // Calculate next review date based on repetition level
                flashcard.NextReviewDue = CalculateNextReviewDate(flashcard.RepetitionLevel);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in SubmitAnswer: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Server error" });
            }
        }

        // Calculate the next review date based on spaced repetition algorithm
        private DateTime CalculateNextReviewDate(int repetitionLevel)
        {
            var today = DateTime.UtcNow.Date;

            return repetitionLevel switch
            {
                0 => today, // New card - due immediately
                1 => today.AddDays(1), // Level 1 - review after 1 day
                2 => today.AddDays(3), // Level 2 - review after 3 days
                3 => today.AddDays(7), // Level 3 - review after 1 week
                4 => today.AddDays(14), // Level 4 - review after 2 weeks
                _ => today.AddDays(30), // Level 5+ - review after 1 month
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetStudyData(string mode = "all")
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var today = DateTime.UtcNow.Date;
                List<Flashcard> cardsToStudy;

                switch (mode.ToLower())
                {
                    case "new":
                        // Only new cards
                        cardsToStudy = await _context.Flashcards
                            .Where(f => f.UserId == userId && f.RepetitionLevel == 0)
                            .OrderBy(f => Guid.NewGuid())
                            .Take(20)
                            .ToListAsync();
                        break;

                    case "due":
                        // Only cards due for review
                        cardsToStudy = await _context.Flashcards
                            .Where(f => f.UserId == userId &&
                                  f.RepetitionLevel > 0 &&
                                  f.NextReviewDue <= today)
                            .OrderBy(f => Guid.NewGuid())
                            .Take(20)
                            .ToListAsync();
                        break;

                    case "all":
                    default:
                        // Mix of new and due cards
                        var reviewCards = await _context.Flashcards
                            .Where(f => f.UserId == userId &&
                                  f.RepetitionLevel > 0 &&
                                  f.NextReviewDue <= today)
                            .OrderBy(f => Guid.NewGuid())
                            .Take(15)
                            .ToListAsync();

                        var newCards = await _context.Flashcards
                            .Where(f => f.UserId == userId && f.RepetitionLevel == 0)
                            .OrderBy(f => Guid.NewGuid())
                            .Take(10)
                            .ToListAsync();

                        cardsToStudy = new List<Flashcard>();
                        cardsToStudy.AddRange(reviewCards);
                        cardsToStudy.AddRange(newCards);
                        cardsToStudy = cardsToStudy.OrderBy(c => Guid.NewGuid()).ToList();
                        break;
                }

                return Json(new { success = true, flashcards = cardsToStudy });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public class AnswerRequest
        {
            public int FlashcardId { get; set; }
            public bool IsCorrect { get; set; }
        }

        public class TextToSpeechRequest
        {
            public string Text { get; set; }
        }
    }
}

