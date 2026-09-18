// group class under Models namespace
namespace PSI1.Api.Models;

public class User
{
    public int Id { get; set; } = 0; 
    public string Username { get; set; } = ""; // ="" to avoid null values
    public string PasswordHash { get; set; } = ""; // ="" to avoid null values
    public string PictureFilename {get; set; } = "default.jpg";
}