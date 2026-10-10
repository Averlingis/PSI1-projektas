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
    public async Task<ActionResult<List<QuizSummaryResponse>>> GetAll()
    {
        var quizzes = await _db.Quizzes
            .AsNoTracking()
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
        if (index < 0)
            return NotFound();

        var quiz = await _db.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
            return NotFound();

        var question = quiz.Questions.FirstOrDefault(q => q.Position == index);

        if (question is null)
            return NotFound();

        var response = new QuestionPlayResponse(
            question.Id,
            question.Position,
            question.QuestionText,
            question.Options.Select(o => o.OptionText).ToList()
        );

        return Ok(response);
    }
}