using PSI1.Api.DTOs;
using PSI1.Api.Models;

namespace PSI1.Api.QuizBuilding;

public class QuizBuilder()
{
    public QuizBuildResult Build(QuizDto dto)
    {
        var errors = new List<QuizError>();

        if (!TryParseEnum(dto.Category, out Category category))
        {
            errors.Add(new QuizError(null, $"Unknown or missing category: '{dto.Category}'."));
        }

        if (!TryParseEnum(dto.Language, out Language language))
        {
            errors.Add(new QuizError(null, $"Unknown or missing language: '{dto.Language}'."));
        }

        var questions = new List<Question>();
        var questionDtos = dto.Questions ?? [];

        for (var i = 0; i < questionDtos.Count; i++)
        {
            var questionDto = questionDtos[i];
            if (questionDto is null)
            {
                errors.Add(new QuizError(i + 1, "Question cannot be null."));
                continue;
            }

            try
            {
                var options = (questionDto.Options ?? [])
                    .Select(o => o ?? throw new ArgumentException("Options cannot contain null items."))
                    .Select(o => new AnswerOption(o.Text!, o.IsCorrect))
                    .ToList();

                questions.Add(new Question(questionDto.Text!, options));
            }
            catch (ArgumentException ex)
            {
                errors.Add(new QuizError(i + 1, ex.Message));
            }
        }

        if (errors.Count > 0)
        {
            return new QuizBuildResult(null, errors);
        }

        try
        {
            var quiz = new Quiz(title: dto.Title!, description: dto.Description, category: category, language: language, questions: questions);
            return new QuizBuildResult(quiz, []);
        }
        catch (ArgumentException ex)
        {
            return new QuizBuildResult(null, [new QuizError(null, ex.Message)]);
        }
    }

    private static bool TryParseEnum<T>(string? text, out T value) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value);
}