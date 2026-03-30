using System.Text;

namespace TimeManager;

public class WeeklyReportForm : Form
{
    private readonly DateTimePicker _weekPicker;
    private readonly DataGridView _grid;
    private readonly Label _totalLabel;
    private readonly Button _btnSave;
    private List<TimeEntry> _entries = new();

    public WeeklyReportForm()
    {
        Text = "Weekly Report";
        Size = new Size(900, 520);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterParent;

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 8, 8, 0),
            FlowDirection = FlowDirection.LeftToRight
        };

        topPanel.Controls.Add(new Label { Text = "Week starting:", AutoSize = true, Margin = new Padding(0, 4, 4, 0) });

        _weekPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 120,
            Value = GetMonday(DateTime.Today)
        };
        _weekPicker.ValueChanged += (_, _) => LoadData();
        topPanel.Controls.Add(_weekPicker);

        var btnLoad = new Button { Text = "Load", Width = 70, Margin = new Padding(8, 0, 0, 0) };
        btnLoad.Click += (_, _) => LoadData();
        topPanel.Controls.Add(btnLoad);

        _btnSave = MakeSaveButton();
        _btnSave.Click += OnSave;
        topPanel.Controls.Add(_btnSave);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            BackgroundColor = SystemColors.Window,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",        HeaderText = "Date",        FillWeight = 12, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 44, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category",    HeaderText = "Category",    FillWeight = 10, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Activity",    HeaderText = "Activity",    FillWeight = 14, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Duration",
            HeaderText = "Duration (HH:mm:ss)",
            FillWeight = 14,
            ReadOnly = false,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Consolas", 9),
                ForeColor = Color.DarkBlue
            }
        });

        _grid.Columns["Duration"]!.ToolTipText = "Click to edit — format HH:mm:ss, then click Save changes";
        _grid.CellValueChanged += (_, _) => _btnSave.Enabled = true;

        _totalLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 12, 0),
            Font = new Font(Font, FontStyle.Bold)
        };

        Controls.Add(_grid);
        Controls.Add(_totalLabel);
        Controls.Add(topPanel);

        LoadData();
    }

    private void LoadData()
    {
        // Commit any active edit before clearing rows
        _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        _grid.CellValueChanged -= OnCellValueChangedMarkDirty;
        _grid.Rows.Clear();

        var weekStart = GetMonday(_weekPicker.Value);
        _entries = TimeEntryStore.GetWeek(weekStart);

        foreach (var e in _entries)
        {
            var rowIndex = _grid.Rows.Add();
            var row = _grid.Rows[rowIndex];
            row.Tag = e.Id;
            row.Cells["Date"].Value        = e.Date.ToString("ddd dd/MM");
            row.Cells["Category"].Value    = e.Category.ToString();
            row.Cells["Activity"].Value    = e.Activity.ToString();
            row.Cells["Description"].Value = e.Description;
            row.Cells["Duration"].Value    = FormatDuration(e.Duration);
        }

        _grid.CellValueChanged += OnCellValueChangedMarkDirty;
        _btnSave.Enabled = false;
        RefreshTotal();
    }

    private void OnCellValueChangedMarkDirty(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Duration")
            _btnSave.Enabled = true;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        // Commit any cell still open in the editor
        _grid.EndEdit();

        var errors = new List<string>();

        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            var row = _grid.Rows[i];
            var input = row.Cells["Duration"].Value?.ToString() ?? string.Empty;

            if (!TryParseDuration(input, out var newDuration))
            {
                errors.Add($"Row {i + 1}: \"{input}\" is not a valid duration (expected HH:mm:ss).");
                row.ErrorText = "Format must be HH:mm:ss  (e.g. 01:30:00)";
                continue;
            }

            row.ErrorText = string.Empty;
            row.Cells["Duration"].Value = FormatDuration(newDuration);

            var rowId = (Guid)row.Tag!;
            var entry = _entries.FirstOrDefault(x => x.Id == rowId);
            if (entry is null) continue;

            entry.Duration = newDuration;
            TimeEntryStore.Update(entry);
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors),
                "Some durations could not be saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSave.Enabled = false;
        RefreshTotal();
    }

    private void RefreshTotal()
    {
        var total = _entries.Aggregate(TimeSpan.Zero, (acc, e) => acc + e.Duration);
        _totalLabel.Text = $"Total: {FormatDuration(total)}";
    }

    private static bool TryParseDuration(string input, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var parts = input.Split(':');
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out int h) ||
            !int.TryParse(parts[1], out int m) ||
            !int.TryParse(parts[2], out int s)) return false;
        if (m < 0 || m > 59 || s < 0 || s > 59 || h < 0) return false;
        result = new TimeSpan(h, m, s);
        return true;
    }

    private static string FormatDuration(TimeSpan t)
        => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

    private static DateTime GetMonday(DateTime date)
    {
        int diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff).Date;
    }

    // ?? Owner-drawn Save button ???????????????????????????????????????????????

    private static Button MakeSaveButton()
    {
        var btn = new Button
        {
            Width = 130,
            Height = 28,
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = SystemColors.Control,
            Margin = new Padding(8, 0, 0, 0)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        btn.Paint += OnSaveButtonPaint;
        return btn;
    }

    private static void OnSaveButtonPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Button btn) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        bool enabled = btn.Enabled;
        var iconColor = enabled ? Color.FromArgb(0, 100, 200) : Color.FromArgb(160, 160, 160);
        var textColor = enabled ? Color.FromArgb(50, 50, 50) : Color.FromArgb(160, 160, 160);

        // Draw floppy-disk icon
        const int iconW = 16;
        const int pad = 6;
        int iy = (btn.Height - iconW) / 2;
        var r = new Rectangle(pad, iy, iconW, iconW);

        using var brush = new SolidBrush(iconColor);
        using var pen = new Pen(iconColor, 1.5f);
        using var whiteBrush = new SolidBrush(enabled ? Color.White : Color.FromArgb(220, 220, 220));

        // Outer body
        g.FillRectangle(brush, r);
        // Label window (top)
        g.FillRectangle(whiteBrush, r.Left + 2, r.Top + 2, r.Width - 4, 5);
        // Shutter slot
        g.FillRectangle(brush, r.Left + r.Width - 5, r.Top + 2, 3, 5);
        // Read window (bottom centre)
        g.FillRectangle(whiteBrush, r.Left + 3, r.Bottom - 6, r.Width - 6, 4);

        // Label text
        using var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        var textRect = new Rectangle(pad + iconW + 4, 0, btn.Width - pad - iconW - 6, btn.Height);
        using var textBrush = new SolidBrush(textColor);
        g.DrawString("Save changes", btn.Font, textBrush, textRect, sf);
    }
}
