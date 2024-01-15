using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Serialization;

namespace File_Organizer
{
    public class OrganizerViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly System.Timers.Timer timer = new System.Timers.Timer(2000);
        private int total = 0;
        private int current = 0;
        private People people = new People();
        private bool isStarted = false;

        #endregion

        #region Properties

        public string Progress
        {
            get { return $"{current} / {total} {Math.Round((double)current * 100 / total, 2)}%"; }
        }

        public string SelectedPath { get; set; } = Constants.DEFAULT_SELECTED_PATH;
        public string DisplayedImagePath { get; set; } = string.Empty;
        public string DisplayedVideoPath { get; set; } = string.Empty;
        public string DisplayedVideoName
        {
            get { return Path.GetFileNameWithoutExtension(DisplayedVideoPath); }
        }
        public string MuteButtonText
        {
            get { return IsDisplayedVideoMuted ? "Unmute" : "Mute"; }
        }
        public string StartStopButtonText
        {
            get { return isStarted ? "Stop" : "Start"; }
        }

        public bool IsProgressBarVisible { get; set; } = false;
        public bool IsMuteButtonVisible { get; set; } = false;
        public bool IsDisplayedVideoMuted { get; set; } = true;

        #endregion

        #region Commands
        public ICommand StartStopButtonCommand { get; set; }
        public ICommand SelectDirectoryCommand { get; set; }
        public ICommand OrganizeCommand { get; set; }
        public ICommand UpdatePeopleCommand { get; set; }
        public ICommand DrawCommand { get; set; }
        public ICommand MuteButtonClickCommand { get; set; }

        #endregion

        public OrganizerViewModel()
        {
            StartStopButtonCommand = new DelegateCommand<object>(OnStartStopButtonClick);
            SelectDirectoryCommand = new DelegateCommand<object>(OnSelectDirectory);
            OrganizeCommand = new DelegateCommand<object>(OnOrganize);
            UpdatePeopleCommand = new DelegateCommand<object>(OnUpdatePeople);
            DrawCommand = new DelegateCommand<object>(OnDraw);
            MuteButtonClickCommand = new DelegateCommand<object>(OnMuteButtonClick);

            SelectedPath = Constants.DEFAULT_SELECTED_PATH;
            timer.Elapsed += Timer_Elapsed;

            OnPropertyChanged(nameof(SelectedPath));
        }


        #region Private Methods

        private void OnStartStopButtonClick(object _)
        {
            if (!isStarted)
            {
                total = Directory.GetFiles(SelectedPath).Length;
                timer.Start();
                IsProgressBarVisible = true;
                isStarted = true;
                OnPropertyChanged(nameof(IsProgressBarVisible));
                OnPropertyChanged(nameof(StartStopButtonText));
            }
            else
            {
                timer.Stop();
                IsProgressBarVisible = false;
                isStarted = false;
                OnPropertyChanged(nameof(IsProgressBarVisible));
                OnPropertyChanged(nameof(StartStopButtonText));
            }

        }

