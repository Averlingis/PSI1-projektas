// group class under Models namespace
namespace PSI1.Api.Models;

public class User
{
    public int Id { get; set; } = 0;
// ="" to avoid null values
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PictureFilename {get; set; } = "default.jpg";

    public Language? LearningLanguage { get; set; }
}

// ***pws are currently saved a plain text
