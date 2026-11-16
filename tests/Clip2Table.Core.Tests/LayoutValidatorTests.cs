using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class LayoutValidatorTests
{
    private static readonly LayoutDefinition Valid = BuiltInLayout.Create();

    [Fact]
    public void Builtin_is_valid() => Assert.Null(LayoutValidator.FindInvalidField(Valid));

    [Fact]
    public void Unknown_font() => Assert.Equal("fontFamily", LayoutValidator.FindInvalidField(Valid with { FontFamily = "Comic Sans MS" }));

    [Theory]
    [InlineData(8)]
    [InlineData(17)]
    public void Font_size_out_of_range(int size) => Assert.Equal("fontSize", LayoutValidator.FindInvalidField(Valid with { FontSize = size }));

    [Fact]
    public void Shorthand_colour_rejected() =>
        Assert.Equal("headerBackground", LayoutValidator.FindInvalidField(Valid with { HeaderBackground = "#abc" }));

    [Fact]
    public void Hidden_stripe_colour_still_validated() =>
        Assert.Equal("stripeBackground", LayoutValidator.FindInvalidField(Valid with { Stripes = false, StripeBackground = "nope" }));

    [Fact]
    public void Padding_out_of_range() => Assert.Equal("padding", LayoutValidator.FindInvalidField(Valid with { Padding = new(21, 7) }));

    [Fact]
    public void Blank_name() => Assert.Equal("name", LayoutValidator.FindInvalidField(Valid with { Name = " " }));
}

public class LayoutNamesTests
{
    [Fact]
    public void Blank_name_rejected() => Assert.Equal("Enter a name.", LayoutNames.Validate("  ", []));

    [Fact]
    public void Clash_is_case_insensitive_and_trimmed() =>
        Assert.Equal("A layout named 'sales' already exists.", LayoutNames.Validate(" sales ", ["Sales"]));

    [Fact]
    public void Free_name_accepted() => Assert.Null(LayoutNames.Validate("Sales 2", ["Sales"]));

    [Fact]
    public void Unique_keeps_free_name_trimmed() => Assert.Equal("Sales", LayoutNames.MakeUnique(" Sales ", ["Other"]));

    [Fact]
    public void Unique_appends_counter() => Assert.Equal("Sales (2)", LayoutNames.MakeUnique("Sales", ["sales"]));

    [Fact]
    public void Unique_skips_taken_counters() => Assert.Equal("Sales (3)", LayoutNames.MakeUnique("Sales", ["Sales", "Sales (2)"]));
}
