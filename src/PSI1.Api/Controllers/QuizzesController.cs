using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Import;
using PSI1.Api.Models;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizzesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JsonQuizImporter _importer;

    public QuizzesController(AppDbContext db, JsonQuizImporter importer)
    {
        _db = db;
        _importer = importer;
    }

    [HttpGet]
    public async Task<ActionResult<List<QuizSummaryResponse>>> GetAll(
    [FromQuery] Category? category,
    [FromQuery] Language? language)
    {
        //validation
        if (category.HasValue && !Enum.IsDefined(category.Value))
        {
            return BadRequest(new { message = "Unknown category." });
        }
        if (language.HasValue && !Enum.IsDefined(language.Value))
        {
            return BadRequest(new { message = "Unknown language." });
        }

        var query = _db.Quizzes.AsNoTracking();

        // filters are independent: each one only applies if it was provided
        if (category.HasValue)
        {
            query = query.Where(q => q.Category == category.Value);
        }
        if (language.HasValue)
        {
            query = query.Where(q => q.Language == language.Value);
        }

        var quizzes = await query
         .OrderBy(q => q.Id)
         .Select(q => new QuizSummaryResponse(
             q.Id,
             q.Title,
             q.Description,
             q.Category,
             q.Language,
             EF.Property<ICollection<Question>>(q, "questions").Count))
         .ToListAsync();

        return Ok(quizzes);
    }

    [Authorize]
    [HttpGet("{quizId}/questions/{index}")]
    public async Task<ActionResult<QuestionPlayResponse>> GetQuestion(int quizId, int index)
    {

        var quiz = await _db.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
            return NotFound(new { message = "Quiz not found." });

        var question = quiz.Questions.FirstOrDefault(q => q.Position == index);

        if (question is null)
            return NotFound(new { message = "Question not found." });

        var response = new QuestionPlayResponse(
            question.Position,
            question.QuestionText,
            question.Options.Select(o => o.OptionText).ToList()
        );

        return Ok(response);
    }

    [Authorize]
    [HttpPost("{quizId}/questions/{index}/answer")]
    public async Task<ActionResult<AnswerResultResponse>> AnswerQuestion(int quizId, int index, AnswerRequest request)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing token." });
        }

        var quiz = await _db.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
            return NotFound(new { message = "Quiz not found." });

        var question = quiz.Questions.FirstOrDefault(q => q.Position == index);

        if (question is null)
            return NotFound(new { message = "Question not found." });

        var options = question.Options;
        if (request.SelectedOptionIndex < 0 || request.SelectedOptionIndex >= options.Count)
        {
            return BadRequest(new { message = "Invalid option index." });
        }

        var isCorrect = options[request.SelectedOptionIndex].IsCorrect;
        var correctOptionText = options.Single(o => o.IsCorrect).OptionText;

        var score = await _db.QuizScores
            .FirstOrDefaultAsync(s => s.UserId == userId && s.QuizId == quizId);

        if (score is null)
        {
            score = new QuizScore(userId, quizId);
            _db.QuizScores.Add(score);
        }

        try
        {
            score.RecordAnswer(index, isCorrect);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        await _db.SaveChangesAsync();

        return Ok(new AnswerResultResponse(isCorrect, correctOptionText));
    }

    [Authorize]
    [HttpGet("{quizId}/score")]
    public async Task<ActionResult<QuizScoreResponse>> GetScore(int quizId)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing token." });
        }

        var score = await _db.QuizScores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.QuizId == quizId);

        if (score is null)
        {
            return NotFound(new { message = "Quiz score not found." });
        }

        var correctAnswers = score.Answers.Count(a => a.Value);
        var totalAnswered = score.Answers.Count;

        return Ok(new QuizScoreResponse(correctAnswers, totalAnswered));
    }

    [Authorize]
    [HttpPost("import")]
    [RequestSizeLimit(1_000_000)]
    public async Task<ActionResult<QuizSummaryResponse>> ImportQuiz(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _importer.ImportAsync(stream);

        var quiz = result.Quiz;

        if (quiz is null)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var existing = await _db.Quizzes.AsNoTracking().ToListAsync();

        if (existing.Contains(quiz))
        {
            return Conflict(new { message = "A quiz with the same title, category and language already exists." });
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new QuizSummaryResponse(
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Category,
            quiz.Language,
            quiz.Questions.Count)
        );
    }
}