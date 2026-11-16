using System.Text.Json;
using System.Text.Json.Nodes;

namespace Clip2Table.Core.Styling;

/// <summary>Either the parsed layouts or a message for the user, never both.</summary>
public sealed record ImportParse(IReadOnlyList<LayoutDefinition>? Layouts, string? Error);

public sealed record ImportMerge(IReadOnlyList<LayoutDefinition> Added, int Skipped);

/// <summary>Moves layouts between people as a <c>.json</c> file holding any number of layouts.</summary>
public static class LayoutExchange
{
    private const string Format = "clip2table-layouts";
    private const int Version = 1;
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string Export(IEnumerable<LayoutDefinition> layouts)
    {
        var root = new JsonObject
        {
            ["format"] = Format,
            ["version"] = Version,
            ["layouts"] = new JsonArray([.. layouts.Select(l => (JsonNode)LayoutJson.ToJson(l, includeId: false))]),
        };
        return root.ToJsonString(WriteOptions);
    }

    /// <summary>Validates the whole file; one bad layout rejects it all. Parsed layouts get fresh ids.</summary>
    public static ImportParse Parse(string json)
    {
        const string NotOurs = "This is not a Clip2Table layouts file.";
        JsonNode? format, versionNode, layoutsNode;
        try
        {
            if (JsonNode.Parse(json) is not JsonObject root)
            {
                return new(null, NotOurs);
            }

            (format, versionNode, layoutsNode) = (root["format"], root["version"], root["layouts"]);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            // ArgumentException: a duplicate property name, detected when the object is first accessed.
            return new(null, NotOurs);
        }

        if (!IsString(format, Format))
        {
            return new(null, NotOurs);
        }

        if (versionNode is not JsonValue v || !v.TryGetValue<int>(out var version) || version != Version)
        {
            return new(null, "This file was made by a newer version of Clip2Table.");
        }

        if (layoutsNode is not JsonArray array || array.Count == 0)
        {
            return new(null, "The file contains no layouts.");
        }

        var layouts = new List<LayoutDefinition>();
        for (var i = 0; i < array.Count; i++)
        {
            var layout = LayoutJson.FromJson(array[i], Guid.NewGuid().ToString("N"), out var field);
            if (layout is null)
            {
                return new(null, $"Layout {i + 1} ('{NameOrPlaceholder(array[i])}') has an invalid {field}.");
            }

            layouts.Add(layout);
        }

        return new(layouts, null);
    }

    /// <summary>
    /// In file order: a layout that looks the same as an existing or already-added one is skipped, names ignored.
    /// The rest are added, with " (2)", " (3)", … appended to a taken name.
    /// </summary>
    public static ImportMerge Merge(IReadOnlyList<LayoutDefinition> existing, IReadOnlyList<LayoutDefinition> imported)
    {
        var added = new List<LayoutDefinition>();
        var skipped = 0;
        foreach (var layout in imported)
        {
            if (existing.Concat(added).Any(l => l.HasSameStyle(layout)))
            {
                skipped++;
                continue;
            }

            var name = LayoutNames.MakeUnique(layout.Name, existing.Concat(added).Select(l => l.Name));
            added.Add(layout with { Name = name });
        }

        return new(added, skipped);
    }

    public static string Summary(ImportMerge m)
    {
        var text = $"Imported {m.Added.Count} {(m.Added.Count == 1 ? "layout" : "layouts")}.";
        return m.Skipped > 0 ? $"{text} {m.Skipped} {(m.Skipped == 1 ? "was" : "were")} already present." : text;
    }

    private static string NameOrPlaceholder(JsonNode? node)
    {
        try
        {
            return node is JsonObject o && o["name"] is JsonValue n && n.TryGetValue<string>(out var s) && s.Trim().Length > 0 ? s : "?";
        }
        catch (ArgumentException)
        {
            return "?";
        }
    }

    private static bool IsString(JsonNode? node, string expected) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) && s == expected;
}
