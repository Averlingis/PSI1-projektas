using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Extensions;
using PSI1.Api.Import;
using PSI1.Api.Models;
using PSI1.Api.QuizBuilding;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizzesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JsonQuizImporter _importer;
    private readonly QuizBuilder _quizBuilder;

    public QuizzesController(
     AppDbContext db,
     JsonQuizImporter importer,
     QuizBuilder quizBuilder)
    {
        _db = db;
        _importer = importer;
        _quizBuilder = quizBuilder;
    }

    [HttpGet]
    public async Task<ActionResult<List<QuizSummaryResponse>>> GetAll(
    [FromQuery] Category? category,
    [FromQuery] Language? language)
    {
        return await GetSummariesAsync(_db.Quizzes.AsNoTracking().Where(q => q.IsPublic), category, language);
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<List<QuizSummaryResponse>>> GetMyQuizzes(
    [FromQuery] Category? category,
    [FromQuery] Language? language)
    {
        var userId = User.GetUserId();
        return await GetSummariesAsync(_db.Quizzes.AsNoTracking().Where(q => q.OwnerId == userId), category, language);
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
        var userId = User.GetUserId();

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
        var userId = User.GetUserId();

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
    [HttpGet("{quizId}/progress")]
    public async Task<ActionResult<QuizProgressResponse>> GetProgress(int quizId)
    {
        var userId = User.GetUserId();

        var quiz = await _db.Quizzes
            .AsNoTracking()
            .Where(q => q.Id == quizId)
            .Select(q => new { QuestionCount = EF.Property<ICollection<Question>>(q, "questions").Count })
            .FirstOrDefaultAsync();

        if (quiz is null)
        {
            return NotFound(new { message = "Quiz not found." });
        }

        var score = await _db.QuizScores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.QuizId == quizId);
        //new score if null
        score ??= new QuizScore(userId, quizId);

        return Ok(new QuizProgressResponse(
            score.GetFirstUnansweredIndex(quiz.QuestionCount),
            score.Answers.Count,
            quiz.QuestionCount));
    }

    [Authorize]
    [HttpGet("in-progress/latest")]
    public async Task<ActionResult<QuizInProgressResponse>> GetLatestInProgress()
    {
        var userId = User.GetUserId();

        var scores = await _db.QuizScores
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var quizIds = scores.Select(s => s.QuizId).ToList();

        var quizzes = await _db.Quizzes
            .AsNoTracking()
            .Where(q => quizIds.Contains(q.Id))
            .Select(q => new
            {
                q.Id,
                q.Title,
                QuestionCount = EF.Property<ICollection<Question>>(q, "questions").Count
            })
            .ToDictionaryAsync(q => q.Id);

        var latest = scores
            .Where(s => s.GetLatestAnswerId() is not null
                && s.GetFirstUnansweredIndex(quizzes[s.QuizId].QuestionCount) is not null)
            .MaxBy(s => s.GetLatestAnswerId());

        if (latest is null)
        {
            return NoContent();
        }

        return Ok(new QuizInProgressResponse(latest.QuizId, quizzes[latest.QuizId].Title));
    }

    [Authorize]
    [HttpDelete("{quizId}/score")]
    public async Task<IActionResult> ResetScore(int quizId)
    {
        var userId = User.GetUserId();

        // if two resets arrive at the same time, the second one simply deletes nothing and allows the quiz to be played the second time
        await _db.QuizScores
            .Where(s => s.UserId == userId && s.QuizId == quizId)
            .ExecuteDeleteAsync();

        return NoContent();
    }
    [Authorize]
    [HttpPost]
    [RequestSizeLimit(1_000_000)]
    public async Task<ActionResult<QuizSummaryResponse>> CreateQuiz(
        [FromBody] CreateQuizRequest request)
    {
        if (request?.Quiz is null)
        {
            return BadRequest(new
            {
                errors = new[] { new QuizError(null, "Quiz is required.") }
            });
        }

        var userId = User.GetUserId();

        var result = _quizBuilder.Build(
            request.Quiz,
            ownerId: userId,
            isPublic: request.IsPublic);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var quiz = result.Quiz!;

        var existing = await _db.Quizzes.AsNoTracking().ToListAsync();

        if (existing.Contains(quiz))
        {
            return Conflict(new
            {
                message = "A quiz with the same title, category and language already exists."
            });
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();

        return StatusCode(
            StatusCodes.Status201Created,
            new QuizSummaryResponse(
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.Category,
                quiz.Language,
                quiz.Questions.Count,
                quiz.IsPublic));
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
            quiz.Questions.Count,
            quiz.IsPublic)
        );
    }

    private async Task<ActionResult<List<QuizSummaryResponse>>> GetSummariesAsync(IQueryable<Quiz> query, Category? category, Language? language)
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
             EF.Property<ICollection<Question>>(q, "questions").Count,
             q.IsPublic))
         .ToListAsync();

        return Ok(quizzes);
    }
}