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
    public class LessonsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly TextToSpeechService _textToSpeechService;
        private readonly SpeechToTextService _speechToTextService;

        public LessonsController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            TextToSpeechService textToSpeechService,
            SpeechToTextService speechToTextServic)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _textToSpeechService = textToSpeechService;
            _speechToTextService = speechToTextServic;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = _userManager.GetUserId(User);

            // Define page size (12 cards per page)
            int pageSize = 12;

            // Get total number of lessons for this user
            int totalLessons = await _context.Lessons
                .Where(l => l.UserId == userId)
                .CountAsync();

            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalLessons / (double)pageSize);

            // Ensure page is within valid range
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get paginated lessons
            var lessons = await _context.Lessons
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt) // Most recent first
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create pagination info for the view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            ViewBag.HideFooter = true;
            return View(lessons);
        }


        // GET: Lessons/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile uploadedFile, IFormFile lessonImage, string title)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid text or audio file.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            string textContent = "";
            string audioRelativePath = "";
            string imageRelativePath = "/images/default-lesson.jpg"; // Default image path
            string fileName = Guid.NewGuid().ToString();
            var ext = Path.GetExtension(uploadedFile.FileName).ToLowerInvariant();

            // Process text file or audio file
            if (ext == ".txt")
            {
                // Handle text file: read text and generate audio
                using (var reader = new StreamReader(uploadedFile.OpenReadStream()))
                {
                    textContent = await reader.ReadToEndAsync();
                }
                try
                {
                    audioRelativePath = await _textToSpeechService.GenerateAudioFromTextAsync(textContent, fileName);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Audio generation failed: " + ex.Message);
                    return View();
                }
            }
            else if (ext == ".wav" || ext == ".mp3" || uploadedFile.ContentType.StartsWith("audio/"))
            {
                // Handle audio file: save audio and generate text
                var audioDir = Path.Combine(_environment.WebRootPath, "audio");
                if (!Directory.Exists(audioDir))
                    Directory.CreateDirectory(audioDir);
                var audioPath = Path.Combine(audioDir, fileName + ext);
                using (var stream = new FileStream(audioPath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }
                audioRelativePath = "/audio/" + fileName + ext;

                // If not WAV, convert to WAV for Azure STT
                string wavPath = audioPath;
                if (ext != ".wav")
                {
                    wavPath = Path.Combine(audioDir, fileName + ".wav");
                    // You need to implement audio conversion here if needed
                    // For now, just throw if not WAV
                    ModelState.AddModelError("", "Only WAV audio files are supported for speech-to-text.");
                    return View();
                }

                try
                {
                    textContent = await _speechToTextService.RecognizeSpeechAsync(wavPath);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Speech-to-text failed: " + ex.Message);
                    return View();
                }
            }
            else
            {
                ModelState.AddModelError("", "Unsupported file type.");
                return View();
            }

            // Process lesson image if provided
            if (lessonImage != null && lessonImage.Length > 0)
            {
                var imageExt = Path.GetExtension(lessonImage.FileName).ToLowerInvariant();
                if (imageExt == ".jpg" || imageExt == ".jpeg" || imageExt == ".png" || imageExt == ".gif")
                {
                    var imageDir = Path.Combine(_environment.WebRootPath, "images", "lessons");
                    if (!Directory.Exists(imageDir))
                        Directory.CreateDirectory(imageDir);

                    var imagePath = Path.Combine(imageDir, fileName + imageExt);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await lessonImage.CopyToAsync(stream);
                    }
                    imageRelativePath = "/images/lessons/" + fileName + imageExt;
                }
                else
                {
                    ModelState.AddModelError("", "Only JPG, PNG and GIF images are supported.");
                    return View();
                }
            }

            var lesson = new Lesson
            {
                Title = title,
                TextContent = textContent,
                AudioFilePath = audioRelativePath,
                ImagePath = imageRelativePath,
                UserId = user.Id
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = lesson.LessonId });
        }



        public async Task<IActionResult> Details(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Flashcards)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null) return NotFound();
            ViewBag.HideFooter = true;
           
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgress([FromBody] LessonProgressUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = _userManager.GetUserId(User);
            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.LessonId == model.LessonId && l.UserId == userId);

            if (lesson == null)
            {
                return NotFound();
            }

            // If the lesson is completed and wasn't before, update user stats
            var user = await _userManager.FindByIdAsync(userId);
            if (model.Completed && user != null)
            {
                user.LessonsCompleted += 1;
                await _userManager.UpdateAsync(user);
            }

            // Return success
            return Ok();
        }

    }
}
