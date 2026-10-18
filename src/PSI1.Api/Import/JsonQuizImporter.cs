using System.Text.Json;

using PSI1.Api.Models;

namespace PSI1.Api.Import;

public class JsonQuizImporter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    public async Task<QuizImportResult> ImportAsync(Stream stream)
    {
        QuizFileDto? dto;

        try
        {
            dto = await JsonSerializer.DeserializeAsync<QuizFileDto>(stream, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return new QuizImportResult(null, [new ImportError(null, "The file is not valid JSON.")]);
        }

        if (dto is null)
        {
            return new QuizImportResult(null, [new ImportError(null, "The file does not contain a quiz.")]);
        }

        var errors = new List<ImportError>();

        if (!TryParseEnum(dto.Category, out Category category))
        {
            errors.Add(new ImportError(null, $"Unknown or missing category: '{dto.Category}'."));
        }

        if (!TryParseEnum(dto.Language, out Language language))
        {
            errors.Add(new ImportError(null, $"Unknown or missing language: '{dto.Language}'."));
        }

        var questions = new List<Question>();
        var questionDtos = dto.Questions ?? [];

        for (var i = 0; i < questionDtos.Count; i++)
        {
            var questionDto = questionDtos[i];

            try
            {
                var options = (questionDto.Options ?? [])
                    .Select(o => new AnswerOption(o.Text!, o.IsCorrect))
                    .ToList();

                questions.Add(new Question(questionDto.Text!, options));
            }
            catch (ArgumentException ex)
            {
                errors.Add(new ImportError(i + 1, ex.Message));
            }
        }

        if (errors.Count > 0)
        {
            return new QuizImportResult(null, errors);
        }
        try
        {
            var quiz = new Quiz(title: dto.Title!, description: dto.Description, category: category, language: language, questions: questions);
            return new QuizImportResult(quiz, []);
        }
        catch (ArgumentException ex)
        {
            return new QuizImportResult(null, [new ImportError(null, ex.Message)]);
        }
    }

    private static bool TryParseEnum<T>(string? text, out T value) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value);

}