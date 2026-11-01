using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Extensions;
using PSI1.Api.Models;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public LanguagesController(AppDbContext db)
    {
        _db = db;
    }

    // Returns all available languages defined in the Language enum.
    // Left open to anonymous users so the sign-up/selection UI can list options before login.
    [HttpGet]
    public IActionResult GetLanguages()
    {
        var languages = Enum.GetValues<Language>()
            .Select(language => language.ToString())
            .ToList();

        return Ok(languages);
    }

    // Updates and saves the selected learning language for the authenticated user.
    [Authorize]
    [HttpPut("select")]
    public async Task<IActionResult> SelectLanguage(LanguageSelectionRequest request)
    {
        var user = await _db.GetCurrentUserAsync(User);

        user.LearningLanguage = request.Language;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Learning language changed to: {request.Language}.",
            language = request.Language.ToString()
        });
    }

    // Returns the currently selected learning language for the authenticated user.
    // The user is identified from the JWT token, so another user's language
    // cannot be retrieved by providing a username or user ID.
    [Authorize]
    [HttpGet("selected")]
    public async Task<IActionResult> GetSelectedLanguage()
    {
        var user = await _db.GetCurrentUserAsync(User);

        return Ok(new
        {
            language = user.LearningLanguage?.ToString()
        });
    }
}