using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace File_Organizer
{
    public abstract class IPictureSelector
    {
        protected readonly Dictionary<int, bool> commitList = new Dictionary<int, bool>();
        protected string CollectionPath { get; set; } = string.Empty;
        protected List<string> ImagePaths { get; set; } = new List<string>();
        protected int ImageIndex { get; set; }

        public string ImagePath
        {
            get { return ImageIndex < 0 || ImagePaths.Count == 0 ? string.Empty : ImagePaths[ImageIndex]; }
        }

        public abstract bool Start();
        public abstract void Stop();
        public abstract void Commit();
        public abstract void AddCommitItem(bool commit);


        /// <summary>
        /// Go to the next image (in order).
        /// FolderFilterSelector overrides this method.
        /// <see cref="FolderFilterSelector.Next"/>
        /// </summary>
        public virtual bool Next()
        {
            if (ImageIndex == ImagePaths.Count - 1)
            {
                ImageIndex = -1;
                throw new ArgumentOutOfRangeException(nameof(Next));
            }

            ++ImageIndex;
            return true;
        }

        /// <summary>
        /// Go to the previous image (in order).
        /// FolderFilterSelector overrides this method.
        /// <see cref="FolderFilterSelector.Previous"/>
        /// </summary>
        public virtual bool Previous()
        {
            if (ImageIndex == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Previous));
            }

            --ImageIndex;
            return true;
        }

        /// <summary>
        /// Should only be called by FolderFilterSelector.
        /// <see cref="FolderFilterSelector.NextRandom"/>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void NextRandom() { throw new NotImplementedException(); }

        /// <summary>
        /// Update ImagePaths based on CollectionPath.
        /// </summary>
        protected void UpdateImagePaths()
        {
            if (CollectionPath != string.Empty)
                ImagePaths = GetPictureFiles(CollectionPath);
        }

        public List<string> GetPictureFiles(string path)
        {
            string[] extensions = [".jpg", ".png", ".jpeg"];
            ArgumentNullException.ThrowIfNull(extensions);

            var dir = new DirectoryInfo(path);
            IEnumerable<FileInfo> files = dir.EnumerateFiles();
            return files.Where(f => extensions.Contains(f.Extension)).Select(f => f.FullName).ToList();
        }

        public virtual int GetRemainingCount()
        {
            return ImagePaths.Count - ImageIndex - 1;
        }
    }
}
