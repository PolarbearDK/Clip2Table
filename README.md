# Clip2Table

Clip2Table is a small Windows tray app that turns tables copied as plain text into formatted HTML
tables. Copy a Markdown table from a chat window, query output from a terminal, or tab-separated
data from anywhere. When you paste it into Outlook, Word, Teams or another rich-text editor, you get
a styled table instead of misaligned text.

## Features

- **Detects three text table formats**
  - **Tab-separated text**, such as results grid from SQL Server Management Studio (SSMS) or rows copied from a spreadsheet.
  - **Markdown pipe tables**, including `:---:` column alignment. Inline formatting such as `**bold**`,
    `_emphasis_`, `` `code` ``, `~~strike~~` and `[links](…)` is removed from the cell text.
  - **Box-drawing tables** (`┌─┬─┐ │ │ └─┴─┘`), such as those printed by CLI tools and database
    shells. Cells that wrap over several lines are joined back together.
- **Two ways to convert**
  - **Convert now** (tray menu) converts whatever is on the clipboard and accepts ragged rows.
  - **Auto-convert** watches the clipboard and converts tables as you copy them. It uses stricter
    rules so it never touches ordinary text: it skips content that already has an HTML table,
    content copied from Word or Outlook, and clipboard text over 1 MB.
- **Paste-friendly output.** The HTML uses inline CSS only, so it survives pasting into email.
  Numeric columns are right-aligned and `NULL` cells are muted. The plain-text version stays on the
  clipboard next to the HTML, so editors that only accept text still get the original.
- **Custom layouts.** Choose the font, size, colours, header style, striped rows and cell padding,
  with a live preview of what is currently on your clipboard. Warnings point out colour combinations
  that will be hard to read, including in new Outlook, which strips text colours when you paste.
- **Shareable layouts.** Export layouts to a `.json` file and import them on another machine.
  Layouts you already have are skipped on import.
- **Start with Windows**, available as a tray menu toggle.

## Requirements

- Windows 10 or 11 (x64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/), used
  for the layout preview. It is already installed on current Windows versions.

## Usage

1. Start `Clip2Table.exe`. An icon appears in the notification area. Only one instance runs at a time.
2. Copy a table as text.
3. Either choose **Convert now** from the tray menu, or turn on **Auto-convert** and do nothing.
4. Paste into Outlook, Word, Teams, OneNote or another rich-text editor.

The tray menu has the following items:

| Item               | What it does                                                        |
|--------------------|---------------------------------------------------------------------|
| Convert now        | Converts the current clipboard contents and shows a notice if no table is found. |
| Auto-convert       | Turns automatic conversion on or off. The icon shows the current state.          |
| Layout             | Selects the active layout. **Customize…** opens the layout editor.               |
| Start with Windows | Adds Clip2Table to, or removes it from, your Windows sign-in startup items.      |
| Exit               | Closes the app.                                                                  |

### Example

Copying this text:

```
| Region | Orders | Revenue |
|:-------|-------:|--------:|
| North  |    120 | 4310.50 |
| South  |     87 | 2975.00 |
```

and pasting it into Outlook produces a table with a header row, borders, striped rows and
right-aligned numbers, styled with the active layout.

## Settings

Settings and custom layouts are stored in `%APPDATA%\Clip2Table\settings.json`. If the file cannot
be parsed, it is copied to `settings.json.bak` before it is ever overwritten.

The built-in layout, *Compact, light blue header*, cannot be edited, but you can rename it. Use
**Save as…** in the layout editor to start a custom layout from it.

## Building from source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet build
dotnet test
```

To produce a single-file executable:

```powershell
dotnet publish src/Clip2Table -c Release
```

The output is written to `src/Clip2Table/bin/Release/net10.0-windows/win-x64/publish/`.

Version numbers come from [GitVersion](https://gitversion.net/) and the git history, so build from
a git clone rather than from a source archive.

### Project layout

| Path                         | Contents                                                        |
|------------------------------|-----------------------------------------------------------------|
| `src/Clip2Table`             | The WinForms tray app: clipboard monitoring, tray menu and layout editor. |
| `src/Clip2Table.Core`        | Platform-independent parsing, HTML rendering, styling and settings.       |
| `tests/Clip2Table.Core.Tests`| xUnit tests for the core library.                                         |

## License

[MIT](LICENSE) © 2026 Philip Hoppe
