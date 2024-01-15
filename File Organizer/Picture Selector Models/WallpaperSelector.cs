using System.IO;

namespace File_Organizer
{
    public class WallpaperSelector : ISelector
    {
        private const string DEFAULT_WALLPAPER_PATH = "D:\\123\\test\\Wallpapers";

        public WallpaperSelector(string selectedPath)
        {
            CollectionPath = selectedPath;
        }

        public override bool Start()
        {
            UpdateImagePaths();
            ImageIndex = 0;
            return true;
        }

        public override void Stop()
        {
            ImageIndex = -1;
        }

        public override void Commit()
        {
            var index = 0;

            if (Directory.Exists(DEFAULT_WALLPAPER_PATH))
                index = GetPictureFiles(DEFAULT_WALLPAPER_PATH).Count;
            else
                Directory.CreateDirectory(DEFAULT_WALLPAPER_PATH);

            foreach (var commitItem in commitList)
            {
                if (commitItem.Value)
                {
                    var file = ImagePaths[commitItem.Key];
                    File.Copy(file, $"{DEFAULT_WALLPAPER_PATH}{CollectionPath[CollectionPath.LastIndexOf('\\')..]} ({++index}).jpg");
                }
            }
            commitList.Clear();
        }

        public override void AddCommitItem(bool commit)
        {
            commitList[ImageIndex] = commit;
        }
    }
}
