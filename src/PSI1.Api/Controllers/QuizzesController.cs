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
}