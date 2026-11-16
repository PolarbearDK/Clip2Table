using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Clip2Table.Native;

internal sealed class ClipboardMonitor : NativeWindow, IDisposable
{
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private bool listening;

    public ClipboardMonitor()
    {
        CreateHandle(new CreateParams { Parent = HWND_MESSAGE });
        listening = NativeMethods.AddClipboardFormatListener(Handle);
        if (!listening)
        {
            Trace.WriteLine($"Clip2Table: AddClipboardFormatListener failed, Win32 error {Marshal.GetLastPInvokeError()}.");
        }
    }

    public event EventHandler? ClipboardChanged;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            if (listening)
            {
                NativeMethods.RemoveClipboardFormatListener(Handle);
                listening = false;
            }

            DestroyHandle();
        }
    }
}
