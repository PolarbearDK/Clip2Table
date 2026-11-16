using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class ReadabilityCheckTests
{
    [Fact]
    public void Builtin_has_no_warnings() => Assert.Empty(ReadabilityCheck.Find(BuiltInLayout.Create()));

    [Fact]
    public void Dark_header_warns_for_outlook_and_header_text()
    {
        var warnings = ReadabilityCheck.Find(BuiltInLayout.Create() with { HeaderBackground = "#1f3a5f" });
        Assert.Equal(
            [
                "Header text (#1f4e79) is hard to read on the header background (#1f3a5f).",
                "In new Outlook, text in the header turns black and will be hard to read on #1f3a5f.",
            ],
            warnings);
    }

    [Fact]
    public void Dark_stripes_warn_for_text_null_and_outlook()
    {
        var warnings = ReadabilityCheck.Find(BuiltInLayout.Create() with { StripeBackground = "#6f6f6f" });
        Assert.Equal(
            [
                "Table text (#222222) is hard to read on the stripe background (#6f6f6f).",
                "NULL text (#999999) is hard to read on the stripe background (#6f6f6f).",
                "In new Outlook, text in striped rows turns black and will be hard to read on #6f6f6f.",
            ],
            warnings);
    }

    [Fact]
    public void Stripe_checks_skipped_when_stripes_off() =>
        Assert.Empty(ReadabilityCheck.Find(BuiltInLayout.Create() with { Stripes = false, StripeBackground = "#000000" }));

    [Fact]
    public void Pale_text_and_borders_warn()
    {
        var warnings = ReadabilityCheck.Find(BuiltInLayout.Create() with
        {
            TextColor = "#cccccc",
            CellBorderColor = "#f8f8f8",
            HeaderBorderColor = "#fafafa",
            NullTextColor = "#eeeeee",
        });
        Assert.Contains("Table text (#cccccc) is hard to read on white.", warnings);
        Assert.Contains("Cell border (#f8f8f8) is nearly invisible on white.", warnings);
        Assert.Contains("Header border (#fafafa) is nearly invisible on white.", warnings);
        Assert.Contains("NULL text (#eeeeee) is hard to read on white.", warnings);
    }
}
