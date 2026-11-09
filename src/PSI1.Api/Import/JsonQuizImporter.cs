using System.Text.Json;

using PSI1.Api.DTOs;
using PSI1.Api.QuizBuilding;

namespace PSI1.Api.Import;

public class JsonQuizImporter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly QuizBuilder _builder;

    public JsonQuizImporter(QuizBuilder builder)
    {
        _builder = builder;
    }

    public async Task<QuizBuildResult> ImportAsync(Stream stream)
    {
        QuizDto? dto;

        try
        {
            dto = await JsonSerializer.DeserializeAsync<QuizDto>(stream, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return new QuizBuildResult(null, [new QuizError(null, "The file is not valid JSON.")]);
        }

        if (dto is null)
        {
            return new QuizBuildResult(null, [new QuizError(null, "The file does not contain a quiz.")]);
        }

        return _builder.Build(dto);
    }
}