using Clip2Table.Core;
using Clip2Table.Core.Settings;
using Clip2Table.Core.Styling;

namespace Clip2Table.Customize;

/// <summary>
/// Creates, edits, renames, deletes, exports and imports layouts, with a live preview.
/// Settings are read through <c>current</c> and changed only through <c>update</c>, a transform applied to the
/// tray's live settings, so changes made in the tray while this is open (e.g. Auto-convert) are never reverted.
/// </summary>
internal sealed class LayoutDialog : Form
{
    private const string BaseTitle = "Customize layouts";
    private const string FileFilter = "Clip2Table layouts (*.json)|*.json";

    private readonly Func<AppSettings> current;
    private readonly Action<Func<AppSettings, AppSettings>> update;
    private readonly ParsedTable previewTable;

    private readonly ComboBox layoutCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly Button importButton = new() { Text = "Import…", AutoSize = true };
    private readonly Button exportButton = new() { Text = "Export…", AutoSize = true };

    private readonly ComboBox fontCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ComboBox sizeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 55 };
    private readonly ColorField textColor = new();
    private readonly ColorField headerBackground = new();
    private readonly ColorField headerText = new();
    private readonly CheckBox headerBold = new() { Text = "Bold", AutoSize = true };
    private readonly ColorField headerBorder = new();
    private readonly ColorField cellBorder = new();
    private readonly CheckBox stripes = new() { Text = "Stripes", AutoSize = true };
    private readonly ColorField stripeBackground = new();
    private readonly ColorField nullText = new();
    private readonly CheckBox nullItalic = new() { Text = "Italic", AutoSize = true };
    private readonly Dictionary<PaddingPreset, RadioButton> paddingRadios = [];
    private readonly NumericUpDown paddingVertical = new() { Maximum = CellPadding.MaxVertical, Width = 50 };
    private readonly NumericUpDown paddingHorizontal = new() { Maximum = CellPadding.MaxHorizontal, Width = 50 };

    private readonly PreviewPane preview = new() { Dock = DockStyle.Fill, MinimumSize = new Size(0, 160) };
    private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 100 };

    private readonly Button saveButton = new() { Text = "Save", AutoSize = true };
    private readonly Button saveAsButton = new() { Text = "Save as…", AutoSize = true };
    private readonly Button renameButton = new() { Text = "Rename…", AutoSize = true };
    private readonly Button deleteButton = new() { Text = "Delete", AutoSize = true };

    private IReadOnlyList<LayoutDefinition> layouts = [];
    private LayoutDefinition selected;
    private int selectedIndex = -1;
    private bool loading;
    private bool closeWithoutPrompt;

    public LayoutDialog(Func<AppSettings> current, Action<Func<AppSettings, AppSettings>> update, string? clipboardText)
    {
        this.current = current;
        this.update = update;
        previewTable = PreviewSample.For(clipboardText);
        selected = LayoutCatalog.Active(current());

        Text = BaseTitle;
        Font = SystemFonts.MessageBoxFont ?? Font;
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowIcon = false;
        Size = new Size(820, 640);
        MinimumSize = new Size(760, 560);

        BuildUi();
        WireEvents();
        RebindLayouts(selected.Id);
    }

    /// <summary>Closes even with unsaved edits, for app shutdown.</summary>
    public void CloseWithoutPrompt()
    {
        closeWithoutPrompt = true;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Esc closes the dialog, unless it is closing an open dropdown.
        if (keyData == Keys.Escape && ActiveControl is not ComboBox { DroppedDown: true })
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!closeWithoutPrompt && e.CloseReason == CloseReason.UserClosing && IsPending && !ConfirmDiscard())
        {
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            previewTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool IsCustomSelected => selected.Id != BuiltInLayout.Id;

    private bool IsPending => ReadEditor() is not { } editor || editor != selected;

    private void BuildUi()
    {
        fontCombo.Items.AddRange([.. LayoutFonts.Families]);
        for (var size = LayoutFonts.MinSize; size <= LayoutFonts.MaxSize; size++)
        {
            sizeCombo.Items.Add(size);
        }

        textColor.AccessibleLabel = "Table text colour";
        headerBackground.AccessibleLabel = "Header background";
        headerText.AccessibleLabel = "Header text colour";
        headerBorder.AccessibleLabel = "Header border colour";
        cellBorder.AccessibleLabel = "Cell border colour";
        stripeBackground.AccessibleLabel = "Stripe background";
        nullText.AccessibleLabel = "NULL text colour";
        layoutCombo.AccessibleName = "Layout";
        fontCombo.AccessibleName = "Font";
        sizeCombo.AccessibleName = "Font size";
        paddingVertical.AccessibleName = "Vertical padding";
        paddingHorizontal.AccessibleName = "Horizontal padding";

        var top = Row(Caption("Layout"), layoutCombo, importButton, exportButton);

        var settings = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 8, 0, 8) };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddSetting(settings, "Table", Caption("Font"), fontCombo, Caption("Size"), sizeCombo, Caption("Text"), textColor);
        AddSetting(settings, "Header", Caption("Background"), headerBackground, Caption("Text"), headerText, headerBold);
        AddSetting(settings, "", Caption("Border"), headerBorder);
        AddSetting(settings, "Cells", Caption("Border"), cellBorder);
        AddSetting(settings, "Rows", stripes, Caption("Background"), stripeBackground);
        AddSetting(settings, "NULL", Caption("Text"), nullText, nullItalic);

        var paddingControls = new List<Control>();
        foreach (var preset in Enum.GetValues<PaddingPreset>())
        {
            var radio = new RadioButton { Text = preset.ToString(), AutoSize = true, Tag = preset };
            paddingRadios[preset] = radio;
            paddingControls.Add(radio);
        }

        paddingControls.AddRange([Caption("V"), paddingVertical, Caption("H"), paddingHorizontal]);
        AddSetting(settings, "Padding", [.. paddingControls]);

        var previewBox = new GroupBox { Text = "Preview", Dock = DockStyle.Fill, Padding = new Padding(8) };
        previewBox.Controls.Add(preview);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        buttons.Controls.AddRange([deleteButton, renameButton, saveAsButton, saveButton]);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(top, 0, 0);
        root.Controls.Add(settings, 0, 1);
        root.Controls.Add(previewBox, 0, 2);
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);
    }

    private void WireEvents()
    {
        layoutCombo.SelectedIndexChanged += OnLayoutSelected;
        fontCombo.SelectedIndexChanged += OnEditorChanged;
        sizeCombo.SelectedIndexChanged += OnEditorChanged;
        foreach (var field in new[] { textColor, headerBackground, headerText, headerBorder, cellBorder, stripeBackground, nullText })
        {
            field.ValueChanged += OnEditorChanged;
        }

        foreach (var box in new[] { headerBold, stripes, nullItalic })
        {
            box.CheckedChanged += OnEditorChanged;
        }

        foreach (var radio in paddingRadios.Values)
        {
            radio.CheckedChanged += OnPaddingPresetChanged;
        }

        // ValueChanged fires only when the box commits (on leaving it); TextChanged keeps Save, the title and
        // the preview current while typing, so clicking Save straight after typing a number works.
        foreach (var box in new[] { paddingVertical, paddingHorizontal })
        {
            box.ValueChanged += OnEditorChanged;
            box.TextChanged += OnEditorChanged;
        }

        stripes.CheckedChanged += (_, _) =>
        {
            // A disabled field must not keep Save disabled: drop a half-typed stripe colour when stripes go off.
            if (!stripes.Checked && !stripeBackground.IsValid)
            {
                stripeBackground.Value = stripeBackground.Value;
            }
        };
        previewTimer.Tick += OnPreviewTick;

        saveButton.Click += (_, _) => Run(OnSave);
        saveAsButton.Click += (_, _) => Run(OnSaveAs);
        renameButton.Click += (_, _) => Run(OnRename);
        deleteButton.Click += (_, _) => Run(OnDelete);
        importButton.Click += (_, _) => Run(OnImport);
        exportButton.Click += (_, _) => Run(OnExport);
    }

    private void RebindLayouts(string selectId)
    {
        layouts = LayoutCatalog.All(current());
        var index = Math.Max(0, layouts.ToList().FindIndex(l => l.Id == selectId));

        loading = true;
        try
        {
            layoutCombo.Items.Clear();
            layoutCombo.Items.AddRange([.. layouts.Select(l => l.Name)]);
            layoutCombo.SelectedIndex = index;
        }
        finally
        {
            loading = false;
        }

        selectedIndex = index;
        selected = layouts[index];
        LoadEditor(selected);
    }

    private void LoadEditor(LayoutDefinition d)
    {
        loading = true;
        try
        {
            fontCombo.SelectedItem = d.FontFamily;
            sizeCombo.SelectedItem = d.FontSize;
            textColor.Value = d.TextColor;
            headerBackground.Value = d.HeaderBackground;
            headerText.Value = d.HeaderTextColor;
            headerBold.Checked = d.HeaderBold;
            headerBorder.Value = d.HeaderBorderColor;
            cellBorder.Value = d.CellBorderColor;
            stripes.Checked = d.Stripes;
            stripeBackground.Value = d.StripeBackground;
            nullText.Value = d.NullTextColor;
            nullItalic.Checked = d.NullItalic;
            paddingVertical.Value = d.Padding.Vertical;
            paddingHorizontal.Value = d.Padding.Horizontal;
            paddingRadios[PaddingPresets.Detect(d.Padding)].Checked = true;
        }
        finally
        {
            loading = false;
        }

        OnEditorChanged(this, EventArgs.Empty);
    }

    /// <summary>The editor's values as a layout with the selected id and name, or null while a colour is invalid.</summary>
    private LayoutDefinition? ReadEditor()
    {
        ColorField[] fields = [textColor, headerBackground, headerText, headerBorder, cellBorder, stripeBackground, nullText];
        if (fields.Any(f => !f.IsValid) || fontCombo.SelectedItem is not string font || sizeCombo.SelectedItem is not int size)
        {
            return null;
        }

        return new LayoutDefinition(
            Id: selected.Id,
            Name: selected.Name,
            FontFamily: font,
            FontSize: size,
            TextColor: textColor.Value,
            HeaderBackground: headerBackground.Value,
            HeaderTextColor: headerText.Value,
            HeaderBold: headerBold.Checked,
            HeaderBorderColor: headerBorder.Value,
            CellBorderColor: cellBorder.Value,
            Stripes: stripes.Checked,
            StripeBackground: stripeBackground.Value,
            NullTextColor: nullText.Value,
            NullItalic: nullItalic.Checked,
            Padding: new CellPadding(TypedValue(paddingVertical), TypedValue(paddingHorizontal)));
    }

    /// <summary>The number as typed, if valid and in range, else the box's committed value.</summary>
    private static int TypedValue(NumericUpDown box) =>
        int.TryParse(box.Text, out var typed) && typed >= box.Minimum && typed <= box.Maximum ? typed : (int)box.Value;

    private void OnLayoutSelected(object? sender, EventArgs e)
    {
        if (loading || layoutCombo.SelectedIndex == selectedIndex)
        {
            return;
        }

        if (IsPending && !ConfirmDiscard())
        {
            loading = true;
            layoutCombo.SelectedIndex = selectedIndex;
            loading = false;
            return;
        }

        selectedIndex = layoutCombo.SelectedIndex;
        selected = layouts[selectedIndex];
        LoadEditor(selected);
    }

    private void OnPaddingPresetChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton { Checked: true, Tag: PaddingPreset preset } && !loading && preset != PaddingPreset.Custom)
        {
            var values = PaddingPresets.For(preset);
            loading = true;
            paddingVertical.Value = values.Vertical;
            paddingHorizontal.Value = values.Horizontal;
            loading = false;
        }

        OnEditorChanged(sender, e);
    }

    private void OnEditorChanged(object? sender, EventArgs e)
    {
        if (loading)
        {
            return;
        }

        var valid = ReadEditor() is not null;
        var pending = IsPending;
        Text = pending ? BaseTitle + " *" : BaseTitle;
        stripeBackground.Enabled = stripes.Checked;
        var custom = paddingRadios[PaddingPreset.Custom].Checked;
        paddingVertical.Enabled = custom;
        paddingHorizontal.Enabled = custom;

        saveButton.Enabled = IsCustomSelected && pending && valid;
        saveAsButton.Enabled = valid;
        renameButton.Enabled = !pending;
        deleteButton.Enabled = IsCustomSelected;
        importButton.Enabled = !pending;
        exportButton.Enabled = !pending && current().Customs.Count > 0;

        previewTimer.Stop();
        previewTimer.Start();
    }

    private void OnPreviewTick(object? sender, EventArgs e)
    {
        previewTimer.Stop();
        if (ReadEditor() is { } editor)
        {
            preview.ShowHtml(PreviewSample.Document(previewTable, editor.ToLayout()));
        }
    }

    private void OnSave()
    {
        if (ReadEditor() is not { } editor || !ConfirmReadable(editor))
        {
            return;
        }

        update(s => s with { CustomLayouts = [.. s.Customs.Select(l => l.Id == editor.Id ? editor : l)], Layout = editor.Id });
        RebindLayouts(editor.Id);
    }

    private void OnSaveAs()
    {
        if (ReadEditor() is not { } editor || !ConfirmReadable(editor))
        {
            return;
        }

        var name = NamePrompt.Ask(this, "Save layout as", $"{selected.Name} copy", n => LayoutNames.Validate(n, layouts.Select(l => l.Name)));
        if (name is null)
        {
            return;
        }

        var created = editor with { Id = Guid.NewGuid().ToString("N"), Name = name };
        update(s => s with { CustomLayouts = [.. s.Customs, created], Layout = created.Id });
        RebindLayouts(created.Id);
    }

    private void OnRename()
    {
        var others = layouts.Where(l => l.Id != selected.Id).Select(l => l.Name);
        var name = NamePrompt.Ask(this, "Rename layout", selected.Name, n => LayoutNames.Validate(n, others));
        if (name is null)
        {
            return;
        }

        var id = selected.Id;
        update(s => id == BuiltInLayout.Id
            ? s with { BuiltInName = name }
            : s with { CustomLayouts = [.. s.Customs.Select(l => l.Id == id ? l with { Name = name } : l)] });
        RebindLayouts(id);
    }

    private void OnDelete()
    {
        if (MessageBox.Show(this, $"Delete layout '{selected.Name}'?", BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes)
        {
            return;
        }

        var id = selected.Id;
        update(s => s with
        {
            CustomLayouts = [.. s.Customs.Where(l => l.Id != id)],
            Layout = s.Layout == id ? BuiltInLayout.Id : s.Layout,
        });
        RebindLayouts(BuiltInLayout.Id);
    }

    private void OnExport()
    {
        using var dialog = new SaveFileDialog { Filter = FileFilter, FileName = "Clip2Table layouts.json", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var customs = LayoutCatalog.All(current()).Where(l => l.Id != BuiltInLayout.Id);
        File.WriteAllText(dialog.FileName, LayoutExchange.Export(customs));
    }

    private void OnImport()
    {
        using var dialog = new OpenFileDialog { Filter = FileFilter };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var parsed = LayoutExchange.Parse(File.ReadAllText(dialog.FileName));
        if (parsed.Layouts is null)
        {
            MessageBox.Show(this, parsed.Error, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var merge = LayoutExchange.Merge(LayoutCatalog.All(current()), parsed.Layouts);
        if (merge.Added.Count > 0)
        {
            update(s => s with { CustomLayouts = [.. s.Customs, .. merge.Added] });
        }

        MessageBox.Show(this, LayoutExchange.Summary(merge), BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        RebindLayouts(merge.Added.Count > 0 ? merge.Added[0].Id : selected.Id);
    }

    private bool ConfirmReadable(LayoutDefinition editor)
    {
        var warnings = ReadabilityCheck.Find(editor);
        return warnings.Count == 0
            || MessageBox.Show(this, string.Join(Environment.NewLine, warnings) + Environment.NewLine + Environment.NewLine + "Save anyway?",
                BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private bool ConfirmDiscard() =>
        MessageBox.Show(this, "Discard changes?", BaseTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>Runs a button action, turning file and settings errors into a message instead of a crash.</summary>
    private void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Label Caption(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(8, 6, 3, 0) };

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
        row.Controls.AddRange(controls);
        return row;
    }

    private static void AddSetting(TableLayoutPanel table, string area, params Control[] controls)
    {
        var label = new Label { Text = area, AutoSize = true, Margin = new Padding(0, 8, 12, 0), Font = new Font(table.Font, FontStyle.Bold) };
        table.Controls.Add(label);
        table.Controls.Add(Row(controls));
    }
}
