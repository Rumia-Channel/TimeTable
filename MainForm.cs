using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TimeTableApp
{
    public sealed class MainForm : Form
    {
        private readonly TextBox _stationBox;
        private readonly TextBox _subtitleBox;
        private readonly TextBox _serviceBox;
        private readonly NumericUpDown _yearBox;
        private readonly NumericUpDown _monthBox;
        private readonly NumericUpDown _dayBox;
        private readonly TextBox _platform3Box;
        private readonly TextBox _platform4Box;
        private readonly NumericUpDown _widthBox;
        private readonly NumericUpDown _heightBox;
        private readonly CheckBox _bottomVerticalCheck;
        private readonly Label _statusLabel;

        private readonly DataGridView _topInfoGrid;
        private readonly DataGridView _topTimeGrid;
        private readonly DataGridView _midInfoGrid;
        private readonly DataGridView _midTimeGrid;
        private readonly DataGridView _bottomInfoGrid;
        private readonly DataGridView _bottomTimeGrid;

        public MainForm()
        {
            Text = "TimeTable Editor (.NET Framework 4.8)";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 700);
            Width = 1320;
            Height = 820;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Top;
            actionPanel.Padding = new Padding(8, 8, 8, 0);
            actionPanel.AutoSize = true;
            actionPanel.WrapContents = false;

            var saveButton = new Button();
            saveButton.Text = "Save Timetable PNG";
            saveButton.AutoSize = true;
            saveButton.Click += SaveTimetableButton_Click;

            var resetButton = new Button();
            resetButton.Text = "Reset Sample";
            resetButton.AutoSize = true;
            resetButton.Click += ResetButton_Click;

            _statusLabel = new Label();
            _statusLabel.AutoSize = true;
            _statusLabel.Padding = new Padding(12, 7, 0, 0);
            _statusLabel.Text = "Edit timetable data and export image.";

            actionPanel.Controls.Add(saveButton);
            actionPanel.Controls.Add(resetButton);
            actionPanel.Controls.Add(_statusLabel);

            var headPanel = new FlowLayoutPanel();
            headPanel.Dock = DockStyle.Top;
            headPanel.Padding = new Padding(8, 6, 8, 6);
            headPanel.AutoSize = true;
            headPanel.WrapContents = true;

            _stationBox = CreateTextBox(80);
            _subtitleBox = CreateTextBox(520);
            _serviceBox = CreateTextBox(110);
            _yearBox = CreateDatePartBox(2000, 2100, 2025, 70);
            _monthBox = CreateDatePartBox(1, 12, 3, 45);
            _dayBox = CreateDatePartBox(1, 31, 15, 45);
            _platform3Box = CreateTextBox(90);
            _platform4Box = CreateTextBox(90);

            var revisedDatePanel = new FlowLayoutPanel();
            revisedDatePanel.AutoSize = true;
            revisedDatePanel.WrapContents = false;
            revisedDatePanel.Margin = new Padding(0);
            revisedDatePanel.Padding = new Padding(0);
            revisedDatePanel.Controls.Add(_yearBox);
            revisedDatePanel.Controls.Add(CreateInlineLabel("年"));
            revisedDatePanel.Controls.Add(_monthBox);
            revisedDatePanel.Controls.Add(CreateInlineLabel("月"));
            revisedDatePanel.Controls.Add(_dayBox);
            revisedDatePanel.Controls.Add(CreateInlineLabel("日"));

            _widthBox = new NumericUpDown();
            _widthBox.Minimum = 300;
            _widthBox.Maximum = 4000;
            _widthBox.Value = 2480;
            _widthBox.Width = 70;

            _heightBox = new NumericUpDown();
            _heightBox.Minimum = 300;
            _heightBox.Maximum = 4000;
            _heightBox.Value = 1748;
            _heightBox.Width = 70;

            _bottomVerticalCheck = new CheckBox();
            _bottomVerticalCheck.AutoSize = true;
            _bottomVerticalCheck.Text = "下段縦線";

            AddLabeled(headPanel, "駅名", _stationBox);
            AddLabeled(headPanel, "副題", _subtitleBox);
            AddLabeled(headPanel, "黒帯", _serviceBox);
            AddLabeled(headPanel, "改正日", revisedDatePanel);
            AddLabeled(headPanel, "時刻欄ホーム", _platform3Box);
            AddLabeled(headPanel, "情報欄ホーム", _platform4Box);
            AddLabeled(headPanel, "幅", _widthBox);
            AddLabeled(headPanel, "高", _heightBox);
            headPanel.Controls.Add(_bottomVerticalCheck);

            var tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            _topInfoGrid = CreateInfoGrid(10);
            _topTimeGrid = CreateTimeGrid(10);
            _midInfoGrid = CreateInfoGrid(10);
            _midTimeGrid = CreateTimeGrid(10);
            _bottomInfoGrid = CreateInfoGrid(6);
            _bottomTimeGrid = CreateTimeGrid(6);

            AddGroupTab(
                tabs,
                "4番ホーム (Info)",
                "Top Info",
                _topInfoGrid,
                "Mid Info",
                _midInfoGrid,
                "Bottom Info",
                _bottomInfoGrid);
            AddGroupTab(
                tabs,
                "3番ホーム (Time)",
                "Top Time",
                _topTimeGrid,
                "Mid Time",
                _midTimeGrid,
                "Bottom Time",
                _bottomTimeGrid);

            root.Controls.Add(actionPanel, 0, 0);
            root.Controls.Add(headPanel, 0, 1);
            root.Controls.Add(tabs, 0, 2);
            Controls.Add(root);

            LoadDataToUi(TimetableData.CreateDefault());
        }

        private void SaveTimetableButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG image (*.png)|*.png|JPEG image (*.jpg)|*.jpg|Bitmap image (*.bmp)|*.bmp|All files (*.*)|*.*";
                dialog.FileName = "timetable_layout.png";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                TimetableData data = BuildDataFromUi();
                using (var bitmap = TimetableRenderer.Render(data, (int)_widthBox.Value, (int)_heightBox.Value))
                {
                    bitmap.Save(dialog.FileName, GetImageFormatFromPath(dialog.FileName));
                }

                _statusLabel.Text = "Saved: " + dialog.FileName;
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            LoadDataToUi(TimetableData.CreateDefault());
            _statusLabel.Text = "Reset to sample.";
        }

        private TimetableData BuildDataFromUi()
        {
            return new TimetableData
            {
                StationName = _stationBox.Text,
                SubTitle = _subtitleBox.Text,
                ServiceLabel = _serviceBox.Text,
                RevisedDate = FormatRevisedDate((int)_yearBox.Value, (int)_monthBox.Value, (int)_dayBox.Value),
                Platform3Label = _platform3Box.Text,
                Platform4Label = _platform4Box.Text,
                DrawBottomVerticalLines = _bottomVerticalCheck.Checked,
                TopLeftHour = "17時",
                TopOverlayColumn = 2,
                TopOverlayHour = "18時",
                MidLeftHour = string.Empty,
                MidOverlayColumn = 4,
                MidOverlayHour = "19時",
                TopInfo = ReadInfoRows(_topInfoGrid),
                TopTimes = ReadTimeRows(_topTimeGrid),
                MidInfo = ReadInfoRows(_midInfoGrid),
                MidTimes = ReadTimeRows(_midTimeGrid),
                BottomInfo = ReadInfoRows(_bottomInfoGrid),
                BottomTimes = ReadTimeRows(_bottomTimeGrid)
            };
        }

        private void LoadDataToUi(TimetableData data)
        {
            _stationBox.Text = data.StationName ?? string.Empty;
            _subtitleBox.Text = data.SubTitle ?? string.Empty;
            _serviceBox.Text = data.ServiceLabel ?? string.Empty;
            int year;
            int month;
            int day;
            ParseRevisedDate(data.RevisedDate, out year, out month, out day);
            _yearBox.Value = year;
            _monthBox.Value = month;
            _dayBox.Value = day;
            _platform3Box.Text = data.Platform3Label ?? string.Empty;
            _platform4Box.Text = data.Platform4Label ?? string.Empty;
            _bottomVerticalCheck.Checked = data.DrawBottomVerticalLines;

            WriteInfoRows(_topInfoGrid, data.TopInfo);
            WriteTimeRows(_topTimeGrid, data.TopTimes);
            WriteInfoRows(_midInfoGrid, data.MidInfo);
            WriteTimeRows(_midTimeGrid, data.MidTimes);
            WriteInfoRows(_bottomInfoGrid, data.BottomInfo);
            WriteTimeRows(_bottomTimeGrid, data.BottomTimes);
        }

        private static DataGridView CreateInfoGrid(int rows)
        {
            var grid = BaseGrid();
            grid.Columns.Add("Code", "Code");
            var colorCol = new DataGridViewComboBoxColumn();
            colorCol.Name = "CodeColor";
            colorCol.HeaderText = "CodeColor";
            colorCol.Items.AddRange("Orange", "Blue", "Black", "Red", "Green");
            colorCol.FlatStyle = FlatStyle.Flat;
            grid.Columns.Add(colorCol);
            grid.Columns.Add("TrainNo", "TrainNo");
            grid.Columns.Add("TrainType", "TrainType");
            grid.Columns.Add("Destination", "Destination");
            grid.Rows.Add(rows);
            return grid;
        }

        private static DataGridView CreateTimeGrid(int rows)
        {
            var grid = BaseGrid();
            grid.Columns.Add("Time4", "Time4");
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Highlight", HeaderText = "Highlight" });
            grid.Columns.Add("TrainNo", "TrainNo");
            grid.Columns.Add("TypeAndDestination", "TypeAndDestination");
            grid.Columns.Add("NoteBlue", "NoteBlue");
            grid.Columns.Add("NoteRed", "NoteRed");
            grid.Rows.Add(rows);
            return grid;
        }

        private static DataGridView BaseGrid()
        {
            var grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersWidth = 55;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            return grid;
        }

        private static void AddGroupTab(
            TabControl tabs,
            string tabTitle,
            string group1Title,
            Control group1Control,
            string group2Title,
            Control group2Control,
            string group3Title,
            Control group3Control)
        {
            var page = new TabPage(tabTitle);

            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 3;
            panel.ColumnCount = 1;
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334f));

            var group1 = new GroupBox();
            group1.Text = group1Title;
            group1.Dock = DockStyle.Fill;
            group1Control.Dock = DockStyle.Fill;
            group1.Controls.Add(group1Control);

            var group2 = new GroupBox();
            group2.Text = group2Title;
            group2.Dock = DockStyle.Fill;
            group2Control.Dock = DockStyle.Fill;
            group2.Controls.Add(group2Control);

            var group3 = new GroupBox();
            group3.Text = group3Title;
            group3.Dock = DockStyle.Fill;
            group3Control.Dock = DockStyle.Fill;
            group3.Controls.Add(group3Control);

            panel.Controls.Add(group1, 0, 0);
            panel.Controls.Add(group2, 0, 1);
            panel.Controls.Add(group3, 0, 2);
            page.Controls.Add(panel);
            tabs.TabPages.Add(page);
        }

        private static TextBox CreateTextBox(int width)
        {
            var box = new TextBox();
            box.Width = width;
            return box;
        }

        private static NumericUpDown CreateDatePartBox(int min, int max, int value, int width)
        {
            var box = new NumericUpDown();
            box.Minimum = min;
            box.Maximum = max;
            box.Value = value;
            box.Width = width;
            box.ThousandsSeparator = false;
            return box;
        }

        private static Label CreateInlineLabel(string text)
        {
            var label = new Label();
            label.AutoSize = true;
            label.Text = text;
            label.Padding = new Padding(3, 7, 6, 0);
            return label;
        }

        private static void AddLabeled(FlowLayoutPanel panel, string label, Control control)
        {
            var l = new Label();
            l.Text = label;
            l.AutoSize = true;
            l.Padding = new Padding(6, 7, 2, 0);
            panel.Controls.Add(l);
            panel.Controls.Add(control);
        }

        private static void WriteInfoRows(DataGridView grid, TimetableTrainInfoRow[] rows)
        {
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                TimetableTrainInfoRow row = (rows != null && i < rows.Length && rows[i] != null) ? rows[i] : new TimetableTrainInfoRow();
                grid.Rows[i].Cells[0].Value = row.Code ?? string.Empty;
                grid.Rows[i].Cells[1].Value = string.IsNullOrEmpty(row.CodeColor) ? "Orange" : row.CodeColor;
                grid.Rows[i].Cells[2].Value = row.TrainNo ?? string.Empty;
                grid.Rows[i].Cells[3].Value = row.TrainType ?? string.Empty;
                grid.Rows[i].Cells[4].Value = row.Destination ?? string.Empty;
            }
        }

        private static void WriteTimeRows(DataGridView grid, TimetableTimeRow[] rows)
        {
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                TimetableTimeRow row = (rows != null && i < rows.Length && rows[i] != null) ? rows[i] : new TimetableTimeRow();
                grid.Rows[i].Cells[0].Value = row.Time4 ?? string.Empty;
                grid.Rows[i].Cells[1].Value = row.Highlight;
                grid.Rows[i].Cells[2].Value = row.TrainNo ?? string.Empty;
                grid.Rows[i].Cells[3].Value = row.TypeAndDestination ?? string.Empty;
                grid.Rows[i].Cells[4].Value = row.NoteBlue ?? string.Empty;
                grid.Rows[i].Cells[5].Value = row.NoteRed ?? string.Empty;
            }
        }

        private static TimetableTrainInfoRow[] ReadInfoRows(DataGridView grid)
        {
            var rows = new TimetableTrainInfoRow[grid.Rows.Count];
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                DataGridViewRow r = grid.Rows[i];
                rows[i] = new TimetableTrainInfoRow
                {
                    Code = CellText(r, 0),
                    CodeColor = CellText(r, 1),
                    TrainNo = CellText(r, 2),
                    TrainType = CellText(r, 3),
                    Destination = CellText(r, 4)
                };
            }

            return rows;
        }

        private static TimetableTimeRow[] ReadTimeRows(DataGridView grid)
        {
            var rows = new TimetableTimeRow[grid.Rows.Count];
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                DataGridViewRow r = grid.Rows[i];
                rows[i] = new TimetableTimeRow
                {
                    Time4 = CellText(r, 0),
                    Highlight = CellBool(r, 1),
                    TrainNo = CellText(r, 2),
                    TypeAndDestination = CellText(r, 3),
                    NoteBlue = CellText(r, 4),
                    NoteRed = CellText(r, 5)
                };
            }

            return rows;
        }

        private static string CellText(DataGridViewRow row, int index)
        {
            object value = row.Cells[index].Value;
            return value == null ? string.Empty : value.ToString();
        }

        private static bool CellBool(DataGridViewRow row, int index)
        {
            object value = row.Cells[index].Value;
            if (value is bool)
            {
                return (bool)value;
            }

            bool parsed;
            return bool.TryParse(value == null ? string.Empty : value.ToString(), out parsed) && parsed;
        }

        private static ImageFormat GetImageFormatFromPath(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Jpeg;
            }

            if (string.Equals(ext, ".bmp", StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Bmp;
            }

            return ImageFormat.Png;
        }

        private static string FormatRevisedDate(int year, int month, int day)
        {
            int y = Clamp(year, 1, 9999);
            int m = Clamp(month, 1, 12);
            int d = Clamp(day, 1, DateTime.DaysInMonth(y, m));
            return string.Format("{0:0000}年{1:00}月{2:00}日改正", y, m, d);
        }

        private static void ParseRevisedDate(string raw, out int year, out int month, out int day)
        {
            year = 2025;
            month = 3;
            day = 15;

            MatchCollection matches = Regex.Matches(raw ?? string.Empty, @"\d+");
            if (matches.Count >= 3)
            {
                int parsedYear;
                int parsedMonth;
                int parsedDay;
                if (int.TryParse(matches[0].Value, out parsedYear))
                {
                    year = Clamp(parsedYear, 1, 9999);
                }
                if (int.TryParse(matches[1].Value, out parsedMonth))
                {
                    month = Clamp(parsedMonth, 1, 12);
                }
                if (int.TryParse(matches[2].Value, out parsedDay))
                {
                    day = Clamp(parsedDay, 1, DateTime.DaysInMonth(year, month));
                }
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
