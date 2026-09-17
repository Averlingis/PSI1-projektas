using Microsoft.AspNetCore.Mvc;
using PSI1.Api.Models;
using Microsoft.EntityFrameworkCore;
using PSI1.Api.Data;

namespace PSI1.Api.Controllers;

// DTO for receiving the username and selected language from the client
public record LanguageSelectionRequest(string Username, Language Language);

[ApiController]
[Route("api/languages")]
public class LanguagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public LanguagesController(AppDbContext db)
    {
        _db = db;
    }

// Returns all available languages defined in the Language enum
    [HttpGet]
    public IActionResult GetLanguages()
    {
        var languages = Enum.GetValues<Language>()
            .Select(language => language.ToString())
            .ToList();

        return Ok(languages);
    }


// Updates and saves the selected learning language for a user
    [HttpPut("select")]
    public async Task<IActionResult> SelectLanguage(LanguageSelectionRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null)
        {
            return NotFound("User not found.");
        }

        user.LearningLanguage = request.Language;

        await _db.SaveChangesAsync();

        return Ok($"Learning language changed to: {request.Language}.");
    }
}