using System.Text.Json;
using System.Text.Json.Nodes;

namespace Clip2Table.Core.Styling;

/// <summary>The JSON shape of one layout, shared by the settings file and the export file.</summary>
internal static class LayoutJson
{
    public static JsonObject ToJson(LayoutDefinition d, bool includeId)
    {
        var o = new JsonObject();
        if (includeId)
        {
            o["id"] = d.Id;
        }

        o["name"] = d.Name;
        o["fontFamily"] = d.FontFamily;
        o["fontSize"] = d.FontSize;
        o["textColor"] = d.TextColor;
        o["headerBackground"] = d.HeaderBackground;
        o["headerTextColor"] = d.HeaderTextColor;
        o["headerBold"] = d.HeaderBold;
        o["headerBorderColor"] = d.HeaderBorderColor;
        o["cellBorderColor"] = d.CellBorderColor;
        o["stripes"] = d.Stripes;
        o["stripeBackground"] = d.StripeBackground;
        o["nullTextColor"] = d.NullTextColor;
        o["nullItalic"] = d.NullItalic;
        o["padding"] = new JsonArray(d.Padding.Vertical, d.Padding.Horizontal);
        return o;
    }

    /// <summary>
    /// Reads and validates one layout. Uses the node's <c>id</c> when present, otherwise <paramref name="fallbackId"/>.
    /// Returns null with the offending camelCase field name when anything is missing, mistyped or invalid.
    /// </summary>
    public static LayoutDefinition? FromJson(JsonNode? node, string? fallbackId, out string? invalidField)
    {
        try
        {
            return Read(node, fallbackId, out invalidField);
        }
        catch (ArgumentException)
        {
            // JsonNode accepts duplicate property names and only throws once the object is accessed.
            invalidField = "layout";
            return null;
        }
    }

    private static LayoutDefinition? Read(JsonNode? node, string? fallbackId, out string? invalidField)
    {
        invalidField = null;
        if (node is not JsonObject o)
        {
            invalidField = "layout";
            return null;
        }

        var reader = new Reader(o);
        var id = o["id"] is null ? fallbackId : reader.String("id");
        if (id is null)
        {
            invalidField = reader.Failed ?? "id";
            return null;
        }

        var definition = new LayoutDefinition(
            Id: id,
            Name: reader.String("name"),
            FontFamily: reader.String("fontFamily"),
            FontSize: reader.Int("fontSize"),
            TextColor: reader.String("textColor"),
            HeaderBackground: reader.String("headerBackground"),
            HeaderTextColor: reader.String("headerTextColor"),
            HeaderBold: reader.Bool("headerBold"),
            HeaderBorderColor: reader.String("headerBorderColor"),
            CellBorderColor: reader.String("cellBorderColor"),
            Stripes: reader.Bool("stripes"),
            StripeBackground: reader.String("stripeBackground"),
            NullTextColor: reader.String("nullTextColor"),
            NullItalic: reader.Bool("nullItalic"),
            Padding: reader.Padding("padding"));

        invalidField = reader.Failed ?? LayoutValidator.FindInvalidField(definition);
        return invalidField is null ? definition : null;
    }

    /// <summary>Reads fields leniently, remembering the first one that is missing or of the wrong type.</summary>
    private sealed class Reader(JsonObject o)
    {
        public string? Failed { get; private set; }

        public string String(string field) => Read(field, JsonValueKind.String, v => v.GetValue<string>(), "");

        public bool Bool(string field) =>
            o[field] is JsonValue v && v.GetValueKind() is JsonValueKind.True or JsonValueKind.False
                ? v.GetValue<bool>()
                : Fail(field, false);

        public int Int(string field) => Read(field, JsonValueKind.Number, v => v.TryGetValue<int>(out var i) ? i : throw new FormatException(), 0);

        public CellPadding Padding(string field)
        {
            if (o[field] is JsonArray { Count: 2 } a
                && a[0] is JsonValue v && v.GetValueKind() == JsonValueKind.Number && v.TryGetValue<int>(out var vertical)
                && a[1] is JsonValue h && h.GetValueKind() == JsonValueKind.Number && h.TryGetValue<int>(out var horizontal))
            {
                return new CellPadding(vertical, horizontal);
            }

            return Fail(field, default(CellPadding));
        }

        private T Read<T>(string field, JsonValueKind kind, Func<JsonValue, T> get, T fallback)
        {
            if (o[field] is JsonValue v && v.GetValueKind() == kind)
            {
                try
                {
                    return get(v);
                }
                catch (FormatException)
                {
                }
            }

            return Fail(field, fallback);
        }

        private T Fail<T>(string field, T fallback)
        {
            Failed ??= field;
            return fallback;
        }
    }
}