        private void OnSelectDirectory(object _)
        {
            var dialog = new FolderBrowserDialog() { SelectedPath = SelectedPath };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedPath = dialog.SelectedPath;
                OnPropertyChanged(nameof(SelectedPath));
            }
        }

        private void OnUpdatePeople(object _)
        {
            UpdatePeople();
        }

        private void UpdatePeople()
        {
            var serializer = new XmlSerializer(typeof(People));
            var people = new People();
            people.UpdatePeople();
            var writer = new StreamWriter(Constants.DEFAULT_XML_PATH, false);
            serializer.Serialize(writer, people);
        }

        private void OnMuteButtonClick(object _)
        {
            IsDisplayedVideoMuted = !IsDisplayedVideoMuted;
            OnPropertyChanged(nameof(IsDisplayedVideoMuted));
            OnPropertyChanged(nameof(MuteButtonText));
        }

        private async Task OrganizeAsync(string path, int numOfThread = 10)
        {
            // Todo: Add progress bar

            if (path == string.Empty)
                return;

            var dirName = Path.GetFileName(path);
            var dirHor = Directory.CreateDirectory($"{path}\\{dirName} 横屏");
            var dirVer = Directory.CreateDirectory($"{path}\\{dirName} 竖屏");
            var indexHor = Directory.GetFiles(dirHor.FullName).Length;
            var indexVer = Directory.GetFiles(dirVer.FullName).Length;
            var lockHor = new object();
            var lockVer = new object();

            // Prep: Reorder existing files.
            var task1 = Task.Run(() => ReorderFiles(dirHor.FullName));
            var task2 = Task.Run(() => ReorderFiles(dirVer.FullName));
            await Task.WhenAll(task1, task2);

            // Prep: Rename all files to .jpg
            RenamePicExtensions(path);

            // Organize multi-threaded
            var files = Directory.GetFiles(path);
            Parallel.ForEach(files, file =>
            {
                var stream = File.OpenRead(file);
                var image = Image.FromStream(stream, false, false);
                stream.Close();
                if (image.Width > image.Height)
                    lock (lockHor)
                        File.Move(file, $"{dirHor}\\{dirName} 横屏 ({++indexHor}).jpg");
                else
                    lock (lockVer)
                        File.Move(file, $"{dirVer}\\{dirName} 竖屏 ({++indexVer}).jpg");
                image.Dispose();
            });

            //// Organize single-threaded
            //foreach (var fileFullName in Directory.EnumerateFiles(path))
            //{
            //    var stream = File.OpenRead(fileFullName);
            //    var image = Image.FromStream(stream, false, false);
            //    stream.Close();

            //    if (image.Width > image.Height)
            //        File.Move(fileFullName, $"{dirHor}\\{dirName} 横屏 ({++indexHor}).jpg");
            //    else
            //        File.Move(fileFullName, $"{dirVer}\\{dirName} 竖屏 ({++indexVer}).jpg");
            //    image.Dispose();
            //}
        }

        private void OnOrganize(object _)
        {
            Task.Run(() => OrganizeAsync(SelectedPath));
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
                if (Path.GetExtension(fileFullName).ToLower() != ".jpg")
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

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            var files = Directory.GetFiles(SelectedPath);
            var count = files.Length;
            var random = new Random();
            DisplayedImagePath = files[random.Next(count)];
            ++current;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayedImagePath));
        }

        private bool Deserialize()
        {
            if (!File.Exists(Constants.DEFAULT_XML_PATH))
                return false;

            var serializer = new XmlSerializer(typeof(People));
            using (var reader = new StreamReader(Constants.DEFAULT_XML_PATH))
            {
                var deserializedPeople = serializer.Deserialize(reader);
                if (deserializedPeople is null)
                    return false;
                people = (People)deserializedPeople;
            }
            return true;
        }

        private void OnDraw(object _)
        {
            if (!Deserialize())
            {
                if (System.Windows.MessageBox.Show("No people.xml file found. Update people.xml?",
                                    "Update people.xml", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    UpdatePeople();
                else
                    return;
            }

            var drawnPerson = people.DrawPerson();

            if (drawnPerson.IsFolder)
            {
                var random = new Random();
                var index = random.Next(drawnPerson.NumOfFiles);
                DisplayedVideoPath = Directory.GetFiles(Constants.DEFAULT_VIDEO_COLLECTION_PATH + "\\" + drawnPerson.Name)[index];
            }
            else
            {
                DisplayedVideoPath = Constants.DEFAULT_VIDEO_COLLECTION_PATH + "\\" + drawnPerson.Name + ".mp4";
            }
            IsMuteButtonVisible = true;
            OnPropertyChanged(nameof(IsMuteButtonVisible));
            OnPropertyChanged(nameof(DisplayedVideoPath));
            OnPropertyChanged(nameof(DisplayedVideoName));
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
