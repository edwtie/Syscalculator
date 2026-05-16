#nullable enable
using System.Globalization;

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Excel-like grid UI helpers for clipboard text.
/// </summary>
public static class ClipboardGridApi
{
    public static DataGridView CreateExcelLikeGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            ColumnHeadersHeight = 26,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            EditMode = DataGridViewEditMode.EditProgrammatically,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(210, 214, 220),
            ReadOnly = true,
            RowHeadersWidth = 46,
            RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = true,
            Font = new Font("Segoe UI", 8.5f)
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
        grid.RowHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
        grid.CellPainting += PaintRowHeaderWithoutSelectorArrow;
        return grid;
    }

    public static void FillExcelLikeGrid(
        DataGridView grid,
        string text,
        int minimumRows = 24,
        int minimumColumns = 10,
        int? maximumRows = null)
    {
        grid.SuspendLayout();
        try
        {
            grid.Columns.Clear();
            grid.Rows.Clear();

            var rows = ParseTable(text);
            var visibleRows = maximumRows.HasValue
                ? rows.Take(Math.Max(0, maximumRows.Value)).ToList()
                : rows;
            var dataColumnCount = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
            var columnCount = Math.Max(minimumColumns, dataColumnCount);

            for (var i = 0; i < columnCount; i++)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = ColumnName(i),
                    Width = 96,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }

            var rowCount = Math.Max(minimumRows, visibleRows.Count);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var gridRowIndex = grid.Rows.Add();
                grid.Rows[gridRowIndex].HeaderCell.Value = (gridRowIndex + 1).ToString(CultureInfo.InvariantCulture);

                if (rowIndex >= visibleRows.Count)
                    continue;

                var row = visibleRows[rowIndex];
                for (var column = 0; column < row.Count && column < columnCount; column++)
                    grid.Rows[gridRowIndex].Cells[column].Value = row[column];
            }

            if (grid.Rows.Count > 0 && grid.Columns.Count > 0)
                grid.CurrentCell = grid.Rows[0].Cells[0];
        }
        finally
        {
            grid.ResumeLayout();
        }
    }

    public static List<List<string>> ParseTable(string text)
    {
        var result = new List<List<string>>();
        if (string.IsNullOrEmpty(text))
            return result;

        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n').ToList();
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        foreach (var line in lines)
            result.Add(line.Split('\t').ToList());

        return result;
    }

    public static string ColumnName(int index)
    {
        var name = "";
        index++;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }

        return name;
    }

    private static void PaintRowHeaderWithoutSelectorArrow(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (sender is not DataGridView grid || e.Graphics is null || e.ColumnIndex != -1 || e.RowIndex < 0)
            return;

        var graphics = e.Graphics;
        using var background = new SolidBrush(Color.FromArgb(245, 247, 250));
        using var border = new Pen(grid.GridColor);
        graphics.FillRectangle(background, e.CellBounds);
        graphics.DrawRectangle(border, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);

        var text = grid.Rows[e.RowIndex].HeaderCell.Value?.ToString()
            ?? (e.RowIndex + 1).ToString(CultureInfo.InvariantCulture);
        TextRenderer.DrawText(
            graphics,
            text,
            grid.RowHeadersDefaultCellStyle.Font ?? grid.Font,
            e.CellBounds,
            Color.FromArgb(31, 41, 55),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        e.Handled = true;
    }
}
