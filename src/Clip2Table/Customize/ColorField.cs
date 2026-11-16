using System.ComponentModel;
using System.Globalization;
using Clip2Table.Core.Styling;

namespace Clip2Table.Customize;

/// <summary>A hex text box followed by a colour swatch that opens the colour picker when clicked.</summary>
internal sealed class ColorField : UserControl
{
    private static readonly Color InvalidBack = Color.FromArgb(0xff, 0xd6, 0xd6);

    private readonly TextBox text = new() { Width = 70, Margin = new Padding(0, 1, 4, 0) };
    private readonly Button swatch = new()
    {
        Size = new Size(22, 22),
        Margin = new Padding(0, 1, 0, 0),
        FlatStyle = FlatStyle.Flat,
        UseVisualStyleBackColor = false,
        Cursor = Cursors.Hand,
        FlatAppearance = { BorderColor = SystemColors.ControlDark, BorderSize = 1 },
    };

    private string value = "#000000";
    private bool suppress;

    public ColorField()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        row.Controls.AddRange([text, swatch]);
        Controls.Add(row);

        text.TextChanged += OnTextChanged;
        text.Leave += (_, _) =>
        {
            if (HexColor.TryNormalize(text.Text, out var normalized) && normalized != text.Text)
            {
                SetTextSilently(normalized);
            }
        };
        swatch.Click += OnPick;
    }

    public event EventHandler? ValueChanged;

    /// <summary>The last valid colour, lowercase <c>#rrggbb</c>. Setting it never raises <see cref="ValueChanged"/>.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value
    {
        get => value;
        set
        {
            this.value = value;
            IsValid = true;
            SetTextSilently(value);
            ShowValidity();
        }
    }

    public bool IsValid { get; private set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string AccessibleLabel
    {
        set
        {
            text.AccessibleName = value;
            swatch.AccessibleName = $"{value}: choose colour";
        }
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (suppress)
        {
            return;
        }

        IsValid = HexColor.TryNormalize(text.Text, out var normalized);
        if (IsValid)
        {
            value = normalized;
        }

        ShowValidity();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPick(object? sender, EventArgs e)
    {
        var (r, g, b) = (Channel(1), Channel(3), Channel(5));
        using var dialog = new ColorDialog { FullOpen = true, AnyColor = true, Color = Color.FromArgb(r, g, b) };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
        {
            var c = dialog.Color;
            value = $"#{c.R:x2}{c.G:x2}{c.B:x2}";
            IsValid = true;
            SetTextSilently(value);
            ShowValidity();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int Channel(int start) => int.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private void SetTextSilently(string s)
    {
        suppress = true;
        try
        {
            text.Text = s;
        }
        finally
        {
            suppress = false;
        }
    }

    private void ShowValidity()
    {
        text.BackColor = IsValid ? SystemColors.Window : InvalidBack;
        var color = Color.FromArgb(Channel(1), Channel(3), Channel(5));
        swatch.BackColor = color;
        swatch.FlatAppearance.MouseOverBackColor = color;
        swatch.FlatAppearance.MouseDownBackColor = color;
    }
}
