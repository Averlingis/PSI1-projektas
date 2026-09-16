// group class under Models namespace
namespace PSI1.Api.Models;

public class User
{

// ="" to avoid null values
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}


// ***pws are currently saved a plain text