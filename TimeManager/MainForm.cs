namespace TimeManager;

public class MainForm : Form
{
    private readonly TextBox _descriptionBox;
    private readonly ComboBox _categoryCombo;
    private readonly ComboBox _activityCombo;
    private readonly Button _runButton;
    private readonly Button _pauseButton;
    private readonly Button _stopButton;
    private readonly Button _reportButton;
    private readonly Label _timerLabel;
    private readonly CheckBox _startupCheck;

    private readonly System.Windows.Forms.Timer _ticker;
    private TimeSpan _elapsed;
    private DateTime _segmentStart;
    private bool _running;

    public MainForm()
    {
        Text = "Time Manager";
        Size = new Size(460, 340);
        MinimumSize = new Size(420, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Description
        layout.Controls.Add(MakeLabel("Description:"), 0, 0);
        _descriptionBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
        layout.SetRowSpan(_descriptionBox, 2);
        layout.Controls.Add(_descriptionBox, 1, 0);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        // Category
        layout.Controls.Add(MakeLabel("Category:"), 0, 2);
        _categoryCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _categoryCombo.Items.AddRange(Enum.GetNames<Category>());
        _categoryCombo.SelectedIndex = 0;
        layout.Controls.Add(_categoryCombo, 1, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        // Activity
        layout.Controls.Add(MakeLabel("Activity:"), 0, 3);
        _activityCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _activityCombo.Items.AddRange(Enum.GetNames<Activity>());
        _activityCombo.SelectedIndex = 0;
        layout.Controls.Add(_activityCombo, 1, 3);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        // Timer display
        _timerLabel = new Label
        {
            Text = "00:00:00",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        layout.SetColumnSpan(_timerLabel, 2);
        layout.Controls.Add(_timerLabel, 0, 4);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };

        _runButton = MakeIconButton(DrawPlay, "Run", 90);
        _pauseButton = MakeIconButton(DrawPause, "Pause", 90);
        _pauseButton.Enabled = false;
        _stopButton = MakeIconButton(DrawStop, "Stop", 90);
        _stopButton.Enabled = false;
        _reportButton = MakeIconButton(DrawReport, "Report", 100);

        _runButton.Click += OnRun;
        _pauseButton.Click += OnPause;
        _stopButton.Click += OnStop;
        _reportButton.Click += (_, _) => new WeeklyReportForm().ShowDialog(this);

        _startupCheck = new CheckBox
        {
            Text = "Run at startup",
            AutoSize = true,
            Checked = StartupHelper.IsEnabled(),
            Margin = new Padding(12, 8, 0, 0)
        };
        _startupCheck.CheckedChanged += (_, _) =>
        {
            if (_startupCheck.Checked) StartupHelper.Enable();
            else StartupHelper.Disable();
        };

        buttonPanel.Controls.AddRange(new Control[] { _runButton, _pauseButton, _stopButton, _reportButton, _startupCheck });

        layout.SetColumnSpan(buttonPanel, 2);
        layout.Controls.Add(buttonPanel, 0, 5);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(layout);

        _ticker = new System.Windows.Forms.Timer { Interval = 1000 };
        _ticker.Tick += (_, _) =>
        {
            _timerLabel.Text = FormatDuration(_elapsed + (DateTime.Now - _segmentStart));
        };
    }

    private void OnRun(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_descriptionBox.Text))
        {
            MessageBox.Show("Please enter a description before starting.", "Missing Description",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _segmentStart = DateTime.Now;
        _running = true;
        _ticker.Start();

        _runButton.Enabled = false;
        _pauseButton.Enabled = true;
        _stopButton.Enabled = true;
    }

    private void OnPause(object? sender, EventArgs e)
    {
        if (_running)
        {
            _elapsed += DateTime.Now - _segmentStart;
            _ticker.Stop();
            _running = false;
            SetButtonIcon(_runButton, DrawPlay, "Resume");
            _runButton.Enabled = true;
            _pauseButton.Enabled = false;
        }
    }

    private void OnStop(object? sender, EventArgs e)
    {
        if (_running)
        {
            _elapsed += DateTime.Now - _segmentStart;
            _ticker.Stop();
            _running = false;
        }

        var finalDuration = _elapsed;
        if (finalDuration.TotalSeconds < 1)
        {
            ResetForm();
            return;
        }

        var entry = new TimeEntry
        {
            Date = DateTime.Now,
            Description = _descriptionBox.Text.Trim(),
            Category = Enum.Parse<Category>(_categoryCombo.SelectedItem!.ToString()!),
            Activity = Enum.Parse<Activity>(_activityCombo.SelectedItem!.ToString()!),
            Duration = finalDuration
        };

        TimeEntryStore.Save(entry);

        MessageBox.Show(
            $"Saved!\n\nDuration: {FormatDuration(finalDuration)}\nCategory: {entry.Category}\nActivity: {entry.Activity}",
            "Entry Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

        ResetForm();
    }

    private void ResetForm()
    {
        _elapsed = TimeSpan.Zero;
        _timerLabel.Text = "00:00:00";
        _descriptionBox.Clear();
        _categoryCombo.SelectedIndex = 0;
        _activityCombo.SelectedIndex = 0;
        SetButtonIcon(_runButton, DrawPlay, "Run");
        _runButton.Enabled = true;
        _pauseButton.Enabled = false;
        _stopButton.Enabled = false;
    }

    private static string FormatDuration(TimeSpan t)
        => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

    private static Label MakeLabel(string text) =>
        new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

    // ?? Owner-drawn button helpers ????????????????????????????????????????????

    private static Button MakeIconButton(Action<Graphics, Rectangle, bool> drawIcon, string label, int width)
    {
        var btn = new Button
        {
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = SystemColors.Control,
            Tag = (drawIcon, label)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
        btn.Paint += OnButtonPaint;
        return btn;
    }

    private static void SetButtonIcon(Button btn, Action<Graphics, Rectangle, bool> drawIcon, string label)
    {
        btn.Tag = (drawIcon, label);
        btn.Invalidate();
    }

    private static void OnButtonPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Button btn) return;
        var (drawIcon, label) = ((Action<Graphics, Rectangle, bool>, string))btn.Tag!;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        bool enabled = btn.Enabled;
        var iconColor = enabled ? Color.FromArgb(50, 50, 50) : Color.FromArgb(160, 160, 160);

        // Icon occupies the left portion, text the right
        const int iconW = 20;
        const int padding = 6;
        var iconRect = new Rectangle(padding, (btn.Height - iconW) / 2, iconW, iconW);

        drawIcon(g, iconRect, enabled);

        using var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        var textRect = new Rectangle(iconW + padding * 2, 0, btn.Width - iconW - padding * 3, btn.Height);
        using var brush = new SolidBrush(iconColor);
        g.DrawString(label, btn.Font, brush, textRect, sf);
    }

    // ?? Icon painters ?????????????????????????????????????????????????????????

    private static void DrawPlay(Graphics g, Rectangle r, bool enabled)
    {
        var color = enabled ? Color.FromArgb(0, 140, 0) : Color.FromArgb(160, 160, 160);
        using var brush = new SolidBrush(color);
        var pts = new PointF[]
        {
            new(r.Left,  r.Top),
            new(r.Right, r.Top + r.Height / 2f),
            new(r.Left,  r.Bottom)
        };
        g.FillPolygon(brush, pts);
    }

    private static void DrawPause(Graphics g, Rectangle r, bool enabled)
    {
        var color = enabled ? Color.FromArgb(200, 130, 0) : Color.FromArgb(160, 160, 160);
        using var brush = new SolidBrush(color);
        int barW = Math.Max(2, r.Width / 3);
        g.FillRectangle(brush, r.Left, r.Top, barW, r.Height);
        g.FillRectangle(brush, r.Right - barW, r.Top, barW, r.Height);
    }

    private static void DrawStop(Graphics g, Rectangle r, bool enabled)
    {
        var color = enabled ? Color.FromArgb(190, 0, 0) : Color.FromArgb(160, 160, 160);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, r);
    }

    private static void DrawReport(Graphics g, Rectangle r, bool enabled)
    {
        var color = enabled ? Color.FromArgb(0, 100, 200) : Color.FromArgb(160, 160, 160);
        using var pen = new Pen(color, 1.5f);
        // Clipboard outline
        int cw = r.Width - 2, ch = r.Height - 2;
        g.DrawRectangle(pen, r.Left + 1, r.Top + 3, cw, ch - 3);
        // Three horizontal lines
        int lx1 = r.Left + 3, lx2 = r.Right - 1;
        int gap = ch / 4;
        for (int i = 1; i <= 3; i++)
            g.DrawLine(pen, lx1, r.Top + 3 + gap * i, lx2, r.Top + 3 + gap * i);
        // Clip tab at top
        g.DrawRectangle(pen, r.Left + cw / 3, r.Top, cw / 3, 4);
    }
}
