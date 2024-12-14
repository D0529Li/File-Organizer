using System;
using System.Collections.Generic;

namespace File_Organizer
{
    public static class Constants
    {
        public static readonly List<string> PicExtensions = [".jpg", ".png", ".jpeg"];
        public static readonly string PERSISTED_SETTINGS_PATH = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\File Organizer Persisted Settings.xml";
    }
}
