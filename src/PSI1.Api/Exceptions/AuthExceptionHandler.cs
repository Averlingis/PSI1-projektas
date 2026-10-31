using Microsoft.AspNetCore.Diagnostics;

namespace PSI1.Api.Exceptions;

public class AuthExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not (InvalidTokenException or UserNotFoundException))
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(new { message = exception.Message }, cancellationToken);

        return true;
    }
}