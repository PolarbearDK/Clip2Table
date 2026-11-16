using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Clip2Table.Customize;

/// <summary>
/// Shows rendered table HTML in WebView2, so the preview is the exact markup that gets pasted.
/// Falls back to a message when the WebView2 runtime is missing.
/// </summary>
internal sealed class PreviewPane : UserControl
{
    private const string UnavailableMessage = "Preview unavailable — the Microsoft Edge WebView2 Runtime is not installed.";

    private readonly WebView2 webView = new() { Dock = DockStyle.Fill };
    private readonly Label fallback = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = UnavailableMessage,
        Visible = false,
    };

    private bool ready;
    private bool initStarted;
    private string? pendingHtml;

    public PreviewPane()
    {
        BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(webView);
        Controls.Add(fallback);
    }

    /// <summary>The latest call wins; calls made before WebView2 is ready are applied once it is.</summary>
    public void ShowHtml(string html)
    {
        pendingHtml = html;
        if (ready)
        {
            Flush();
        }
    }

    protected override async void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (initStarted)
        {
            return;
        }

        initStarted = true;
        try
        {
            var dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clip2Table", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, dataFolder);
            await webView.EnsureCoreWebView2Async(environment);

            var core = webView.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.NavigationStarting += (_, args) =>
            {
                // Only our own NavigateToString content may load.
                if (!args.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    args.Cancel = true;
                }
            };

            ready = true;
            Flush();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Clip2Table: preview unavailable: {ex}");
            if (!IsDisposed)
            {
                webView.Visible = false;
                fallback.Visible = true;
            }
        }
    }

    private void Flush()
    {
        if (pendingHtml is null || IsDisposed)
        {
            return;
        }

        var html = pendingHtml;
        pendingHtml = null;
        try
        {
            webView.NavigateToString(html);
            webView.Visible = true;
            fallback.Visible = false;
        }
        catch (ArgumentException ex)
        {
            // NavigateToString rejects documents over about 2 MB.
            Trace.WriteLine($"Clip2Table: preview too large: {ex.Message}");
            fallback.Text = "This table is too large to preview.";
            webView.Visible = false;
            fallback.Visible = true;
        }
    }
}
