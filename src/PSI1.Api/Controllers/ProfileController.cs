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
    private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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

    // the request limit stops Kestrel from reading huge bodies at all
    [HttpPost("picture")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid or missing token." });
        }

        // size is checked on the server
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }
        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "The image must be 2 MB or smaller." });
        }

        // the type is decided from the file's first bytes, the client's ContentType can be faked
        var extension = await DetectImageExtensionAsync(file);
        if (extension is null)
        {
            return BadRequest(new { message = "Only JPEG and PNG images are allowed." });
        }

        Directory.CreateDirectory(_picturesDirectory);
        var filename = $"{user.Id}{extension}";

        // the uploaded file is read as a stream and copied into a file on disk
        await using (var uploadStream = file.OpenReadStream())
        await using (var fileStream = new FileStream(Path.Combine(_picturesDirectory, filename), FileMode.Create))
        {
            await uploadStream.CopyToAsync(fileStream);
        }

        // delete the previous picture if it had another extension
        foreach (var oldPath in Directory.GetFiles(_picturesDirectory, $"{user.Id}.*"))
        {
            if (!string.Equals(Path.GetFileName(oldPath), filename, StringComparison.OrdinalIgnoreCase))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        user.ProfilePictureFilename = filename;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile picture updated." });
    }

    // reads the first bytes of the file to find out if it is really a PNG or JPEG
    private static async Task<string?> DetectImageExtensionAsync(IFormFile file)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

        if (read >= 8 && header.AsSpan().SequenceEqual(PngSignature))
        {
            return ".png";
        }
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }
        return null;
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