using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Extensions;
using PSI1.Api.Helpers;
using PSI1.Api.Models;
using PSI1.Api.Services;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly AppDbContext _db;
    private readonly string _picturesDirectory;
    private readonly TimeProvider _timeProvider;
    private readonly ProgressAnalyzer _progressAnalyzer;

    public ProfileController(
        AppDbContext db,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ProgressAnalyzer progressAnalyzer)
    {
        _db = db;
        _timeProvider = timeProvider;
        _progressAnalyzer = progressAnalyzer;
        // tests point Uploads:Path to a temporary foldert herefore they never touch the real Uploads folder
        _picturesDirectory = configuration["Uploads:Path"]
            ?? Path.Combine(environment.ContentRootPath, "Uploads");
    }

    // the current user's profile details; the user comes from the JWT,
    // so another user's profile can't be requested
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        var user = await _db.GetCurrentUserAsync(User);

        return Ok(new ProfileResponse(user.Username, user.Email));
    }

    [HttpPut("username")]
    public async Task<IActionResult> ChangeUsername(ChangeUsernameRequest request)
    {
        var user = await _db.GetCurrentUserAsync(User);

        var username = request.Username.ToLowerInvariant();

        var usernameExists = await _db.Users.AnyAsync(
            u => u.Id != user.Id && u.Username.ToLower() == username);

        if (usernameExists)
        {
            return Conflict(new { message = "Username already taken." });
        }

        user.Username = username;
        await _db.SaveChangesAsync();

        return Ok(new { username = user.Username });
    }

    [HttpGet("streak")]
    public async Task<IActionResult> GetStreak()
    {
        var user = await _db.GetCurrentUserAsync(User);
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        return Ok(new StreakResponse(user.GetCurrentStreak(today)));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProgressResponse>> GetStats([FromQuery] Language? language)
    {
        if (language is null || !EnumValues<Language>.IsDefined(language.Value))
        {
            return BadRequest(new { message = "Unknown language." });
        }

        var userId = User.GetUserId();

        // the user's scores with their answers (answerRows are auto-included)
        var scores = await _db.QuizScores
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var quizIds = scores.Select(s => s.QuizId).ToList();

        // cateogry of each of those quizzes, only in the chosen language
        var categoriesByQuizId = await _db.Quizzes
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(q => quizIds.Contains(q.Id) && q.Language == language.Value)
            .ToDictionaryAsync(q => q.Id, q => q.Category);

        // answers grouped by category of their quiz. Scores of other languages are skipped.
        var counts = scores
            .Where(s => categoriesByQuizId.ContainsKey(s.QuizId))
            .GroupBy(s => categoriesByQuizId[s.QuizId])
            .Select(group => new CategoryAnswerCounts(
                group.Key,
                group.Sum(s => s.Answers.Count),
                group.Sum(s => s.Answers.Count(a => a.Value))))
            .ToList();

        return Ok(_progressAnalyzer.Analyze(counts));
    }

    [HttpGet("picture")]
    public async Task<IActionResult> GetPicture()
    {
        var user = await _db.GetCurrentUserAsync(User);

        var path = Path.Combine(_picturesDirectory, user.ProfilePictureFilename);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { message = "No profile picture uploaded." });
        }

        // the picture is loaded from disk through a stream
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        var contentType = path.EndsWith(".png") ? "image/png" : "image/jpeg";

        return File(stream, contentType);
    }

    // the request limit stops Kestrel from reading huge bodies at all
    [HttpPost("picture")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var user = await _db.GetCurrentUserAsync(User);

        // size is checked on the server
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }
        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "The image must be 2 MB or smaller." });
        }

        // the type is decided from the file's first bytes, the client's ContentType can be faked
        var extension = await DetectImageExtensionAsync(file);
        if (extension is null)
        {
            return BadRequest(new { message = "Only JPEG and PNG images are allowed." });
        }

        Directory.CreateDirectory(_picturesDirectory);
        var filename = $"{user.Id}{extension}";

        // the uploaded file is read as a stream and copied into a file on disk
        await using (var uploadStream = file.OpenReadStream())
        await using (var fileStream = new FileStream(Path.Combine(_picturesDirectory, filename), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            await uploadStream.CopyToAsync(fileStream);
        }

        // delete the previous picture if it had another extension
        foreach (var oldPath in Directory.GetFiles(_picturesDirectory, $"{user.Id}.*"))
        {
            if (!string.Equals(Path.GetFileName(oldPath), filename, StringComparison.OrdinalIgnoreCase))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        user.ProfilePictureFilename = filename;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile picture updated." });
    }

    // reads the first bytes of the file to find out if it is really a PNG or JPEG
    private static async Task<string?> DetectImageExtensionAsync(IFormFile file)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

        if (read >= 8 && header.AsSpan().SequenceEqual(PngSignature))
        {
            return ".png";
        }
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }
        return null;
    }
}