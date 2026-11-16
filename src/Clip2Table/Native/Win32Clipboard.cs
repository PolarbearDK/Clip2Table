using System.Runtime.InteropServices;
using System.Text;

namespace Clip2Table.Native;

/// <param name="SequenceNumber">
/// Clipboard sequence number recorded while the clipboard was open for this read. Pass it to
/// <see cref="Win32Clipboard.TryWrite"/> so a newer copy made in the meantime is never overwritten.
/// </param>
/// <param name="TooLarge">
/// The text exceeded the caller's length cap; <see cref="Text"/> and <see cref="Html"/> were not read.
/// </param>
internal sealed record ClipboardContent(string? Text, string? Html, bool HasOwnMarker, uint SequenceNumber, bool TooLarge = false);

internal static class Win32Clipboard
{
    private const int OpenAttempts = 10;
    private const int OpenRetryDelayMs = 25;

    private static readonly uint HtmlFormat = NativeMethods.RegisterClipboardFormatW("HTML Format");
    private static readonly uint MarkerFormat = NativeMethods.RegisterClipboardFormatW("Clip2Table.Converted");

    public static uint SequenceNumber => NativeMethods.GetClipboardSequenceNumber();

    /// <param name="maxTextLength">
    /// When set and the text is longer, the HTML Format is not read and the result is flagged
    /// <see cref="ClipboardContent.TooLarge"/>.
    /// </param>
    public static ClipboardContent? TryRead(IntPtr owner, int? maxTextLength = null)
    {
        if (!TryOpen(owner))
        {
            return null;
        }

        try
        {
            var hasMarker = MarkerFormat != 0 && NativeMethods.IsClipboardFormatAvailable(MarkerFormat);
            var text = ReadUnicodeText();
            if (text is not null && text.Length > maxTextLength)
            {
                return new ClipboardContent(null, null, hasMarker, NativeMethods.GetClipboardSequenceNumber(), TooLarge: true);
            }

            var html = HtmlFormat != 0 ? ReadUtf8(HtmlFormat) : null;

            // Recorded after reading, still while open: rendering a delayed format during the reads may bump it.
            return new ClipboardContent(text, html, hasMarker, NativeMethods.GetClipboardSequenceNumber());
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    /// <summary>
    /// Replaces the clipboard with the original text, the CF_HTML bytes and the Clip2Table marker.
    /// </summary>
    /// <param name="writeSequence">
    /// Set whenever this call changed the clipboard (EmptyClipboard succeeded), on success and on failure,
    /// to the sequence number read immediately after CloseClipboard. Callers arm their own-write guard with it.
    /// It cannot be read before CloseClipboard: closing synthesizes CF_TEXT, CF_OEMTEXT and CF_LOCALE, and each
    /// of those bumps the sequence number again, so a value read while open would never match.
    /// </param>
    /// <param name="expectedSequence">
    /// The <see cref="ClipboardContent.SequenceNumber"/> of the read this write is based on. If the clipboard
    /// changed since, nothing is written (and <paramref name="writeSequence"/> stays null): never overwrite a newer copy.
    /// </param>
    public static bool TryWrite(IntPtr owner, uint expectedSequence, string text, byte[] cfHtml, out uint? writeSequence)
    {
        writeSequence = null;
        if (HtmlFormat == 0 || MarkerFormat == 0 || !TryOpen(owner))
        {
            return false;
        }

        var changed = false;
        try
        {
            if (NativeMethods.GetClipboardSequenceNumber() != expectedSequence)
            {
                return false;
            }

            if (!NativeMethods.EmptyClipboard())
            {
                return false;
            }

            changed = true;

            var textBytes = new byte[(text.Length + 1) * sizeof(char)];
            Encoding.Unicode.GetBytes(text, 0, text.Length, textBytes, 0);

            var htmlBytes = new byte[cfHtml.Length + 1];
            Buffer.BlockCopy(cfHtml, 0, htmlBytes, 0, cfHtml.Length);

            var written = SetData(NativeMethods.CF_UNICODETEXT, textBytes)
                && SetData(HtmlFormat, htmlBytes)
                && SetData(MarkerFormat, [1]);

            if (!written)
            {
                // Best effort: never leave the user's copy destroyed or half-converted.
                // Fall back to the original text alone.
                if (NativeMethods.EmptyClipboard())
                {
                    SetData(NativeMethods.CF_UNICODETEXT, textBytes);
                }
            }

            return written;
        }
        finally
        {
            NativeMethods.CloseClipboard();
            if (changed)
            {
                writeSequence = NativeMethods.GetClipboardSequenceNumber();
            }
        }
    }

    private static bool TryOpen(IntPtr owner)
    {
        for (var attempt = 0; attempt < OpenAttempts; attempt++)
        {
            if (NativeMethods.OpenClipboard(owner))
            {
                return true;
            }

            Thread.Sleep(OpenRetryDelayMs);
        }

        return false;
    }

    private static string? ReadUnicodeText()
    {
        if (!NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_UNICODETEXT))
        {
            return null;
        }

        var bytes = ReadBytes(NativeMethods.CF_UNICODETEXT);
        if (bytes is null)
        {
            return null;
        }

        var text = Encoding.Unicode.GetString(bytes, 0, bytes.Length & ~1);
        var nul = text.IndexOf('\0');
        return nul >= 0 ? text[..nul] : text;
    }

    private static string? ReadUtf8(uint format)
    {
        if (!NativeMethods.IsClipboardFormatAvailable(format))
        {
            return null;
        }

        var bytes = ReadBytes(format);
        if (bytes is null)
        {
            return null;
        }

        var nul = Array.IndexOf(bytes, (byte)0);
        return Encoding.UTF8.GetString(bytes, 0, nul >= 0 ? nul : bytes.Length);
    }

    private static byte[]? ReadBytes(uint format)
    {
        var handle = NativeMethods.GetClipboardData(format);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        var size = NativeMethods.GlobalSize(handle);
        if (size == 0 || size > int.MaxValue)
        {
            return null;
        }

        var pointer = NativeMethods.GlobalLock(handle);
        if (pointer == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var bytes = new byte[(int)size];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            NativeMethods.GlobalUnlock(handle);
        }
    }

    private static bool SetData(uint format, byte[] data)
    {
        var handle = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (nuint)data.Length);
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        var pointer = NativeMethods.GlobalLock(handle);
        if (pointer == IntPtr.Zero)
        {
            NativeMethods.GlobalFree(handle);
            return false;
        }

        try
        {
            Marshal.Copy(data, 0, pointer, data.Length);
        }
        finally
        {
            NativeMethods.GlobalUnlock(handle);
        }

        if (NativeMethods.SetClipboardData(format, handle) == IntPtr.Zero)
        {
            NativeMethods.GlobalFree(handle);
            return false;
        }

        return true;
    }
}
