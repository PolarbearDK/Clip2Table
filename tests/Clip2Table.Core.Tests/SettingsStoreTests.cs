using Clip2Table.Core.Settings;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "clip2table-tests-" + Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    [Fact]
    public void Load_missing_returns_defaults()
    {
        var s = new SettingsStore(SettingsPath).Load();
        Assert.False(s.AutoConvert);
        Assert.Equal(BuiltInLayout.Id, s.Layout);
        Assert.Empty(s.Customs);
        Assert.Null(s.BuiltInName);
    }

    [Fact]
    public void Round_trips_custom_layouts_and_builtin_name()
    {
        var store = new SettingsStore(SettingsPath);
        var custom = BuiltInLayout.Create() with { Id = "c1", Name = "Sales", Padding = new(5, 10), Stripes = false };
        store.Save(new AppSettings(true, "c1", [custom], "House"));

        var loaded = store.Load();

        Assert.Equal((true, "c1", "House"), (loaded.AutoConvert, loaded.Layout, loaded.BuiltInName));
        Assert.Equal(custom, Assert.Single(loaded.Customs));
        var text = File.ReadAllText(SettingsPath);
        Assert.Contains("\"autoConvert\": true", text);
        Assert.Matches(@"""padding"": \[\s*5,\s*10\s*\]", text);
    }

    [Fact]
    public void Builtin_name_omitted_when_not_set()
    {
        new SettingsStore(SettingsPath).Save(new AppSettings());
        Assert.DoesNotContain("builtInName", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Load_skips_invalid_layout_keeps_rest()
    {
        var store = new SettingsStore(SettingsPath);
        var good = BuiltInLayout.Create() with { Id = "ok", Name = "Good" };
        var bad = BuiltInLayout.Create() with { Id = "bad", Name = "Bad", FontSize = 15 };
        store.Save(new AppSettings(true, "ok", [good, bad]));
        File.WriteAllText(SettingsPath, File.ReadAllText(SettingsPath).Replace("\"fontSize\": 15", "\"fontSize\": 99"));

        var loaded = store.Load();

        Assert.True(loaded.AutoConvert);
        Assert.Equal("ok", Assert.Single(loaded.Customs).Id);
    }

    [Fact]
    public void Load_skips_layout_without_id()
    {
        Directory.CreateDirectory(_dir);
        var store = new SettingsStore(SettingsPath);
        store.Save(new AppSettings(CustomLayouts: [BuiltInLayout.Create() with { Id = "x", Name = "X" }]));
        File.WriteAllText(SettingsPath, File.ReadAllText(SettingsPath).Replace("\"id\": \"x\",", ""));

        Assert.Empty(store.Load().Customs);
    }

    [Fact]
    public void Corrupt_file_returns_defaults_and_keeps_backup()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, "{not json");

        var loaded = new SettingsStore(SettingsPath).Load();

        Assert.False(loaded.AutoConvert);
        Assert.Empty(loaded.Customs);
        Assert.Equal("{not json", File.ReadAllText(SettingsPath + ".bak"));
    }

    [Fact]
    public void Duplicate_root_key_treated_as_unreadable()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "autoConvert": true, "autoConvert": false }""");

        var loaded = new SettingsStore(SettingsPath).Load();

        Assert.False(loaded.AutoConvert);
        Assert.True(File.Exists(SettingsPath + ".bak"));
    }

    [Fact]
    public void Duplicate_key_in_one_layout_skips_only_that_layout()
    {
        var store = new SettingsStore(SettingsPath);
        store.Save(new AppSettings(true, "ok", [BuiltInLayout.Create() with { Id = "ok", Name = "Good" }, BuiltInLayout.Create() with { Id = "dup", Name = "Dup" }]));
        File.WriteAllText(SettingsPath, File.ReadAllText(SettingsPath).Replace("\"name\": \"Dup\",", "\"name\": \"Dup\", \"name\": \"Again\","));

        var loaded = store.Load();

        Assert.True(loaded.AutoConvert);
        Assert.Equal("ok", Assert.Single(loaded.Customs).Id);
    }

    [Fact]
    public void Unreadable_file_is_not_overwritten_by_next_save()
    {
        var store = new SettingsStore(SettingsPath);
        store.Save(new AppSettings(true, "c", [BuiltInLayout.Create() with { Id = "c", Name = "Keep" }]));
        var original = File.ReadAllText(SettingsPath);

        AppSettings loaded;
        using (new FileStream(SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            loaded = store.Load();
        }

        Assert.Empty(loaded.Customs);
        Assert.Throws<InvalidOperationException>(() => store.Save(loaded));
        Assert.Equal(original, File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Save_leaves_no_temporary_file()
    {
        new SettingsStore(SettingsPath).Save(new AppSettings(true));
        Assert.Equal(["settings.json"], Directory.GetFiles(_dir).Select(Path.GetFileName));
    }

    [Fact]
    public void Save_creates_directory()
    {
        var path = Path.Combine(_dir, "nested", "settings.json");
        new SettingsStore(path).Save(new AppSettings());
        Assert.True(File.Exists(path));
    }
}

public class LayoutCatalogTests
{
    private static LayoutDefinition Custom(string id, string name) => BuiltInLayout.Create() with { Id = id, Name = name };

    [Fact]
    public void Active_falls_back_to_builtin_for_unknown_id() =>
        Assert.Equal(BuiltInLayout.Id, LayoutCatalog.Active(new AppSettings(Layout: "gone")).Id);

    [Fact]
    public void Active_finds_custom_layout()
    {
        var s = new AppSettings(Layout: "a", CustomLayouts: [Custom("a", "Alpha")]);
        Assert.Equal("Alpha", LayoutCatalog.Active(s).Name);
    }

    [Fact]
    public void Active_builtin_uses_renamed_name() =>
        Assert.Equal("House", LayoutCatalog.Active(new AppSettings(BuiltInName: "House")).Name);

    [Fact]
    public void All_lists_builtin_first_then_customs_alphabetically()
    {
        var s = new AppSettings(CustomLayouts: [Custom("b", "beta"), Custom("a", "Alpha")], BuiltInName: "Zed");
        Assert.Equal(["Zed", "Alpha", "beta"], LayoutCatalog.All(s).Select(l => l.Name));
    }
}
