using System.Text;

namespace Clip2Table.Core.Rendering;

public static class HtmlTableRenderer
{
    public static string Render(ParsedTable table, Layout layout)
    {
        var columns = table.Header.Count;
        var alignments = new string[columns];
        for (var c = 0; c < columns; c++)
        {
            alignments[c] = ColumnAnalysis.EffectiveAlignment(table, c) switch
            {
                ColumnAlignment.Center => "center",
                ColumnAlignment.Right => "right",
                _ => "left",
            };
        }

        var sb = new StringBuilder();
        sb.Append("<table style=\"").Append(layout.TableStyle).Append("\">");

        sb.Append("<tr>");
        for (var c = 0; c < columns; c++)
        {
            sb.Append("<th style=\"").Append(layout.HeaderCellStyle)
              .Append(";text-align:").Append(alignments[c]).Append("\">")
              .Append(Escape(table.Header[c])).Append("</th>");
        }

        sb.Append("</tr>");

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var striped = layout.StripeBackground is not null && r % 2 == 1;
            var row = table.Rows[r];

            sb.Append(striped ? $"<tr style=\"background:{layout.StripeBackground}\">" : "<tr>");
            for (var c = 0; c < columns; c++)
            {
                var cell = c < row.Count ? row[c] : "";
                sb.Append("<td style=\"").Append(layout.DataCellStyle)
                  .Append(";text-align:").Append(alignments[c]);
                if (striped)
                {
                    sb.Append(";background:").Append(layout.StripeBackground);
                }

                if (cell == "NULL")
                {
                    sb.Append(';').Append(layout.NullCellStyle);
                }

                sb.Append("\">").Append(cell.Length == 0 ? "&nbsp;" : Escape(cell)).Append("</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
