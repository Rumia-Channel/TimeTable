using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TimeTableApp
{
    public sealed class MainForm : Form
    {
        private readonly DataGridView _grid;
        private readonly Button _saveButton;
        private readonly Button _saveImageButton;
        private readonly Button _loadButton;
        private readonly Button _clearButton;
        private readonly Label _statusLabel;

        public MainForm()
        {
            Text = "TimeTable (.NET Framework 4.8)";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(820, 520);
            Width = 980;
            Height = 640;

            var topPanel = new FlowLayoutPanel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 44;
            topPanel.Padding = new Padding(8);
            topPanel.WrapContents = false;

            _saveButton = new Button();
            _saveButton.Text = "Save CSV";
            _saveButton.AutoSize = true;
            _saveButton.Click += SaveButton_Click;

            _saveImageButton = new Button();
            _saveImageButton.Text = "Save Image";
            _saveImageButton.AutoSize = true;
            _saveImageButton.Click += SaveImageButton_Click;

            _loadButton = new Button();
            _loadButton.Text = "Load CSV";
            _loadButton.AutoSize = true;
            _loadButton.Click += LoadButton_Click;

            _clearButton = new Button();
            _clearButton.Text = "Clear";
            _clearButton.AutoSize = true;
            _clearButton.Click += ClearButton_Click;

            _statusLabel = new Label();
            _statusLabel.AutoSize = true;
            _statusLabel.Padding = new Padding(12, 7, 0, 0);
            _statusLabel.Text = "Edit timetable and save to CSV.";

            topPanel.Controls.Add(_saveButton);
            topPanel.Controls.Add(_saveImageButton);
            topPanel.Controls.Add(_loadButton);
            topPanel.Controls.Add(_clearButton);
            topPanel.Controls.Add(_statusLabel);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersWidth = 90;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            BuildGrid();

            Controls.Add(_grid);
            Controls.Add(topPanel);
        }

        private void BuildGrid()
        {
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            _grid.Columns.Clear();
            foreach (string day in days)
            {
                _grid.Columns.Add(day, day);
            }

            _grid.Rows.Clear();
            for (int period = 1; period <= 8; period++)
            {
                int rowIndex = _grid.Rows.Add();
                _grid.Rows[rowIndex].HeaderCell.Value = "Period " + period;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.FileName = "timetable.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var lines = new List<string>();
                var header = new List<string>();
                for (int c = 0; c < _grid.Columns.Count; c++)
                {
                    header.Add(EscapeCsv(_grid.Columns[c].HeaderText));
                }
                lines.Add(string.Join(",", header.ToArray()));

                for (int r = 0; r < _grid.Rows.Count; r++)
                {
                    var cells = new List<string>();
                    for (int c = 0; c < _grid.Columns.Count; c++)
                    {
                        object value = _grid.Rows[r].Cells[c].Value;
                        cells.Add(EscapeCsv(value == null ? string.Empty : value.ToString()));
                    }
                    lines.Add(string.Join(",", cells.ToArray()));
                }

                File.WriteAllLines(dialog.FileName, lines.ToArray(), Encoding.UTF8);
                _statusLabel.Text = "Saved: " + dialog.FileName;
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                string[] lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    MessageBox.Show(this, "CSV does not contain timetable rows.", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                for (int r = 0; r < _grid.Rows.Count; r++)
                {
                    for (int c = 0; c < _grid.Columns.Count; c++)
                    {
                        _grid.Rows[r].Cells[c].Value = string.Empty;
                    }
                }

                int maxRows = Math.Min(_grid.Rows.Count, lines.Length - 1);
                for (int r = 0; r < maxRows; r++)
                {
                    string[] cells = ParseCsvLine(lines[r + 1]);
                    int maxCols = Math.Min(_grid.Columns.Count, cells.Length);
                    for (int c = 0; c < maxCols; c++)
                    {
                        _grid.Rows[r].Cells[c].Value = cells[c];
                    }
                }

                _statusLabel.Text = "Loaded: " + dialog.FileName;
            }
        }

        private void SaveImageButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG image (*.png)|*.png|JPEG image (*.jpg)|*.jpg|Bitmap image (*.bmp)|*.bmp|All files (*.*)|*.*";
                dialog.FileName = "timetable.png";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _grid.EndEdit();

                using (var bitmap = new Bitmap(_grid.Width, _grid.Height))
                {
                    _grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, _grid.Size));
                    bitmap.Save(dialog.FileName, GetImageFormatFromPath(dialog.FileName));
                }

                _statusLabel.Text = "Saved image: " + dialog.FileName;
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            for (int r = 0; r < _grid.Rows.Count; r++)
            {
                for (int c = 0; c < _grid.Columns.Count; c++)
                {
                    _grid.Rows[r].Cells[c].Value = string.Empty;
                }
            }

            _statusLabel.Text = "Cleared.";
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(ch);
                }
            }

            values.Add(current.ToString());
            return values.ToArray();
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
    }
}
