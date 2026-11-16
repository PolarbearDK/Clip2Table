using System.Text.Json;
using System.Text.Json.Nodes;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Settings;

public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>
    /// Set when the file exists but could not be read and no backup of it could be made. Saving then would
    /// replace the user's settings (and custom layouts) with defaults, so <see cref="Save"/> refuses.
    /// </summary>
    private bool saveBlocked;

    public static string DefaultPath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clip2Table", "settings.json");

    /// <summary>
    /// Reads settings, falling back to defaults per field. An invalid custom layout is skipped without
    /// disturbing the rest. An unparseable file is copied to <c>.bak</c> so the next save can't destroy it.
    /// </summary>
    public AppSettings Load()
    {
        saveBlocked = false;
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Possibly transient (a scanner or sync client holding the file): keep the file, don't overwrite it.
            saveBlocked = true;
            return new AppSettings();
        }

        try
        {
            return Parse(text) ?? Unparseable();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            // ArgumentException: a duplicate property name, detected when the object is first accessed.
            return Unparseable();
        }
    }

    /// <exception cref="InvalidOperationException">The file couldn't be read at load, so saving would lose it.</exception>
    public void Save(AppSettings s)
    {
        if (saveBlocked)
        {
            throw new InvalidOperationException("Settings couldn't be read at startup, so changes aren't saved. Restart Clip2Table.");
        }

        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var root = new JsonObject
        {
            ["autoConvert"] = s.AutoConvert,
            ["layout"] = s.Layout,
        };
        if (s.BuiltInName is not null)
        {
            root["builtInName"] = s.BuiltInName;
        }

        root["customLayouts"] = new JsonArray([.. s.Customs.Select(l => (JsonNode)LayoutJson.ToJson(l, includeId: true))]);

        // Write then rename, so a crash mid-write never leaves a truncated settings file.
        var temp = path + ".tmp";
        File.WriteAllText(temp, root.ToJsonString(WriteOptions));
        File.Move(temp, path, overwrite: true);
    }

    private static AppSettings? Parse(string text)
    {
        if (JsonNode.Parse(text) is not JsonObject root)
        {
            return null;
        }

        var defaults = new AppSettings();
        var autoConvert = TryGet(root["autoConvert"], defaults.AutoConvert);
        var layout = TryGet(root["layout"], defaults.Layout);
        var builtInName = TryGet<string?>(root["builtInName"], null);

        var customs = new List<LayoutDefinition>();
        if (root["customLayouts"] is JsonArray layouts)
        {
            foreach (var node in layouts)
            {
                if (LayoutJson.FromJson(node, fallbackId: null, out _) is { } custom)
                {
                    customs.Add(custom);
                }
            }
        }

        return new AppSettings(autoConvert, layout, customs, builtInName);
    }

    private AppSettings Unparseable()
    {
        try
        {
            File.Copy(path, path + ".bak", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Without a backup, the next save would destroy the only copy.
            saveBlocked = true;
        }

        return new AppSettings();
    }

    private static T TryGet<T>(JsonNode? node, T fallback)
    {
        try
        {
            return node is JsonValue v && v.TryGetValue<T>(out var value) ? value : fallback;
        }
        catch (InvalidOperationException)
        {
            return fallback;
        }
    }
}
