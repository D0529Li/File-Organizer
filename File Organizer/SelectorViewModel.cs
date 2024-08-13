using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace File_Organizer
{
    public enum FilterMode
    {
        PicFilterMode,
        WallpaperMode,
        FolderFilterMode
    }

    public class SelectorViewModel : INotifyPropertyChanged
    {
        #region Fields

        private FilterMode _selectedMode;

        private bool _isStarted = false;

        private ISelector? selector;

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

        public string KeepAllButtonText { get { return SelectedMode == FilterMode.WallpaperMode ? "" : "Keep All"; } }
        public string KeepButtonText { get { return SelectedMode == FilterMode.WallpaperMode ? "Yes" : "Keep"; } }
        public string DropButtonText { get { return SelectedMode == FilterMode.WallpaperMode ? "No" : "Drop"; } }

        public string RemainingCountText
        {
            get
            {
                if (SelectedMode == FilterMode.FolderFilterMode)
                {
                    return $"{selector?.GetRemainingCount()} remaining folders";
                }
                return $"{selector?.GetRemainingCount()} remaining pictures";
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
                return Global.SelectedPath;
            }
            set
            {
                if (value != null)
                    Global.SelectedPath = value;
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
        public ICommand PicFilterModeSelectedCommand { get; set; }
        public ICommand WallpaperModeSelectedCommand { get; set; }
        public ICommand FolderFilterModeSelectedCommand { get; set; }

        #endregion

        public SelectorViewModel()
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
            PicFilterModeSelectedCommand = new DelegateCommand<object>(OnPicFilterModeSelected);
            WallpaperModeSelectedCommand = new DelegateCommand<object>(OnWallpaperModeSelected);
            FolderFilterModeSelectedCommand = new DelegateCommand<object>(OnFolderFilterModeSelected);

            SelectedMode = FilterMode.FolderFilterMode;

            OnPropertyChanged(nameof(DisplayedPath));
        }

        /// <summary>
        /// For Global class to update the path
        /// </summary>
        public void UpdatePath()
        {
            OnPropertyChanged(nameof(DisplayedPath));
        }

        #region Private Methods

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

        private void OnWallpaperModeSelected(object _)
        {
            SelectedMode = FilterMode.WallpaperMode;
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
                case FilterMode.WallpaperMode:
                    selector = new WallpaperSelector(SelectedPath);
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
            RefreshButtons();
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

        private void RefreshButtons()
        {
            OnPropertyChanged(nameof(KeepButtonText));
            OnPropertyChanged(nameof(DropButtonText));
            OnPropertyChanged(nameof(AreExtraButtonsVisible));
        }

        private void RefreshImage()
        {
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(DisplayedPath));
            OnPropertyChanged(nameof(RemainingCountText));
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
