namespace PSI1.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = ""; // = "" to avoid null values
    public string PasswordHash { get; set; } = "";
    public string Username { get; set; } = "";
    public string ProfilePictureFilename { get; set; } = "default.jpg";
    public Language? LearningLanguage { get; set; } // default null 
    public Category? SelectedCategory { get; set; } // default null
    public int CurrentStreak { get; private set; } = 0;
    public DateOnly? LastActivityDate { get; private set; }

    public void RecordActivity(DateOnly today)
    {
        if (LastActivityDate == today)
        {
            return;
        }
        if (LastActivityDate == today.AddDays(-1))
        {
            CurrentStreak += 1;
        }
        else
        {
            CurrentStreak = 1;
        }
        LastActivityDate = today;
    }
    // A missed day is detected here, when the streak is read, so the stored value is not reset in the database.
    public int GetCurrentStreak(DateOnly today)
    {
        if (LastActivityDate >= today.AddDays(-1))
        {
            return CurrentStreak;
        }
        return 0;
    }
}