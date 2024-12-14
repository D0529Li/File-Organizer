using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using MSGBOX = System.Windows.MessageBox;

namespace File_Organizer
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Fields

        private FilterMode _selectedMode = FilterMode.FolderFilterMode;
        private bool _isStarted = false;
        private ISelector? selector;
        private string _selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private bool _disposed = false;

        #endregion


        #region Properties

        public string CurrentImagePath { get { return selector?.ImagePath ?? string.Empty; } }

        public BitmapImage? CurrentImage
        {
            get
            {
                if (CurrentImagePath == null || CurrentImagePath == string.Empty)
                    return null;

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(CurrentImagePath);
                image.EndInit();
                return image;
            }
        }

        public string RemainingCountText
        {
            get
            {
                return $"{selector?.GetRemainingCount()} remaining " + (SelectedMode == FilterMode.FolderFilterMode ? "folders" : "pictures");
            }
        }

        public bool IsStarted
        {
            get { return _isStarted; }
            set
            {
                _isStarted = value;
                OnPropertyChanged(nameof(IsStarted));
            }
        }

        public bool AreExtraButtonsVisible
        {
            get { return SelectedMode == FilterMode.FolderFilterMode; }
        }

        public string SelectedPath
        {
            get
            {
                return _selectedPath;
            }
            set
            {
                if (value != null)
                    _selectedPath = value;
            }
        }

        public string DisplayedPath
        {
            get
            {
                var result = SelectedPath;
                if (SelectedMode == FilterMode.FolderFilterMode)
                {
                    result += $"\t{CurrentImagePath}";
                }
                return result;
            }
        }

        public FilterMode SelectedMode
        {
            get { return _selectedMode; }
            set
            {
                _selectedMode = value;
            }
        }

        public bool IsPicFilterModeSelected
        {
            get { return SelectedMode == FilterMode.PicFilterMode; }
        }

        #endregion


        #region Commands

        public ICommand ChooseFolderCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand PreviousCommand { get; set; }
        public ICommand KeepAllCommand { get; set; }
        public ICommand KeepCommand { get; set; }
        public ICommand DropCommand { get; set; }
        public ICommand SkipCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand OpenFolderCommand { get; set; }
        public ICommand SeeMoreCommand { get; set; }
        public ICommand CommitCommand { get; set; }
        public ICommand OrganizeCommand { get; set; }
        public ICommand PicFilterModeSelectedCommand { get; set; }
        public ICommand FolderFilterModeSelectedCommand { get; set; }

        #endregion

        public MainWindowViewModel()
        {
            ChooseFolderCommand = new DelegateCommand<object>(OnChooseFolder);
            StartCommand = new DelegateCommand<object>(OnStart);
            SeeMoreCommand = new DelegateCommand<object>(OnSeeMore);
            PreviousCommand = new DelegateCommand<object>(OnPrevious);
            StopCommand = new DelegateCommand<object>(OnStop);
            OpenFolderCommand = new DelegateCommand<object>(OnOpenFolder);
            KeepAllCommand = new DelegateCommand<object>(OnKeepAll);
            KeepCommand = new DelegateCommand<object>(OnKeep);
            DropCommand = new DelegateCommand<object>(OnDrop);
            SkipCommand = new DelegateCommand<object>(OnSkip);
            CommitCommand = new DelegateCommand<object>(OnCommit);
            OrganizeCommand = new DelegateCommand<object>(OnOrganize);
            PicFilterModeSelectedCommand = new DelegateCommand<object>(OnPicFilterModeSelected);
            FolderFilterModeSelectedCommand = new DelegateCommand<object>(OnFolderFilterModeSelected);

            OnPropertyChanged(nameof(DisplayedPath));

            if (File.Exists(Constants.PERSISTED_SETTINGS_PATH))
            {
                var serializer = new XmlSerializer(typeof(PersistedSettings));
                using (var reader = new StreamReader(Constants.PERSISTED_SETTINGS_PATH))
                {
                    var deserialized = serializer.Deserialize(reader);
                    if (deserialized != null)
                    {
                        var persistedSettings = (PersistedSettings)deserialized;
                        SelectedPath = persistedSettings.SELECTED_PATH;
                        SelectedMode = persistedSettings.MODE;
                        OnPropertyChanged(nameof(SelectedPath));
                        OnPropertyChanged(nameof(SelectedMode));
                    }
                }
            }
        }


        #region Private Methods

        private void OnOrganize(object _)
        {
            Task.Run(() => OrganizeAsync(SelectedPath));
        }

        private void OnChooseFolder(object _)
        {
            var dialog = new FolderBrowserDialog()
            {
                InitialDirectory = SelectedPath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                SelectedPath = dialog.SelectedPath;

            OnPropertyChanged(nameof(DisplayedPath));
        }

        private void OnPicFilterModeSelected(object _)
        {
            SelectedMode = FilterMode.PicFilterMode;
        }

        private void OnFolderFilterModeSelected(object _)
        {
            SelectedMode = FilterMode.FolderFilterMode;
        }

        private void OnStart(object _)
        {
            switch (SelectedMode)
            {
                case FilterMode.PicFilterMode:
                    selector = new FilterSelector(SelectedPath);
                    break;
                case FilterMode.FolderFilterMode:
                    selector = new FolderFilterSelector(SelectedPath);
                    break;
            }

            try
            {
                if (selector == null || !selector.Start())
                    return;
            }
            catch (ArgumentOutOfRangeException)
            {
                PromptCommit();
                return;
            }

            IsStarted = true;
            OnPropertyChanged(nameof(AreExtraButtonsVisible));
            RefreshImage();
        }

        private void OnStop(object _)
        {
            selector?.Stop();
            IsStarted = false;
            RefreshImage();
        }

        private void OnSeeMore(object _)
        {
            selector?.NextRandom();
            RefreshImage();
        }

        private void OnPrevious(object _)
        {
            try
            {
                selector?.Previous();
            }
            catch (ArgumentOutOfRangeException)
            {
                System.Windows.MessageBox.Show("This is already the first image!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            RefreshImage();
        }

        private void OnKeep(object _)
        {
            Task.Run(() => KeepOrDrop(true));
        }

        private void OnDrop(object _)
        {
            Task.Run(() => KeepOrDrop(false));
        }

        private void OnSkip(object _)
        {
            KeepOrDrop(false, true);
        }

        private void OnCommit(object _)
        {
            Task.Run(() => selector?.Commit());
        }

        private void OnOpenFolder(object _)
        {
            if (SelectedPath == null)
                return;

            System.Diagnostics.Process.Start("explorer.exe", SelectedPath);
        }

        private void OnKeepAll(object _)
        {
            selector?.AddAllCommitItems();
            RefreshImage();
            CommitCommand.Execute(null);
            StopCommand.Execute(null);
        }

        private void KeepOrDrop(bool keep, bool skip = false) // to be modified
        {
            if (!skip)
                selector?.AddCommitItem(keep);
            try
            {
                selector?.Next();
            }
            catch (ArgumentOutOfRangeException)
            {
                PromptCommit();
            }
            RefreshImage();
        }

        private void PromptCommit()
        {
            if (System.Windows.MessageBox.Show("No more folders. Commit? ", "Commit?",
                                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RefreshImage();
                CommitCommand.Execute(null);
                StopCommand.Execute(null);
            }
        }

        private void RefreshImage()
        {
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(DisplayedPath));
            OnPropertyChanged(nameof(RemainingCountText));
        }

        private async Task OrganizeAsync(string path)
        {
            // Todo: Add progress bar

            if (path == string.Empty)
                return;

            try
            {
                var dirName = Path.GetFileName(path);
                var dirHor = Directory.CreateDirectory($"{path}\\{dirName} Horizontal");
                var dirVer = Directory.CreateDirectory($"{path}\\{dirName} Vertical");
                var indexHor = Directory.GetFiles(dirHor.FullName).Length;
                var indexVer = Directory.GetFiles(dirVer.FullName).Length;
                var lockHor = new object();
                var lockVer = new object();

                // Prep: Reorder existing files and uncheck readonly
                var task1 = Task.Run(() => ReorderFiles(dirHor.FullName));
                var task2 = Task.Run(() => ReorderFiles(dirVer.FullName));
                await Task.WhenAll(task1, task2);
                await Task.Run(() => UncheckReadonlyOnFiles(path));

                // Prep: Rename all files to .jpg
                RenamePicExtensions(path);

                // Organize multi-threaded
                var files = Directory.GetFiles(path);
                Parallel.ForEach(files, file =>
                {
                    if (!file.EndsWith(".jpg"))
                        return;

                    FileStream? stream = null;
                    Image? image = null;

                    try
                    {
                        stream = File.OpenRead(file);
                        image = Image.FromStream(stream, false, false);
                        stream?.Close();

                        if (image.Width > image.Height)
                            indexHor = MoveHorizontal(file, ref lockHor, dirHor, dirName, indexHor);
                        else
                            indexVer = MoveVertical(file, ref lockVer, dirVer, dirName, indexVer);
                    }
                    catch (ArgumentException) { }
                    finally
                    {
                        image?.Dispose();
                    }
                });

                MSGBOX.Show($"Organize \"{dirName}\" completed.", "Organize Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MSGBOX.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool RenamePicExtensions(string path)
        {
            if (path == string.Empty)
                return false;

            if (Directory.Exists(path + "\\TEMP"))
            {
                if (System.Windows.MessageBox.Show("Temp folder already exists. Delete it? ", "Temp Folder Conflict",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return false;
                else
                    Directory.Delete(path + "\\TEMP");
            }

            Directory.CreateDirectory(path + "\\TEMP");

            var files = Directory.GetFiles(path, "*", searchOption: SearchOption.TopDirectoryOnly);
            var index = 0;

            // Move all files to TEMP folder first with index for each.
            foreach (var file in files)
            {
                var newFileName = string.Empty;
                if (Constants.PicExtensions.Contains(Path.GetExtension(file).ToLower()))
                    File.Move(file, path + "\\TEMP\\" + ++index + ".jpg");
            }

            // Move files from TEMP folder back to root folder.
            index = 0;
            files = Directory.GetFiles(path + "\\TEMP");
            foreach (var file in files)
                File.Move(file, path + "\\" + ++index + ".jpg");

            // Remove TEMP folder
            Directory.Delete(path + "\\TEMP");

            return true;
        }

        private static int MoveHorizontal(string file, ref object lockHor, DirectoryInfo dirHor, string dirName, int indexHor)
        {
            bool pass = false;

            lock (lockHor)
            {
                try
                {
                    ++indexHor;
                    File.Move(file, $"{dirHor}\\{dirName} Horizontal ({indexHor}).jpg");
                    pass = true;
                }
                catch (IOException ex)
                {
                    if (ex.Message != "Cannot create a file when that file already exists.")
                        throw;
                }
            }

            if (!pass)
                MoveHorizontal(file, ref lockHor, dirHor, dirName, indexHor);

            return indexHor;

        }

        private static int MoveVertical(string file, ref object lockVer, DirectoryInfo dirVer, string dirName, int indexVer)
        {
            bool pass = false;

            lock (lockVer)
            {
                try
                {
                    ++indexVer;
                    File.Move(file, $"{dirVer}\\{dirName} Vertical ({indexVer}).jpg");
                    pass = true;
                }
                catch (IOException ex)
                {
                    if (ex.Message != "Cannot create a file when that file already exists.")
                        throw;
                }
            }

            if (!pass)
                MoveVertical(file, ref lockVer, dirVer, dirName, indexVer);

            return indexVer;
        }

        private static void UncheckReadonlyOnFiles(string path)
        {
            var dir = new DirectoryInfo(path);
            IEnumerable<FileInfo> files = dir.EnumerateFiles();
            foreach (var file in files)
                file.IsReadOnly = false;
        }

        private static void ReorderFiles(string path)
        {
            if (path == string.Empty)
                return;

            if (!CheckFileExtensions(path))
                throw new Exception("File extensions are not all .jpg");

            if (CheckOrder(path))
                return;

            var dirName = Path.GetFileName(path);
            var count = 0;

            Directory.CreateDirectory($"{path}\\Temp");

            foreach (var fileFullName in Directory.EnumerateFiles(path))
                File.Move(fileFullName, $"{path}\\Temp\\temp ({++count}).jpg");

            count = 0;

            foreach (var fileFullName in Directory.EnumerateFiles($"{path}\\Temp"))
                File.Move(fileFullName, $"{path}\\{dirName} ({++count}).jpg");

            Directory.Delete($"{path}\\Temp");
        }

        private static bool CheckFileExtensions(string path)
        {
            if (path == string.Empty)
                throw new Exception("CheckFileExtensions: Path is empty");

            foreach (var fileFullName in Directory.EnumerateFiles(path))
            {
                if (!Path.GetExtension(fileFullName).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool CheckOrder(string path)
        {
            var length = Directory.GetFiles(path).Length;
            var dirName = Path.GetFileName(path);
            return File.Exists($"{path}\\{dirName} (1).jpg") &&
                   File.Exists($"{path}\\{dirName} ({length / 2}).jpg") &&
                   File.Exists($"{path}\\{dirName} ({length}).jpg");
        }

        #endregion


        #region IDisposable Implements

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                var serializer = new XmlSerializer(typeof(PersistedSettings));
                using (var writer = new StreamWriter(Constants.PERSISTED_SETTINGS_PATH))
                {
                    serializer.Serialize(writer, new PersistedSettings() { SELECTED_PATH = SelectedPath, MODE = SelectedMode });
                }
            }

            _disposed = true;
        }

        #endregion


        #region INotifyPropertyChanged Implements

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

    }
}
