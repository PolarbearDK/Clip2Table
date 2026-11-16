using Clip2Table.Core.Parsing;

namespace Clip2Table.Core.Tests;

public class InlineMarkdownTests
{
    [Theory]
    [InlineData("`code`", "code")]
    [InlineData("**bold**", "bold")]
    [InlineData("__bold__", "bold")]
    [InlineData("*em*", "em")]
    [InlineData("_em_", "em")]
    [InlineData("~~x~~", "x")]
    [InlineData("[text](https://x.y/z)", "text")]
    [InlineData("**`x`**", "x")]
    [InlineData("`a*b*`", "a*b*")]
    [InlineData("see [Doc](http://a) and **b**", "see Doc and b")]
    public void Strips(string input, string expected) =>
        Assert.Equal(expected, InlineMarkdown.Strip(input));

    [Theory]
    [InlineData("a * b")]
    [InlineData("*")]
    [InlineData("snake_case_name")]
    [InlineData("file_name_here.cs")]
    [InlineData("2*3*4")]
    [InlineData("~~")]
    [InlineData("_")]
    [InlineData("a_b_")]
    [InlineData("price: 5 * 3")]
    public void Keeps(string input) =>
        Assert.Equal(input, InlineMarkdown.Strip(input));
}
