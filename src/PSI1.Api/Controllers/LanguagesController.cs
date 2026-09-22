using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSI1.Api.Models;
using PSI1.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using PSI1.Api.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
	// Requires a valid JWT (see AuthController.Login); the target user comes from
	// the token's "sub" claim rather than from the request body.
	[Authorize]
	[HttpPut("select")]
	public async Task<IActionResult> SelectLanguage(LanguageSelectionRequest request)
	{
		var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
			?? User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
		{
			return Unauthorized(new { message = "Invalid or missing token." });
		}

		var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

		if (user is null)
		{
			return NotFound(new { message = "User not found." });
		}

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
		var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
		?? User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
		{
			return Unauthorized(new { message = "Invalid or missing token." });
		}

		var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

		if (user is null)
		{
			return NotFound(new { message = "User not found." });
		}

			return Ok(new
		{
			language = user.LearningLanguage?.ToString()
		});
	}
}
