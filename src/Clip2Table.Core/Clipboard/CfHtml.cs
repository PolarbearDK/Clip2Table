using System.Text;

namespace Clip2Table.Core.Clipboard;

public static class CfHtml
{
    private const string HeaderTemplate =
        "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
    private const string Prefix = "<html><body>\r\n<!--StartFragment-->";
    private const string Suffix = "<!--EndFragment-->\r\n</body></html>";

    public static byte[] Build(string fragmentHtml)
    {
        var utf8 = new UTF8Encoding(false);
        var headerLength = utf8.GetByteCount(string.Format(HeaderTemplate, 0, 0, 0, 0));
        var prefixLength = utf8.GetByteCount(Prefix);
        var fragmentLength = utf8.GetByteCount(fragmentHtml);
        var suffixLength = utf8.GetByteCount(Suffix);

        var startHtml = headerLength;
        var startFragment = headerLength + prefixLength;
        var endFragment = startFragment + fragmentLength;
        var endHtml = endFragment + suffixLength;

        var header = string.Format(HeaderTemplate, startHtml, endHtml, startFragment, endFragment);
        return utf8.GetBytes(header + Prefix + fragmentHtml + Suffix);
    }
}
