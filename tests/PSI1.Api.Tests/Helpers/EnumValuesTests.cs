using PSI1.Api.Helpers;
using PSI1.Api.Models;

namespace PSI1.Api.Tests.Helpers;

public class EnumValuesTests
{
    [Fact]
    public void Names_Language_ReturnsAllNames()
    {
        Assert.Equal(["Lithuanian", "Russian", "Italian"], EnumValues<Language>.Names());
    }

    [Fact]
    public void IsDefined_DefinedValue_ReturnsTrue()
    {
        Assert.True(EnumValues<Language>.IsDefined(Language.Italian));
    }

    [Fact]
    public void IsDefined_UndefinedValue_ReturnsFalse()
    {
        Assert.False(EnumValues<Category>.IsDefined((Category)99));
    }

    [Theory]
    [InlineData("food", Category.Food)]
    [InlineData("FAMILY", Category.Family)]
    [InlineData("1", Category.Culture)]
    public void TryParse_ValidText_ReturnsValue(string text, Category expected)
    {
        Assert.True(EnumValues<Category>.TryParse(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("99")]
    [InlineData(null)]
    public void TryParse_UnknownText_ReturnsFalse(string? text)
    {
        Assert.False(EnumValues<Language>.TryParse(text, out _));
    }
}