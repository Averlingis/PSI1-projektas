namespace PSI1.Api.Models;

public class User
{
	public int Id { get; set; }
	public string Email { get; set; } = ""; // = "" to avoid null values
	public string PasswordHash { get; set; } = "";
	public string Username { get; set; } = "";
	public string ProfilePictureFilename { get; set; } = "default.jpg";
	public Language? LearningLanguage { get; set; } // default null 
	public Category? SelectedCategory { get; set;} // default null
}
