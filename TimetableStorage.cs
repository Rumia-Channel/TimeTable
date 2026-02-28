using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace TimeTableApp
{
    public static class TimetableStorage
    {
        private const string DataFolderName = "Data";
        private const string DefaultFileName = "timetable_default.xml";
        private const string CurrentFileName = "timetable_current.xml";

        public static string DataDirectory
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, DataFolderName);
            }
        }

        public static string DefaultFilePath
        {
            get { return Path.Combine(DataDirectory, DefaultFileName); }
        }

        public static string CurrentFilePath
        {
            get { return Path.Combine(DataDirectory, CurrentFileName); }
        }

        public static TimetableData LoadStartupData()
        {
            EnsureDataDirectory();

            TimetableData current = TryLoad(CurrentFilePath);
            if (current != null)
            {
                return current;
            }

            TimetableData fallback = LoadDefaultData();
            SaveCurrent(fallback);
            return fallback;
        }

        public static TimetableData LoadDefaultData()
        {
            EnsureDataDirectory();

            TimetableData data = TryLoad(DefaultFilePath);
            if (data != null)
            {
                return data;
            }

            TimetableData empty = TimetableData.CreateEmpty();
            Save(DefaultFilePath, empty);
            return empty;
        }

        public static void SaveCurrent(TimetableData data)
        {
            EnsureDataDirectory();
            Save(CurrentFilePath, data ?? TimetableData.CreateEmpty());
        }

        private static TimetableData TryLoad(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TimetableData));
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    TimetableData data = serializer.Deserialize(fs) as TimetableData;
                    if (data != null)
                    {
                        if (data.RemarksText == null)
                        {
                            data.RemarksText = TimetableData.DefaultRemarksText();
                        }
                        data.RemarksText = DecodeRemarksNewlines(data.RemarksText);

                        if (data.ContentTextScale <= 0f)
                        {
                            data.ContentTextScale = 0.95f;
                        }
                        else if (data.ContentTextScale < 0.50f)
                        {
                            data.ContentTextScale = 0.50f;
                        }
                        else if (data.ContentTextScale > 1.50f)
                        {
                            data.ContentTextScale = 1.50f;
                        }

                        NormalizeTimeNotes(data.TopTimes);
                        NormalizeTimeNotes(data.MidTimes);
                        NormalizeTimeNotes(data.BottomTimes);
                    }

                    return data;
                }
            }
            catch
            {
                return null;
            }
        }

        private static void Save(string path, TimetableData data)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TimetableData));
            string originalRemarks = data == null ? null : data.RemarksText;
            if (data != null)
            {
                data.RemarksText = EncodeRemarksNewlines(data.RemarksText);
            }

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                try
                {
                    serializer.Serialize(fs, data);
                }
                finally
                {
                    if (data != null)
                    {
                        data.RemarksText = originalRemarks;
                    }
                }
            }
        }

        private static void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }

        private static string EncodeRemarksNewlines(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.Replace("\\", "\\\\").Replace("\n", "\\n");
        }

        private static string DecodeRemarksNewlines(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string s = value;
            if (s.IndexOf('\\') >= 0)
            {
                StringBuilder sb = new StringBuilder(s.Length);
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (ch == '\\' && i + 1 < s.Length)
                    {
                        char next = s[i + 1];
                        if (next == 'n')
                        {
                            sb.Append(Environment.NewLine);
                            i++;
                            continue;
                        }

                        if (next == '\\')
                        {
                            sb.Append('\\');
                            i++;
                            continue;
                        }
                    }

                    sb.Append(ch);
                }

                s = sb.ToString();
            }

            s = s.Replace("\r\n", "\n").Replace('\r', '\n');
            return s.Replace("\n", Environment.NewLine);
        }

        private static void NormalizeTimeNotes(TimetableTimeRow[] rows)
        {
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                TimetableTimeRow row = rows[i];
                if (row == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(row.Note))
                {
                    continue;
                }

                string blue = row.NoteBlue ?? string.Empty;
                string red = row.NoteRed ?? string.Empty;
                bool hasBlue = !string.IsNullOrWhiteSpace(blue);
                bool hasRed = !string.IsNullOrWhiteSpace(red);
                if (!hasBlue && !hasRed)
                {
                    row.Note = string.Empty;
                    continue;
                }

                if (hasBlue && hasRed)
                {
                    row.Note = "{blue|" + blue + "}{red|" + red + "}";
                }
                else if (hasBlue)
                {
                    row.Note = "{blue|" + blue + "}";
                }
                else
                {
                    row.Note = "{red|" + red + "}";
                }
            }
        }
    }
}
