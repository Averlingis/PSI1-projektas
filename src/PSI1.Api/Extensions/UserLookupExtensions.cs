using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using PSI1.Api.Data;
using PSI1.Api.Exceptions;
using PSI1.Api.Models;

namespace PSI1.Api.Extensions;

public static class UserLookupExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidTokenException();
        }

        return userId;
    }

    public static async Task<User> GetCurrentUserAsync(this AppDbContext db, ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new UserNotFoundException();
        }

        return user;
    }
}