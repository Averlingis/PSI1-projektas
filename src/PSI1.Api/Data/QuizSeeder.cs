using Microsoft.EntityFrameworkCore;

using PSI1.Api.Import;
using PSI1.Api.Models;

namespace PSI1.Api.Data;

public class QuizSeeder
{
    private readonly AppDbContext _db;
    private readonly JsonQuizImporter _importer;

    public QuizSeeder(AppDbContext db, JsonQuizImporter importer)
    {
        _db = db;
        _importer = importer;
    }

    public async Task SeedAsync(string directory)
    {
        if (await _db.Quizzes.AnyAsync())
        {
            return;
        }

        var seedFiles = Directory.GetFiles(directory, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal);

        var quizzes = new List<Quiz>();
        var errors = new List<string>();

        foreach (var path in seedFiles)
        {
            await using var stream = File.OpenRead(path);
            var result = await _importer.ImportAsync(stream);

            if (result.IsSuccess)
            {
                quizzes.Add(result.Quiz!);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    errors.Add($"{Path.GetFileName(path)}: {error.Message}");
                }
            }
        }

        if (errors.Count() > 0)
        {
            throw new InvalidOperationException(
                "Seed data is invalid:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        _db.Quizzes.AddRange(quizzes);
        await _db.SaveChangesAsync();
    }
}