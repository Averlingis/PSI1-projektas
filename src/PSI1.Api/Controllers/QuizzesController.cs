using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Models;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizzesController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuizzesController(AppDbContext db)
    {
        _db = db;
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
}