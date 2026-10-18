using PSI1.Api.Extensions;

namespace PSI1.Api.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("  Hello World  ", "hello world")]
    [InlineData("ABC", "abc")]
    [InlineData("\tMixed Case\n", "mixed case")]
    [InlineData("qwwww", "qwwww")]
    [InlineData("   ", "")]
    [InlineData("", "")]
    public void ToLowerTrimmed_TrimsAndLowercases(string input, string expected)
    {
        Assert.Equal(expected, input.ToLowerTrimmed());
    }

    [Fact]
    public void ToLowerTrimmed_Null_Throws()
    {
        string? value = null;
        Assert.Throws<ArgumentNullException>(() => value!.ToLowerTrimmed());
    }
}