using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data;

namespace PSI1.Api.Controllers;

// Uploads and returns the user's profile picture
// image saved on disk, filename is stored in the database.
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly string _picturesDirectory;

    public ProfileController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _picturesDirectory = Path.Combine(environment.ContentRootPath, "Uploads");
    }

    [HttpGet("picture")]
    public async Task<IActionResult> GetPicture()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid or missing token." });
        }

        var path = Path.Combine(_picturesDirectory, user.ProfilePictureFilename);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { message = "No profile picture uploaded." });
        }

        // the picture is loaded from disk through a stream
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var contentType = path.EndsWith(".png") ? "image/png" : "image/jpeg";

        return File(stream, contentType);
    }

    [HttpPost("picture")]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid or missing token." });
        }

        if (file.ContentType != "image/jpeg" && file.ContentType != "image/png")
        {
            return BadRequest(new { message = "Only JPEG and PNG images are allowed." });
        }

        Directory.CreateDirectory(_picturesDirectory);
        var extension = file.ContentType == "image/png" ? ".png" : ".jpg";
        var filename = $"{user.Id}{extension}";

        // the uploaded file is read as a stream and copied into a file on disk
        await using (var uploadStream = file.OpenReadStream())
        await using (var fileStream = new FileStream(Path.Combine(_picturesDirectory, filename), FileMode.Create))
        {
            await uploadStream.CopyToAsync(fileStream);
        }

        user.ProfilePictureFilename = filename;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile picture updated." });
    }

    private async Task<Models.User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }
}