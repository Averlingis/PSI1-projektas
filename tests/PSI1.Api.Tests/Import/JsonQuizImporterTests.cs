using System.Text;

using PSI1.Api.Import;
using PSI1.Api.Models;

namespace PSI1.Api.Tests.Import;

public class JsonQuizImporterTests
{

    private static MemoryStream ToStream(string json) => new(Encoding.UTF8.GetBytes(json));

    [Fact]
    public async Task ImportAsync_ValidFile_ReturnsQuiz()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "text": "How do you say 'Grandmother' in Lithuanian?",
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false }
                ]
              }
            ]
          }
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Quiz);
        Assert.Equal("Family Vocabulary", result.Quiz.Title);
        Assert.Equal(Category.Family, result.Quiz.Category);
        Assert.Equal(Language.Lithuanian, result.Quiz.Language);
        Assert.Equal(3, result.Quiz.Questions.Count);

        var first = result.Quiz.Questions[0];
        Assert.Equal("How do you say 'Mother' in Lithuanian?", first.QuestionText);
        Assert.Equal(new[] { "Sesuo", "Mama", "Močiutė" }, first.Options.Select(o => o.OptionText));
        Assert.Equal("Mama", first.Options.Single(o => o.IsCorrect).OptionText);
    }

    [Fact]
    public async Task ImportAsync_FileWithoutDescription_ReturnsQuiz()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "text": "How do you say 'Grandmother' in Lithuanian?",
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false }
                ]
              }
            ]
          }
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Quiz);
    }

    [Fact]
    public async Task ImportAsync_MalformedJson_ReturnsSingleErrorWithoutLocation()
    {
        var json = """
          {
            "title": "Family Vocabulary"
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
    }

    [Fact]
    public async Task ImportAsync_EmptyFile_ReturnsSingleErrorWithoutLocation()
    {
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(""));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
    }

    [Fact]
    public async Task ImportAsync_JsonNull_ReturnsSingleErrorWithoutLocation()
    {
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream("null"));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
    }

    [Theory]
    [InlineData("Movies")]
    [InlineData("999")]
    [InlineData("")]
    public async Task ImportAsync_UnknownCategory_ReturnsSingleErrorWithoutLocation(string category)
    {
        var json = $$"""
          {
            "title": "Family Vocabulary",
            "category": "{{category}}",
            "language": "Lithuanian"
          }
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
        Assert.Contains("category", error.Message);
    }

    [Theory]
    [InlineData("Polish")]
    [InlineData("999")]
    [InlineData("")]
    public async Task ImportAsync_UnknownLanguage_ReturnsSingleErrorWithoutLocation(string language)
    {
        var json = $$"""
          {
            "title": "Family Vocabulary",
            "category": "Family",
            "language": "{{language}}"
          }
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
        Assert.Contains("language", error.Message);
    }

    [Fact]
    public async Task ImportAsync_InvalidQuestion_ReturnsErrorWithQuestionNumber()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "text": "",
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false }
                ]
              }
            ]
          }
          """;

        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.Location);
    }

    [Fact]
    public async Task ImportAsync_MultipleInvalidQuestions_ReturnsAllErrors()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": false },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "text": "",
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false }
                ]
              }
            ]
          }
          """;

        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        Assert.Equal(new int?[] { 1, 3 }, result.Errors.Select(e => e.Location));
    }

    [Fact]
    public async Task ImportAsync_TooFewQuestions_ReturnsSingleErrorWithoutLocation()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              }
            ]
          }
          """;

        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
        Assert.Contains("questions", error.Message);
    }

    [Fact]
    public async Task ImportAsync_QuestionWithoutText_ReturnsErrorWithQuestionNumber()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false }
                ]
              }
            ]
          }
          """;

        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.Location);
    }

    [Fact]
    public async Task ImportAsync_MissingTitle_ReturnsSingleErrorWithoutLocation()
    {
        var json = """
          {
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "text": "How do you say 'Grandmother' in Lithuanian?",
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false }
                ]
              }
            ]
          }
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Null(error.Location);
        Assert.Contains("title", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportAsync_NullQuestion_ReturnsErrorWithQuestionNumber()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              null
            ]
          }
          """;

        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.Location);
    }

    [Fact]
    public async Task ImportAsync_NullOption_ReturnsErrorWithQuestionNumber()
    {
        var json = """
          {
            "title": "Family Vocabulary",
            "description": "Basic family words in Lithuanian",
            "category": "Family",
            "language": "Lithuanian",
            "questions": [
              {
                "text": "How do you say 'Mother' in Lithuanian?",
                "options": [
                  { "text": "Sesuo", "isCorrect": false },
                  { "text": "Mama", "isCorrect": true },
                  { "text": "Močiutė", "isCorrect": false }
                ]
              },
              {
                "text": "What does 'Brolis' mean in English?",
                "options": [
                  { "text": "Father", "isCorrect": false },
                  { "text": "Brother", "isCorrect": true }
                ]
              },
              {
                "text": "How do you say 'Grandmother' in Lithuanian?",
                "options": [
                  { "text": "Močiutė", "isCorrect": true },
                  { "text": "Teta", "isCorrect": false },
                  null
                ]
              }
            ]
          }
          """;
        var importer = new JsonQuizImporter();

        var result = await importer.ImportAsync(ToStream(json));

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.Location);
    }
}