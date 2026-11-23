using PSI1.Api.DTOs;
using PSI1.Api.Models;

namespace PSI1.Api.Services;

// Calculates the user's accuracy for each category and finds the weakest and strongest one.
public class ProgressAnalyzer
{
    public const int MinAnswered = 10;

    public ProgressResponse Analyze(List<CategoryAnswerCounts> counts)
    {
        var categories = Enum.GetValues<Category>()
            .Select(category =>
            {
                var categoryCounts = counts.FirstOrDefault(c => c.Category == category);
                var answered = categoryCounts?.Answered ?? 0;
                var correct = categoryCounts?.Correct ?? 0;

                return new CategoryStatsResponse(
                    category,
                    answered,
                    correct,
                    answered == 0 ? null : (double)correct / answered,
                    answered >= MinAnswered);

            })
            .ToList();

        var withEnoughData = categories.Where(c => c.HasEnoughData).ToList();
        if (withEnoughData.Count < 2)
        {
            return new ProgressResponse(categories, null, null);
        }

        var weakest = withEnoughData.MinBy(c => c.Accuracy)!;
        var strongest = withEnoughData.MaxBy(c => c.Accuracy)!;

        // with the same accuracy everywhere, no category is weaker or stronger than another
        if (weakest.Accuracy == strongest.Accuracy)
        {
            return new ProgressResponse(categories, null, null);
        }

        return new ProgressResponse(categories, strongest.Category, weakest.Category);
    }
}