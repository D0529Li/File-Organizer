using System.IO;

namespace File_Organizer
{
    public class PictureFilterSelector : IPictureSelector
    {
        public PictureFilterSelector(string selectedPath)
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
            foreach (var commitItem in commitList)
            {
                if (!commitItem.Value)
                {
                    var file = ImagePaths[commitItem.Key];
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
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
