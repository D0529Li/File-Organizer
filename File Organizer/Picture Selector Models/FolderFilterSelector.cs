using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace File_Organizer
{
    public class FolderFilterSelector : ISelector
    {
        private List<string> folderPaths = new List<string>();
        private int folderIndex = 0;
        private Random random = new Random();
        private Stack<int> folderIndicesToRemove = new Stack<int>();

        private readonly List<string> PicExtensions = new List<string> { ".jpg", ".png", ".jpeg" };

        public FolderFilterSelector(string selectedPath)
        {
            CollectionPath = selectedPath;
        }

        public override bool Start()
        {
            folderPaths = Directory.GetDirectories(CollectionPath).ToList();
            if (Directory.GetDirectories(CollectionPath).Length == 0)
            {
                MessageBox.Show("No subfolders found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            CollectionPath = folderPaths[folderIndex];
            if (!GrabAllFiles())
                // User chose not to delete the temp folder.
                return false;
            UpdateImagePaths();
            ImageIndex = 0;
            if (!VerifyCurrentFolder())
                Next();

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
                if (commitItem.Value)
                {
                    var index = 0;
                    foreach (var file in Directory.GetFiles(folderPaths[commitItem.Key]))
                        File.Move(file, $"{file[..file.LastIndexOf('\\')]} ({++index}){Path.GetExtension(file).ToLower()}");
                }
                foreach (var file in Directory.GetFiles(folderPaths[commitItem.Key], "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(folderPaths[commitItem.Key], true);
            }
            commitList.Clear();

            foreach (var folderIndex in folderIndicesToRemove)
            {
                var folder = folderPaths[folderIndex];
                // If there are non-picture files in this folder, move them to the root folder.
                if (Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Length != 0)
                {
                    var index = 0;
                    foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Move(file, $"{file[..file.LastIndexOf('\\')]} ({++index}){Path.GetExtension(file).ToLower()}");
                    }
                }

                folderPaths.Remove(folder);
                Directory.Delete(folder);
            }
        }

        public override void NextRandom()
        {
            if (ImagePaths.Count <= 1)
                ImageIndex = 0;
            else
            {
                var newIndex = RNG(ImagePaths.Count);
                while (newIndex == ImageIndex)
                    newIndex = RNG(ImagePaths.Count);
                ImageIndex = newIndex;
            }
        }

        /// <summary>
        /// Go to the next FOLDER (in order).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override bool Next()
        {
            if (folderIndex == folderPaths.Count - 1)
            {
                ImageIndex = -1;
                throw new ArgumentOutOfRangeException(nameof(Next));
            }

            ++folderIndex;

            return NextOrPrevious(true);
        }

        /// <summary>
        /// Go to the previous FOLDER (in order).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override bool Previous()
        {
            if (folderIndex == 0)
            {
                ImageIndex = -1;
                throw new ArgumentOutOfRangeException(nameof(Previous));
            }

            --folderIndex;

            return NextOrPrevious(false);
        }

        public override void AddCommitItem(bool commit)
        {
            commitList[folderIndex] = commit;
        }

        public override int GetRemainingCount()
        {
            return folderPaths.Count - folderIndex - 1;
        }

        private int RNG(int max)
        {
            random = new Random(DateTime.Now.Millisecond % 45);
            return random.Next(max);
        }

        private bool NextOrPrevious(bool next)
        {
            // folderPaths = Directory.GetDirectories(CollectionPath).ToList();
            var folder = folderPaths[folderIndex];
            if (!GrabAllFiles(folder))
                return false;
            if (next && !VerifyCurrentFolder())
            {
                Next();
                return true;
            }
            else
            {
                CollectionPath = folder;

                UpdateImagePaths();
                ImageIndex = RNG(GetPictureFiles(CollectionPath).Count);

                return true;
            }
            // logic to be modified.
        }

        private bool VerifyCurrentFolder()
        {
            var folder = folderPaths[folderIndex];
            if (GetPictureFiles(folder).Count == 0)
            {
                folderIndicesToRemove.Push(folderIndex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Take all files in subfolders and move them to the root folder.
        /// </summary>
        /// <returns>
        /// False if the user chooses not to delete the temp folder.
        /// Otherwise, true.
        /// </returns>
        private bool GrabAllFiles(string? folder = null)
        {
            folder ??= CollectionPath;

            if (Directory.Exists(folder + "\\TEMP"))
            {
                if (MessageBox.Show("Temp folder already exists. Delete it? ", "Temp Folder Conflict",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return false;
                else
                    Directory.Delete(folder + "\\TEMP");
            }

            Directory.CreateDirectory(folder + "\\TEMP");
            var files = Directory.GetFiles(folder, "*", searchOption: SearchOption.AllDirectories);
            var index = 0;

            // Move all files to TEMP folder first to avoid conflicts.
            foreach (var file in files)
            {
                var newFileName = string.Empty;
                if (PicExtensions.Contains(Path.GetExtension(file).ToLower()))
                    newFileName = $"{++index}.jpg";
                else
                    newFileName = $"{++index}{Path.GetExtension(file).ToLower()}";
                File.Move(file, folder + "\\TEMP\\" + newFileName);
            }

            // Move files from TEMP folder to root folder.
            index = 0;
            files = Directory.GetFiles(folder + "\\TEMP");
            foreach (var file in files)
            {
                var newFileName = string.Empty;
                if (PicExtensions.Contains(Path.GetExtension(file).ToLower()))
                    newFileName = $"{++index}.jpg";
                else
                    newFileName = $"{++index}{Path.GetExtension(file).ToLower()}";
                File.Move(file, folder + "\\" + newFileName);
            }

            // Remove subdirectories
            foreach (var subDirectory in Directory.GetDirectories(folder))
                Directory.Delete(subDirectory, true);

            return true;
        }
    }
}
