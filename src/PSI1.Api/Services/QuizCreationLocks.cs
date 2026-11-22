using System.Collections.Concurrent;

using PSI1.Api.Models;

namespace PSI1.Api.Services;

// Keeps one lock per category and language, so the duplicate check and the save
// in CreateQuiz run one at a time only for quizzes that could be duplicate.
public class QuizCreationLocks
{
    private readonly ConcurrentDictionary<(Category, Language), SemaphoreSlim> _locks = new();

    public SemaphoreSlim Get(Category category, Language language) =>
        _locks.GetOrAdd((category, language), _ => new SemaphoreSlim(1, 1));
}