namespace Clip2Table.Customize;

/// <summary>A small modal prompt for a layout name, validated before it closes.</summary>
internal sealed class NamePrompt : Form
{
    private readonly TextBox nameBox = new() { Width = 300, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label error = new() { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(300, 0) };
    private readonly Func<string, string?> validate;

    private NamePrompt(string title, string initial, Func<string, string?> validate)
    {
        this.validate = validate;
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        var ok = new Button { Text = "OK", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        ok.Click += OnOk;
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        buttons.Controls.AddRange([cancel, ok]);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Name:", AutoSize = true });
        layout.Controls.Add(nameBox);
        layout.Controls.Add(error);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        nameBox.Text = initial;
        nameBox.SelectAll();
    }

    /// <summary>The trimmed name, or null when cancelled. <paramref name="validate"/> returns an error message or null.</summary>
    public static string? Ask(IWin32Window owner, string title, string initial, Func<string, string?> validate)
    {
        using var prompt = new NamePrompt(title, initial, validate);
        return prompt.ShowDialog(owner) == DialogResult.OK ? prompt.nameBox.Text.Trim() : null;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        var message = validate(nameBox.Text);
        if (message is null)
        {
            DialogResult = DialogResult.OK;
            return;
        }

        error.Text = message;
        nameBox.Focus();
        nameBox.SelectAll();
    }
}
