namespace PSI1.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set;} = ""; // = "" to avoid null values
    public string PasswordHash { get; set;} = "";
}