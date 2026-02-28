using System;
using System.IO;
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
                    return serializer.Deserialize(fs) as TimetableData;
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
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(fs, data);
            }
        }

        private static void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }
    }
}
