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
}