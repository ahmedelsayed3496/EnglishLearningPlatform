using EnglishLearningPlatform.Data;
using EnglishLearningPlatform.Models;
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

        public FlashcardsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List all flashcards for the current user
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var flashcards = await _context.Flashcards
                .Include(f => f.Lesson)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            return View(flashcards);
        }

        // Show form to create a new flashcard (optionally for a lesson)
        public async Task<IActionResult> Create(int? lessonId = null)
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.Lessons = await _context.Lessons
                .Where(l => l.UserId == userId)
                .ToListAsync();
            ViewBag.SelectedLessonId = lessonId;
            return View();
        }

        // Handle form POST to create a new flashcard
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

            _context.Add(flashcard);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint to create a flashcard (e.g., from lesson details)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax([FromBody] Flashcard flashcard)
        {
            if (string.IsNullOrWhiteSpace(flashcard.Word) || string.IsNullOrWhiteSpace(flashcard.Meaning))
                return BadRequest();

            flashcard.UserId = _userManager.GetUserId(User);
            flashcard.CreatedAt = DateTime.UtcNow;

            // Set LessonId if provided
            if (flashcard.LessonId.HasValue)
            {
                // Optionally, validate that the lesson exists and belongs to the user
                var lesson = await _context.Lessons
                    .FirstOrDefaultAsync(l => l.LessonId == flashcard.LessonId && l.UserId == flashcard.UserId);
                if (lesson == null)
                    return BadRequest("Invalid lesson.");
            }

            _context.Add(flashcard);
            await _context.SaveChangesAsync();
            return Ok();
        }


        // Delete a flashcard
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
    }
}
