using PSI1.Api.Models;

namespace PSI1.Api.Extensions;

public static class QuizAccessExtensions
{
    public static IQueryable<Quiz> AccessibleBy(
        this IQueryable<Quiz> quizzes,
        int userId)
    {
        return quizzes.Where(q =>
            q.IsPublic || q.OwnerId == userId);
    }

}