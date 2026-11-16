using System.Text.Json.Nodes;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class LayoutExchangeTests
{
    private static readonly LayoutDefinition Builtin = BuiltInLayout.Create();
    private static readonly LayoutDefinition Sales = Builtin with { Id = "s", Name = "Sales", HeaderBackground = "#e2efda" };

    private static string Wrap(string layouts, string format = "clip2table-layouts", int version = 1) =>
        $$"""{ "format": "{{format}}", "version": {{version}}, "layouts": [{{layouts}}] }""";

    [Fact]
    public void Export_round_trips_without_ids()
    {
        var json = LayoutExchange.Export([Sales]);

        Assert.Contains("\"format\": \"clip2table-layouts\"", json);
        Assert.Contains("\"version\": 1", json);
        Assert.DoesNotContain("\"id\"", json);
        var parsed = LayoutExchange.Parse(json);
        Assert.Null(parsed.Error);
        var layout = Assert.Single(parsed.Layouts!);
        Assert.True(layout.HasSameStyle(Sales));
        Assert.Equal("Sales", layout.Name);
        Assert.NotEqual("s", layout.Id);
        Assert.False(string.IsNullOrEmpty(layout.Id));
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Not_a_layouts_file(string json) =>
        Assert.Equal("This is not a Clip2Table layouts file.", LayoutExchange.Parse(json).Error);

    [Fact]
    public void Duplicate_root_key_rejected() =>
        Assert.Equal("This is not a Clip2Table layouts file.",
            LayoutExchange.Parse("""{ "format": "clip2table-layouts", "format": "x", "version": 1, "layouts": [] }""").Error);

    [Fact]
    public void Duplicate_key_in_layout_rejected_with_message()
    {
        var layout = LayoutJson.ToJson(Sales, includeId: false).ToJsonString().Replace("\"name\":\"Sales\",", "\"name\":\"Sales\",\"name\":\"Twice\",");
        Assert.Equal("Layout 1 ('?') has an invalid layout.", LayoutExchange.Parse(Wrap(layout)).Error);
    }

    [Fact]
    public void Wrong_format_rejected() =>
        Assert.Equal("This is not a Clip2Table layouts file.", LayoutExchange.Parse(Wrap("", format: "other")).Error);

    [Fact]
    public void Newer_version_rejected() =>
        Assert.Equal("This file was made by a newer version of Clip2Table.", LayoutExchange.Parse(Wrap("", version: 2)).Error);

    [Fact]
    public void Empty_file_rejected() =>
        Assert.Equal("The file contains no layouts.", LayoutExchange.Parse(Wrap("")).Error);

    [Fact]
    public void Invalid_layout_named_with_its_position_and_field()
    {
        var bad = LayoutJson.ToJson(Sales with { Name = "Bad" }, includeId: false);
        bad["padding"] = new JsonArray(1, 2, 3);
        var json = Wrap(LayoutJson.ToJson(Sales, includeId: false).ToJsonString() + "," + bad.ToJsonString());

        var result = LayoutExchange.Parse(json);

        Assert.Equal("Layout 2 ('Bad') has an invalid padding.", result.Error);
        Assert.Null(result.Layouts);
    }

    [Fact]
    public void Unreadable_name_shown_as_question_mark() =>
        Assert.Equal("Layout 1 ('?') has an invalid name.", LayoutExchange.Parse(Wrap("{}")).Error);

    [Fact]
    public void Import_skips_layout_identical_to_builtin()
    {
        var merge = LayoutExchange.Merge([Builtin], [Builtin with { Id = "n", Name = "Copy" }]);
        Assert.Empty(merge.Added);
        Assert.Equal(1, merge.Skipped);
    }

    [Fact]
    public void Import_adds_name_clash_with_suffix()
    {
        var merge = LayoutExchange.Merge([Builtin, Sales], [Sales with { Id = "n", Padding = new(8, 14) }]);
        var added = Assert.Single(merge.Added);
        Assert.Equal("Sales (2)", added.Name);
        Assert.Equal("n", added.Id);
    }

    [Fact]
    public void Import_skips_in_file_duplicates()
    {
        var merge = LayoutExchange.Merge([Builtin], [Sales, Sales with { Id = "n2", Name = "Again" }]);
        Assert.Equal("Sales", Assert.Single(merge.Added).Name);
        Assert.Equal(1, merge.Skipped);
    }

    [Fact]
    public void Import_suffixes_in_file_name_clashes()
    {
        var merge = LayoutExchange.Merge([Builtin], [Sales, Sales with { Id = "n2", Padding = new(1, 3) }]);
        Assert.Equal(["Sales", "Sales (2)"], merge.Added.Select(l => l.Name));
    }

    [Fact]
    public void Summary_text()
    {
        Assert.Equal("Imported 3 layouts. 1 was already present.", LayoutExchange.Summary(new([Sales, Sales, Sales], 1)));
        Assert.Equal("Imported 1 layout.", LayoutExchange.Summary(new([Sales], 0)));
        Assert.Equal("Imported 0 layouts. 2 were already present.", LayoutExchange.Summary(new([], 2)));
    }
}
