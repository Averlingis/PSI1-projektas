using System.Text.Json; // for conversion from c sharp to json
using Microsoft.AspNetCore.Mvc; // for features needed to build a web api controller
using PSI1.Api.Models; // for User class

namespace PSI1.Api.Controllers;

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);


// Tells asp.net that this class handles API
[ApiController]

// URL for end point
[Route("api/[controller]")]

// Inhertis from ControllerBase core class
public class AuthController : ControllerBase
{
// where user data is saved on disk, value is shared across all requests and never changes for now
    private static readonly string filePath = "users.json";

// reads users
    private List<User> LoadUsers()
    {
        if (!System.IO.File.Exists(filePath))
        {
            return new List<User>();
        }

// converts from json to dynamic list
        using var stream = System.IO.File.OpenRead(filePath);
        var users = JsonSerializer.Deserialize<List<User>>(stream);
        return users ?? new List<User>();
    }

// writes the given list back  to json
    private void SaveUsers(List<User> users)
    {
// creates or overwrites the file and opens it
        using var stream = System.IO.File.Create(filePath);
// converts to json
        JsonSerializer.Serialize(stream, users);
    }

// Handles POST req
    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
// load the users
        var users = LoadUsers();
// adds user
        users.Add(new User { Username = request.Username, Password = request.Password });
        SaveUsers(users);


        return Ok("Account created.");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var users = LoadUsers();

// for loop to check if un and pw match saved ones
        foreach (var user in users)
        {
            if (user.Username == request.Username && user.Password == request.Password)
            {
                return Ok("Logged in.");
            }
        }

        return Unauthorized("Wrong username or password.");
    }
}