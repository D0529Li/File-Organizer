namespace File_Organizer
{
    public static class Global
    {
        private static string _selectedPath = Constants.DEFAULT_SELECTED_PATH;
        public static string SelectedPath
        {
            get
            {
                return _selectedPath;
            }
            set
            {
                if (value != null)
                    _selectedPath = value;

                OrganizerViewModel?.UpdatePath();
                SelectorViewModel?.UpdatePath();

            }
        }

        public static OrganizerViewModel? OrganizerViewModel { get; set; } = null;
        public static SelectorViewModel? SelectorViewModel { get; set; } = null;
    }
}
