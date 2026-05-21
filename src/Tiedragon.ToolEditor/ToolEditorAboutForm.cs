#nullable enable
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Tiedragon.ToolEditor;

internal sealed class ToolEditorAboutForm : Form
{
    private const string Website = "https://www.tiedragon.com";
    private const string Email = "info@tiedragon.com";

    public ToolEditorAboutForm()
    {
        Text = "About Syscalculator 2.0";
        ClientSize = new Size(980, 560);
        MinimumSize = ClientSize;
        MaximumSize = ClientSize;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(246, 248, 252);

        BuildContent();
    }

    private void BuildContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(246, 248, 252),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(BuildInfoPanel(), 1, 0);

        var bottom = BuildBottomBar();
        root.SetColumnSpan(bottom, 2);
        root.Controls.Add(bottom, 0, 1);

        Controls.Add(root);
    }

    private static Control BuildHero()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.White,
        };
        panel.Paint += (_, e) =>
        {
            using var border = new Pen(Color.FromArgb(103, 158, 216));
            e.Graphics.DrawRectangle(border, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        var title = new Label
        {
            AutoSize = false,
            Location = new Point(30, 46),
            Size = new Size(360, 72),
            Text = "Syscalculator 2.0",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
        };

        var subtitle = new Label
        {
            AutoSize = false,
            Location = new Point(34, 120),
            Size = new Size(350, 84),
            Text = "NOD conversion, Graph2D, Graph3D and Language Package workflow.",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.FromArgb(48, 61, 79),
        };

        var badge = new Label
        {
            AutoSize = false,
            Location = new Point(34, 250),
            Size = new Size(350, 92),
            Padding = new Padding(16),
            Text = "ToolEditor is onderdeel van de Syscalculator language-package workflow.",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
            BackColor = Color.FromArgb(232, 241, 252),
        };

        var note = new Label
        {
            AutoSize = false,
            Location = new Point(34, 366),
            Size = new Size(350, 72),
            Text = ".lngpdk packages met HTML-help, media, SHA-256 checks en release-gates.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(72, 84, 102),
        };

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(badge);
        panel.Controls.Add(note);
        return panel;
    }

    private Control BuildInfoPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(0),
            BackColor = Color.White,
        };
        panel.Paint += (_, e) =>
        {
            using var border = new Pen(Color.FromArgb(103, 158, 216));
            e.Graphics.DrawRectangle(border, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(CreateVersionSection(), 0, 0);
        panel.Controls.Add(CreateTextSection("Copyright", "1995-2026 Edward Tie / Tiedragon\r\nAll rights reserved."), 0, 1);
        panel.Controls.Add(CreateTextSection("Description", "Syscalculator is een conversie- en rekensysteem met NOD, graph modules, help tooling en language packages."), 0, 2);
        panel.Controls.Add(CreateContactSection(), 0, 3);
        return panel;
    }

    private static Control CreateVersionSection()
    {
        var grid = CreateSection("Version information");
        AddPair(grid, "Product name:", "Syscalculator 2.0");
        AddPair(grid, "Tool:", "Tiedragon ToolEditor");
        AddPair(grid, "Tool version:", GetToolVersion());
        AddPair(grid, "Runtime:", ".NET " + Environment.Version);
        return grid;
    }

    private static Control CreateTextSection(string title, string text)
    {
        var grid = CreateSection(title);
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = text,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(39, 51, 69),
            Padding = new Padding(12, 4, 12, 6),
        };
        grid.SetColumnSpan(label, 2);
        grid.Controls.Add(label, 0, row);
        return grid;
    }

    private static Control CreateContactSection()
    {
        var grid = CreateSection("Website");
        AddLink(grid, Website, Website);
        AddLink(grid, Email, "mailto:" + Email);
        return grid;
    }

    private static TableLayoutPanel CreateSection(string title)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            Padding = new Padding(12, 10, 12, 8),
            BackColor = Color.White,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
        };
        grid.SetColumnSpan(header, 2);
        grid.Controls.Add(header, 0, 0);
        return grid;
    }

    private static void AddPair(TableLayoutPanel grid, string key, string value)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        grid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = key,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 51, 69),
        }, 0, row);
        grid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = value,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(39, 51, 69),
        }, 1, row);
    }

    private static void AddLink(TableLayoutPanel grid, string text, string link)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var label = new LinkLabel
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            LinkColor = Color.FromArgb(0, 80, 180),
            ActiveLinkColor = Color.FromArgb(0, 50, 130),
        };
        label.LinkClicked += (_, _) => OpenLink(link);
        grid.SetColumnSpan(label, 2);
        grid.Controls.Add(label, 0, row);
    }

    private static Control BuildBottomBar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(238, 242, 248),
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Size = new Size(92, 30),
            Location = new Point(864, 14),
        };
        panel.Controls.Add(ok);
        return panel;
    }

    private static string GetToolVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.1.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void OpenLink(string link)
    {
        try
        {
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            MessageBox.Show(link, "Link openen", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
