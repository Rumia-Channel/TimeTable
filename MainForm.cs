using System;
using System.Collections.Generic;
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
        private readonly ComboBox _dutyBox;
        private readonly ComboBox _directionBox;
        private readonly NumericUpDown _yearBox;
        private readonly NumericUpDown _monthBox;
        private readonly NumericUpDown _dayBox;
        private readonly TextBox _platform3Box;
        private readonly TextBox _platform4Box;
        private readonly NumericUpDown _widthBox;
        private readonly NumericUpDown _heightBox;
        private readonly CheckBox _bottomVerticalCheck;
        private readonly Label _statusLabel;

        private readonly DataGridView _subHomeInfoGrid;
        private readonly DataGridView _mainHomeTimeGrid;

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
            _serviceBox.ReadOnly = true;
            _dutyBox = CreateChoiceBox(70, "朝", "夕");
            _directionBox = CreateChoiceBox(70, "上り", "下り");
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
            AddLabeled(headPanel, "勤務", _dutyBox);
            AddLabeled(headPanel, "方向", _directionBox);
            AddLabeled(headPanel, "黒帯", _serviceBox);
            AddLabeled(headPanel, "改正日", revisedDatePanel);
            AddLabeled(headPanel, "メインホーム", _platform3Box);
            AddLabeled(headPanel, "サブホーム", _platform4Box);
            AddLabeled(headPanel, "幅", _widthBox);
            AddLabeled(headPanel, "高", _heightBox);
            headPanel.Controls.Add(_bottomVerticalCheck);

            _stationBox.TextChanged += ServiceLabelSourceChanged;
            _dutyBox.SelectedIndexChanged += ServiceLabelSourceChanged;
            _directionBox.SelectedIndexChanged += ServiceLabelSourceChanged;

            var tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            _subHomeInfoGrid = CreateInfoGrid(48);
            _mainHomeTimeGrid = CreateTimeGrid(48);

            AddSingleGridTab(tabs, "サブホーム", _subHomeInfoGrid);
            AddSingleGridTab(tabs, "メインホーム", _mainHomeTimeGrid);

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
            UpdateServiceLabelFromSelection();
            TimetableTrainInfoRow[] allInfo = TrimInfoRows(ReadInfoRows(_subHomeInfoGrid));
            TimeInputEntry[] allTimeEntries = ReadTimeInputEntries(ReadTimeRows(_mainHomeTimeGrid));

            TimetableTrainInfoRow[] topInfo;
            TimetableTrainInfoRow[] midInfo;
            TimetableTrainInfoRow[] bottomInfo;
            SplitInfoRowsByLayout(allInfo, (int)_widthBox.Value, (int)_heightBox.Value, out topInfo, out midInfo, out bottomInfo);

            TimeInputEntry[] topEntries;
            TimeInputEntry[] midEntries;
            TimeInputEntry[] bottomEntries;
            SplitTimeEntriesByLayout(allTimeEntries, (int)_widthBox.Value, (int)_heightBox.Value, out topEntries, out midEntries, out bottomEntries);

            TimetableTimeRow[] topTimes;
            string topLeftHour;
            int topOverlayColumn;
            string topOverlayHour;
            BuildTimeBandFromEntries(topEntries, out topTimes, out topLeftHour, out topOverlayColumn, out topOverlayHour);

            TimetableTimeRow[] midTimes;
            string midLeftHour;
            int midOverlayColumn;
            string midOverlayHour;
            BuildTimeBandFromEntries(midEntries, out midTimes, out midLeftHour, out midOverlayColumn, out midOverlayHour);

            TimetableTimeRow[] bottomTimes;
            string bottomLeftHour;
            int bottomOverlayColumn;
            string bottomOverlayHour;
            BuildTimeBandFromEntries(bottomEntries, out bottomTimes, out bottomLeftHour, out bottomOverlayColumn, out bottomOverlayHour);

            if (!string.IsNullOrWhiteSpace(bottomLeftHour) || !string.IsNullOrWhiteSpace(bottomOverlayHour))
            {
                _statusLabel.Text = "Bottom の時ラベルは未対応です（Top/Mid のみ反映）。";
            }

            return new TimetableData
            {
                StationName = _stationBox.Text,
                SubTitle = _subtitleBox.Text,
                ServiceLabel = _serviceBox.Text,
                RevisedDate = FormatRevisedDate((int)_yearBox.Value, (int)_monthBox.Value, (int)_dayBox.Value),
                Platform3Label = _platform3Box.Text,
                Platform4Label = _platform4Box.Text,
                DrawBottomVerticalLines = _bottomVerticalCheck.Checked,
                TopLeftHour = topLeftHour,
                TopOverlayColumn = topOverlayColumn,
                TopOverlayHour = topOverlayHour,
                MidLeftHour = midLeftHour,
                MidOverlayColumn = midOverlayColumn,
                MidOverlayHour = midOverlayHour,
                TopInfo = topInfo,
                TopTimes = topTimes,
                MidInfo = midInfo,
                MidTimes = midTimes,
                BottomInfo = bottomInfo,
                BottomTimes = bottomTimes
            };
        }

        private void LoadDataToUi(TimetableData data)
        {
            _stationBox.Text = data.StationName ?? string.Empty;
            _subtitleBox.Text = data.SubTitle ?? string.Empty;
            string duty;
            string direction;
            ParseServiceLabel(data.ServiceLabel, out duty, out direction);
            SelectChoice(_dutyBox, duty);
            SelectChoice(_directionBox, direction);
            UpdateServiceLabelFromSelection();
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

            WriteInfoRows(_subHomeInfoGrid, MergeInfoRows(data.TopInfo, data.MidInfo, data.BottomInfo));
            WriteTimeRows(
                _mainHomeTimeGrid,
                MergeTimeRowsWithHourMarkers(
                    data.TopTimes,
                    data.MidTimes,
                    data.BottomTimes,
                    data.TopLeftHour,
                    data.TopOverlayColumn,
                    data.TopOverlayHour,
                    data.MidLeftHour,
                    data.MidOverlayColumn,
                    data.MidOverlayHour));
        }

        private static DataGridView CreateInfoGrid(int rows)
        {
            var grid = BaseGrid();
            grid.Columns.Add("Code", "Time");
            var colorCol = new DataGridViewComboBoxColumn();
            colorCol.Name = "CodeColor";
            colorCol.HeaderText = "TimeColor";
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
            grid.Columns.Add("Time4", "Time/Hour");
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

        private static void AddSingleGridTab(TabControl tabs, string tabTitle, Control gridControl)
        {
            var page = new TabPage(tabTitle);

            var group = new GroupBox();
            group.Text = "入力";
            group.Dock = DockStyle.Fill;
            gridControl.Dock = DockStyle.Fill;
            group.Controls.Add(gridControl);

            page.Controls.Add(group);
            tabs.TabPages.Add(page);
        }

        private static TextBox CreateTextBox(int width)
        {
            var box = new TextBox();
            box.Width = width;
            return box;
        }

        private static ComboBox CreateChoiceBox(int width, string item1, string item2)
        {
            var box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Width = width;
            box.Items.Add(item1);
            box.Items.Add(item2);
            box.SelectedIndex = 0;
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
                    Code = NormalizeInfoTimeCode(CellText(r, 0)),
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

        private static TimetableTrainInfoRow[] MergeInfoRows(params TimetableTrainInfoRow[][] groups)
        {
            var merged = new List<TimetableTrainInfoRow>();
            for (int i = 0; i < groups.Length; i++)
            {
                TimetableTrainInfoRow[] rows = groups[i];
                if (rows == null)
                {
                    continue;
                }

                for (int j = 0; j < rows.Length; j++)
                {
                    if (rows[j] != null)
                    {
                        merged.Add(rows[j]);
                    }
                }
            }

            return merged.ToArray();
        }

        private static TimetableTimeRow[] MergeTimeRows(params TimetableTimeRow[][] groups)
        {
            var merged = new List<TimetableTimeRow>();
            for (int i = 0; i < groups.Length; i++)
            {
                TimetableTimeRow[] rows = groups[i];
                if (rows == null)
                {
                    continue;
                }

                for (int j = 0; j < rows.Length; j++)
                {
                    if (rows[j] != null)
                    {
                        merged.Add(rows[j]);
                    }
                }
            }

            return merged.ToArray();
        }

        private static TimetableTimeRow[] MergeTimeRowsWithHourMarkers(
            TimetableTimeRow[] topRows,
            TimetableTimeRow[] midRows,
            TimetableTimeRow[] bottomRows,
            string topLeftHour,
            int topOverlayColumn,
            string topOverlayHour,
            string midLeftHour,
            int midOverlayColumn,
            string midOverlayHour)
        {
            TimeInputEntry[] topEntries = BuildEntriesForBand(topRows, topLeftHour, topOverlayColumn, topOverlayHour);
            TimeInputEntry[] midEntries = BuildEntriesForBand(midRows, midLeftHour, midOverlayColumn, midOverlayHour);
            TimeInputEntry[] bottomEntries = BuildEntriesForBand(bottomRows, string.Empty, -1, string.Empty);

            var rows = new List<TimetableTimeRow>();
            AppendEntriesAsRows(rows, topEntries);
            AppendEntriesAsRows(rows, midEntries);
            AppendEntriesAsRows(rows, bottomEntries);
            return rows.ToArray();
        }

        private static TimetableTrainInfoRow[] TrimInfoRows(TimetableTrainInfoRow[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                return new TimetableTrainInfoRow[0];
            }

            int last = -1;
            for (int i = 0; i < rows.Length; i++)
            {
                TimetableTrainInfoRow row = rows[i];
                if (row != null && !IsInfoRowEmpty(row))
                {
                    last = i;
                }
            }

            if (last < 0)
            {
                return new TimetableTrainInfoRow[0];
            }

            var trimmed = new TimetableTrainInfoRow[last + 1];
            for (int i = 0; i <= last; i++)
            {
                trimmed[i] = rows[i] ?? new TimetableTrainInfoRow();
            }

            return trimmed;
        }

        private static TimetableTimeRow[] TrimTimeRows(TimetableTimeRow[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                return new TimetableTimeRow[0];
            }

            int last = -1;
            for (int i = 0; i < rows.Length; i++)
            {
                TimetableTimeRow row = rows[i];
                if (row != null && !IsTimeRowEmpty(row))
                {
                    last = i;
                }
            }

            if (last < 0)
            {
                return new TimetableTimeRow[0];
            }

            var trimmed = new TimetableTimeRow[last + 1];
            for (int i = 0; i <= last; i++)
            {
                trimmed[i] = rows[i] ?? new TimetableTimeRow();
            }

            return trimmed;
        }

        private static TimeInputEntry[] ReadTimeInputEntries(TimetableTimeRow[] rows)
        {
            var entries = new List<TimeInputEntry>();
            if (rows == null)
            {
                return entries.ToArray();
            }

            for (int i = 0; i < rows.Length; i++)
            {
                TimetableTimeRow row = rows[i];
                if (row == null || IsTimeRowEmpty(row))
                {
                    continue;
                }

                string hourText;
                if (TryGetHourMarker(row, out hourText))
                {
                    entries.Add(new TimeInputEntry { IsHour = true, HourText = hourText, TimeRow = null });
                }
                else
                {
                    entries.Add(new TimeInputEntry { IsHour = false, HourText = string.Empty, TimeRow = row });
                }
            }

            return entries.ToArray();
        }

        private static void SplitTimeEntriesByLayout(
            TimeInputEntry[] entries,
            int width,
            int height,
            out TimeInputEntry[] top,
            out TimeInputEntry[] mid,
            out TimeInputEntry[] bottom)
        {
            int total = entries == null ? 0 : entries.Length;
            int[] counts = CalculateSectionCounts(width, height, total);
            top = SliceTimeEntries(entries, 0, counts[0]);
            mid = SliceTimeEntries(entries, counts[0], counts[1]);
            bottom = SliceTimeEntries(entries, counts[0] + counts[1], counts[2]);
        }

        private static TimeInputEntry[] SliceTimeEntries(TimeInputEntry[] entries, int start, int count)
        {
            if (count <= 0)
            {
                return new TimeInputEntry[0];
            }

            var result = new TimeInputEntry[count];
            for (int i = 0; i < count; i++)
            {
                int idx = start + i;
                if (entries != null && idx >= 0 && idx < entries.Length && entries[idx] != null)
                {
                    result[i] = entries[idx];
                }
                else
                {
                    result[i] = new TimeInputEntry { IsHour = false, HourText = string.Empty, TimeRow = new TimetableTimeRow() };
                }
            }

            return result;
        }

        private static void BuildTimeBandFromEntries(
            TimeInputEntry[] entries,
            out TimetableTimeRow[] rows,
            out string leftHour,
            out int overlayColumn,
            out string overlayHour)
        {
            var times = new List<TimetableTimeRow>();
            var hourSlots = new List<HourSlot>();
            leftHour = string.Empty;
            overlayColumn = -1;
            overlayHour = string.Empty;

            if (entries == null || entries.Length == 0)
            {
                rows = new TimetableTimeRow[0];
                return;
            }

            for (int slot = 0; slot < entries.Length; slot++)
            {
                TimeInputEntry entry = entries[slot];
                if (entry == null)
                {
                    continue;
                }

                if (entry.IsHour)
                {
                    hourSlots.Add(new HourSlot { Slot = slot, Text = entry.HourText ?? string.Empty });
                    continue;
                }

                if (entry.TimeRow != null)
                {
                    times.Add(entry.TimeRow);
                }
            }

            for (int i = 0; i < hourSlots.Count; i++)
            {
                HourSlot hs = hourSlots[i];
                if (hs.Slot == 0 && string.IsNullOrWhiteSpace(leftHour))
                {
                    leftHour = hs.Text;
                }
                else if (string.IsNullOrWhiteSpace(overlayHour))
                {
                    overlayHour = hs.Text;
                    overlayColumn = hs.Slot;
                    if (!string.IsNullOrWhiteSpace(leftHour) && overlayColumn > 0)
                    {
                        overlayColumn--;
                    }
                }
            }

            rows = TrimTimeRows(times.ToArray());
        }

        private static TimeInputEntry[] BuildEntriesForBand(
            TimetableTimeRow[] rows,
            string leftHour,
            int overlayColumn,
            string overlayHour)
        {
            int rowCount = rows == null ? 0 : rows.Length;
            int hourCount = (string.IsNullOrWhiteSpace(leftHour) ? 0 : 1) + (string.IsNullOrWhiteSpace(overlayHour) ? 0 : 1);
            int slotCount = rowCount + hourCount;
            if (slotCount <= 0)
            {
                return new TimeInputEntry[0];
            }

            var entries = new TimeInputEntry[slotCount];
            if (!string.IsNullOrWhiteSpace(leftHour))
            {
                entries[0] = new TimeInputEntry { IsHour = true, HourText = leftHour };
            }

            if (!string.IsNullOrWhiteSpace(overlayHour))
            {
                int overlaySlot = overlayColumn;
                if (!string.IsNullOrWhiteSpace(leftHour))
                {
                    overlaySlot++;
                }

                overlaySlot = Clamp(overlaySlot, 0, slotCount - 1);
                while (overlaySlot < slotCount && entries[overlaySlot] != null)
                {
                    overlaySlot++;
                }

                if (overlaySlot >= slotCount)
                {
                    overlaySlot = slotCount - 1;
                    while (overlaySlot >= 0 && entries[overlaySlot] != null)
                    {
                        overlaySlot--;
                    }
                }

                if (overlaySlot >= 0)
                {
                    entries[overlaySlot] = new TimeInputEntry { IsHour = true, HourText = overlayHour };
                }
            }

            int rowIndex = 0;
            for (int i = 0; i < slotCount; i++)
            {
                if (entries[i] != null)
                {
                    continue;
                }

                TimetableTimeRow row = (rows != null && rowIndex < rows.Length && rows[rowIndex] != null)
                    ? rows[rowIndex]
                    : new TimetableTimeRow();
                entries[i] = new TimeInputEntry { IsHour = false, HourText = string.Empty, TimeRow = row };
                rowIndex++;
            }

            return entries;
        }

        private static void AppendEntriesAsRows(List<TimetableTimeRow> target, TimeInputEntry[] entries)
        {
            if (target == null || entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                TimeInputEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.IsHour)
                {
                    target.Add(CreateHourMarkerRow(entry.HourText));
                }
                else
                {
                    target.Add(entry.TimeRow ?? new TimetableTimeRow());
                }
            }
        }

        private static bool IsInfoRowEmpty(TimetableTrainInfoRow row)
        {
            return string.IsNullOrWhiteSpace(row.Code)
                && string.IsNullOrWhiteSpace(row.TrainNo)
                && string.IsNullOrWhiteSpace(row.TrainType)
                && string.IsNullOrWhiteSpace(row.Destination);
        }

        private static bool IsTimeRowEmpty(TimetableTimeRow row)
        {
            return string.IsNullOrWhiteSpace(row.Time4)
                && !row.Highlight
                && string.IsNullOrWhiteSpace(row.TrainNo)
                && string.IsNullOrWhiteSpace(row.TypeAndDestination)
                && string.IsNullOrWhiteSpace(row.NoteBlue)
                && string.IsNullOrWhiteSpace(row.NoteRed);
        }

        private static bool TryGetHourMarker(TimetableTimeRow row, out string hourText)
        {
            hourText = string.Empty;
            if (row == null)
            {
                return false;
            }

            if (row.Highlight
                || !string.IsNullOrWhiteSpace(row.TrainNo)
                || !string.IsNullOrWhiteSpace(row.TypeAndDestination)
                || !string.IsNullOrWhiteSpace(row.NoteBlue)
                || !string.IsNullOrWhiteSpace(row.NoteRed))
            {
                return false;
            }

            Match match = Regex.Match(row.Time4 ?? string.Empty, @"^\s*(\d{1,2})\s*時\s*$");
            if (!match.Success)
            {
                return false;
            }

            int hour;
            if (!int.TryParse(match.Groups[1].Value, out hour))
            {
                return false;
            }

            hourText = hour.ToString() + "時";
            return true;
        }

        private static void SplitInfoRowsByLayout(
            TimetableTrainInfoRow[] rows,
            int width,
            int height,
            out TimetableTrainInfoRow[] top,
            out TimetableTrainInfoRow[] mid,
            out TimetableTrainInfoRow[] bottom)
        {
            int[] counts = CalculateSectionCounts(width, height, rows == null ? 0 : rows.Length);
            top = SliceInfoRows(rows, 0, counts[0]);
            mid = SliceInfoRows(rows, counts[0], counts[1]);
            bottom = SliceInfoRows(rows, counts[0] + counts[1], counts[2]);
        }

        private static TimetableTrainInfoRow[] SliceInfoRows(TimetableTrainInfoRow[] rows, int start, int count)
        {
            if (count <= 0)
            {
                return new TimetableTrainInfoRow[0];
            }

            var result = new TimetableTrainInfoRow[count];
            for (int i = 0; i < count; i++)
            {
                int index = start + i;
                result[i] = (rows != null && index >= 0 && index < rows.Length && rows[index] != null)
                    ? rows[index]
                    : new TimetableTrainInfoRow();
            }

            return result;
        }

        private static TimetableTimeRow[] SliceTimeRows(TimetableTimeRow[] rows, int start, int count)
        {
            if (count <= 0)
            {
                return new TimetableTimeRow[0];
            }

            var result = new TimetableTimeRow[count];
            for (int i = 0; i < count; i++)
            {
                int index = start + i;
                result[i] = (rows != null && index >= 0 && index < rows.Length && rows[index] != null)
                    ? rows[index]
                    : new TimetableTimeRow();
            }

            return result;
        }

        private static int[] CalculateSectionCounts(int width, int height, int totalCount)
        {
            var counts = new[] { 0, 0, 0 };
            if (totalCount <= 0)
            {
                return counts;
            }

            float tableWidth = width * 0.91f;
            float topBandUsable = tableWidth - (tableWidth * 0.06f) - (tableWidth * 0.024f);
            float midBandUsable = topBandUsable;
            float bottomBandWidth = tableWidth * (1f - 0.415f);
            float bottomBandUsable = bottomBandWidth - (bottomBandWidth * 0.11f) - (bottomBandWidth * 0.024f);

            float[] weights = { Math.Max(1f, topBandUsable), Math.Max(1f, midBandUsable), Math.Max(1f, bottomBandUsable) };
            return DistributeCounts(totalCount, weights);
        }

        private static int[] DistributeCounts(int totalCount, float[] weights)
        {
            var counts = new[] { 0, 0, 0 };
            if (totalCount <= 0)
            {
                return counts;
            }

            int seed = Math.Min(3, totalCount);
            for (int i = 0; i < seed; i++)
            {
                counts[i] = 1;
            }

            int remaining = totalCount - seed;
            if (remaining <= 0)
            {
                return counts;
            }

            float sum = weights[0] + weights[1] + weights[2];
            var fractions = new[] { 0f, 0f, 0f };
            int assigned = 0;
            for (int i = 0; i < 3; i++)
            {
                float exact = remaining * (weights[i] / sum);
                int add = (int)Math.Floor(exact);
                counts[i] += add;
                assigned += add;
                fractions[i] = exact - add;
            }

            int left = remaining - assigned;
            while (left > 0)
            {
                int pick = 0;
                if (fractions[1] > fractions[pick])
                {
                    pick = 1;
                }
                if (fractions[2] > fractions[pick])
                {
                    pick = 2;
                }

                counts[pick]++;
                fractions[pick] = -1f;
                left--;
            }

            return counts;
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

        private static string NormalizeInfoTimeCode(string raw)
        {
            string value = (raw ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return string.Empty;
            }

            string digits = Regex.Replace(value, @"\D", string.Empty);
            if (digits.Length == 0)
            {
                return value;
            }

            if (digits.Length >= 4)
            {
                digits = digits.Substring(0, 4);
            }
            else
            {
                digits = digits.PadLeft(4, '0');
            }

            return "(" + digits + ")";
        }

        private static TimetableTimeRow CreateHourMarkerRow(string text)
        {
            return new TimetableTimeRow
            {
                Time4 = text ?? string.Empty,
                Highlight = false,
                TrainNo = string.Empty,
                TypeAndDestination = string.Empty,
                NoteBlue = string.Empty,
                NoteRed = string.Empty
            };
        }

        private void ServiceLabelSourceChanged(object sender, EventArgs e)
        {
            UpdateServiceLabelFromSelection();
        }

        private void UpdateServiceLabelFromSelection()
        {
            string station = NormalizeStationForService(_stationBox.Text);
            string duty = _dutyBox.SelectedItem == null ? "夕" : _dutyBox.SelectedItem.ToString();
            string direction = _directionBox.SelectedItem == null ? "上り" : _directionBox.SelectedItem.ToString();
            _serviceBox.Text = station + duty + direction;
        }

        private static string NormalizeStationForService(string station)
        {
            string value = Regex.Replace((station ?? string.Empty).Trim(), @"\s+", string.Empty);
            if (value.EndsWith("駅", StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - 1);
            }

            return value;
        }

        private static void ParseServiceLabel(string raw, out string duty, out string direction)
        {
            string value = raw ?? string.Empty;
            duty = value.Contains("朝") ? "朝" : "夕";
            direction = value.Contains("下り") ? "下り" : "上り";
        }

        private static void SelectChoice(ComboBox box, string value)
        {
            for (int i = 0; i < box.Items.Count; i++)
            {
                if (string.Equals(box.Items[i].ToString(), value, StringComparison.Ordinal))
                {
                    box.SelectedIndex = i;
                    return;
                }
            }

            if (box.Items.Count > 0 && box.SelectedIndex < 0)
            {
                box.SelectedIndex = 0;
            }
        }

        private sealed class TimeInputEntry
        {
            public bool IsHour;
            public string HourText;
            public TimetableTimeRow TimeRow;
        }

        private sealed class HourSlot
        {
            public int Slot;
            public string Text;
        }
    }
}
