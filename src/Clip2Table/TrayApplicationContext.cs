using System.Diagnostics;
using Clip2Table.Core.Conversion;
using Clip2Table.Core.Rendering;
using Clip2Table.Core.Settings;
using Clip2Table.Core.Styling;
using Clip2Table.Customize;
using Clip2Table.Native;
using Microsoft.Win32;

namespace Clip2Table;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int DebounceMs = 150;
    private const int BalloonTimeoutMs = 3000;

    /// <summary>Auto mode leaves clipboard text longer than this alone; Convert now is uncapped.</summary>
    private const int AutoMaxTextLength = 1_000_000;
    private const string NoTableMessage = "No table found on the clipboard";
    private const string WriteFailedMessage = "Couldn't write to the clipboard";

    private readonly SettingsStore store;
    private readonly ClipboardMonitor monitor;
    private readonly System.Windows.Forms.Timer debounce;
    private readonly NotifyIcon notifyIcon;
    private readonly ContextMenuStrip menu;
    private readonly ToolStripMenuItem autoItem;
    private readonly ToolStripMenuItem layoutItem;
    private readonly ToolStripMenuItem startupItem;

    private AppSettings settings;
    private LayoutDialog? dialog;
    private Icon? currentIcon;
    private uint? lastOwnWriteSeq;
    private bool disposed;

    public TrayApplicationContext(SettingsStore store)
    {
        this.store = store;
        settings = store.Load();

        monitor = new ClipboardMonitor();
        monitor.ClipboardChanged += OnClipboardChanged;

        debounce = new System.Windows.Forms.Timer { Interval = DebounceMs };
        debounce.Tick += OnDebounceTick;

        var convertItem = new ToolStripMenuItem("Convert now", null, OnConvertNow);
        autoItem = new ToolStripMenuItem("Auto-convert", null, OnToggleAuto);
        layoutItem = new ToolStripMenuItem("Layout");
        RebuildLayoutMenu();

        startupItem = new ToolStripMenuItem("Start with Windows", null, OnToggleStartup);
        var exitItem = new ToolStripMenuItem("Exit", null, OnExit);

        menu = new ContextMenuStrip();
        menu.Items.AddRange([convertItem, autoItem, layoutItem, startupItem, new ToolStripSeparator(), exitItem]);
        menu.Opening += OnMenuOpening;

        notifyIcon = new NotifyIcon { ContextMenuStrip = menu };
        ApplyAutoState();
        RefreshMenuState();
        notifyIcon.Visible = true;

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        try
        {
            // A switch between light and dark taskbar arrives as a General change; redraw in the new ink.
            if (e.Category == UserPreferenceCategory.General)
            {
                ApplyAutoState();
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Clip2Table: icon refresh after theme change failed: {ex}");
        }
    }

    private Layout CurrentLayout => LayoutCatalog.Active(settings).ToLayout();

    private void RebuildLayoutMenu()
    {
        // Disposing the old items releases their resources; the submenu is rebuilt whenever layouts change.
        var old = layoutItem.DropDownItems.Cast<ToolStripItem>().ToList();
        layoutItem.DropDownItems.Clear();
        old.ForEach(i => i.Dispose());

        foreach (var layout in LayoutCatalog.All(settings))
        {
            // "&&" shows a literal ampersand; a single "&" would turn the next letter into a mnemonic.
            layoutItem.DropDownItems.Add(new ToolStripMenuItem(layout.Name.Replace("&", "&&"), null, OnLayoutSelected) { Tag = layout.Id });
        }

        layoutItem.DropDownItems.Add(new ToolStripSeparator());
        layoutItem.DropDownItems.Add(new ToolStripMenuItem("Customize…", null, OnCustomize));
        RefreshMenuState();
    }

    private void OnCustomize(object? sender, EventArgs e)
    {
        try
        {
            if (dialog is not null)
            {
                dialog.Activate();
                return;
            }

            var text = Win32Clipboard.TryRead(monitor.Handle)?.Text;
            var opened = new LayoutDialog(() => settings, ApplyDialogUpdate, text);
            opened.FormClosed += (_, _) =>
            {
                dialog = null;
                opened.Dispose();
            };
            dialog = opened;
            opened.Show();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ApplyDialogUpdate(Func<AppSettings, AppSettings> change)
    {
        try
        {
            UpdateSettings(change(settings));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            // In-memory settings changed even if saving failed; the menu must match them.
            RebuildLayoutMenu();
        }
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        try
        {
            debounce.Stop();
            debounce.Start();
        }
        catch (Exception ex)
        {
            // Never let a clipboard notification take the tray app down.
            Trace.WriteLine($"Clip2Table: clipboard notification failed: {ex}");
        }
    }

    private void ArmOwnWriteGuard(uint? writeSeq)
    {
        // Set on success and on failure alike: any change we made must not re-trigger auto mode.
        if (writeSeq is not null)
        {
            lastOwnWriteSeq = writeSeq;
        }
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        try
        {
            debounce.Stop();

            if (!settings.AutoConvert || Win32Clipboard.SequenceNumber == lastOwnWriteSeq)
            {
                return;
            }

            var content = Win32Clipboard.TryRead(monitor.Handle, AutoMaxTextLength);
            if (content is null || content.HasOwnMarker || content.TooLarge)
            {
                return;
            }

            var outcome = ClipboardConverter.Convert(content.Text, content.Html, ConversionTrigger.Auto, CurrentLayout);
            if (!outcome.Converted)
            {
                return;
            }

            var written = Win32Clipboard.TryWrite(monitor.Handle, content.SequenceNumber, content.Text!, outcome.CfHtml!, out var writeSeq);
            ArmOwnWriteGuard(writeSeq);
            if (!written)
            {
                Trace.WriteLine("Clip2Table: auto-convert did not write (clipboard busy, changed since read, or write failed).");
            }
        }
        catch (Exception ex)
        {
            // Auto mode stays silent toward the user, but leaves a trace for diagnosis.
            Trace.WriteLine($"Clip2Table: auto-convert failed: {ex}");
        }
    }

    private void OnConvertNow(object? sender, EventArgs e)
    {
        try
        {
            var content = Win32Clipboard.TryRead(monitor.Handle);
            if (content is null)
            {
                ShowBalloon(NoTableMessage, ToolTipIcon.Info);
                return;
            }

            var outcome = ClipboardConverter.Convert(content.Text, content.Html, ConversionTrigger.Manual, CurrentLayout);
            if (!outcome.Converted)
            {
                ShowBalloon(NoTableMessage, ToolTipIcon.Info);
                return;
            }

            // A clipboard that changed since the read counts as "not written" too: the newer copy is left alone.
            var written = Win32Clipboard.TryWrite(monitor.Handle, content.SequenceNumber, content.Text!, outcome.CfHtml!, out var writeSeq);
            ArmOwnWriteGuard(writeSeq);
            if (!written)
            {
                ShowBalloon(WriteFailedMessage, ToolTipIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void OnToggleAuto(object? sender, EventArgs e)
    {
        try
        {
            UpdateSettings(settings with { AutoConvert = !settings.AutoConvert });
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            // The in-memory state changed even if saving failed; the icon and menu must match it.
            ApplyUiState();
        }
    }

    private void OnLayoutSelected(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ToolStripMenuItem { Tag: string id })
            {
                UpdateSettings(settings with { Layout = id });
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            ApplyUiState();
        }
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        try
        {
            if (StartupRegistration.IsEnabled())
            {
                StartupRegistration.Disable();
            }
            else
            {
                StartupRegistration.Enable();
            }

            startupItem.Checked = StartupRegistration.IsEnabled();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            RefreshMenuState();
            startupItem.Checked = StartupRegistration.IsEnabled();
        }
        catch (Exception ex)
        {
            // A registry read failure must not stop the menu from opening.
            Trace.WriteLine($"Clip2Table: menu refresh failed: {ex}");
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        try
        {
            dialog?.CloseWithoutPrompt();
            notifyIcon.Visible = false;
        }
        finally
        {
            ExitThread();
        }
    }

    private void UpdateSettings(AppSettings updated)
    {
        // Apply in memory first so the app behaves as the user chose even if saving fails.
        settings = updated;
        store.Save(updated);
    }

    private void ApplyUiState()
    {
        try
        {
            ApplyAutoState();
            RefreshMenuState();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RefreshMenuState()
    {
        autoItem.Checked = settings.AutoConvert;
        var currentId = LayoutCatalog.Active(settings).Id;
        foreach (ToolStripItem item in layoutItem.DropDownItems)
        {
            if (item is ToolStripMenuItem layoutEntry)
            {
                layoutEntry.Checked = layoutEntry.Tag as string == currentId;
            }
        }
    }

    private void ApplyAutoState()
    {
        var newIcon = TrayIcons.Create(settings.AutoConvert);
        var oldIcon = currentIcon;
        notifyIcon.Icon = newIcon;
        notifyIcon.Text = $"Clip2Table {GitVersionInformation.SemVer}: Auto {(settings.AutoConvert ? "on" : "off")}";
        currentIcon = newIcon;
        ReleaseIcon(oldIcon);
    }

    private void ShowBalloon(string message, ToolTipIcon icon)
    {
        try
        {
            notifyIcon.ShowBalloonTip(BalloonTimeoutMs, "Clip2Table", message, icon);
        }
        catch (Exception)
        {
            // Nothing further to surface to.
        }
    }

    private void ShowError(Exception ex) => ShowBalloon(ex.Message, ToolTipIcon.Error);

    private static void ReleaseIcon(Icon? icon)
    {
        if (icon is null)
        {
            return;
        }

        var handle = icon.Handle;
        icon.Dispose();
        NativeMethods.DestroyIcon(handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            disposed = true;
            dialog?.CloseWithoutPrompt();
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            debounce.Stop();
            debounce.Dispose();
            monitor.ClipboardChanged -= OnClipboardChanged;
            monitor.Dispose();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            menu.Dispose();
            ReleaseIcon(currentIcon);
            currentIcon = null;
        }

        base.Dispose(disposing);
    }
}
