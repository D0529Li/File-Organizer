using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Timers;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace File_Organizer
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Constants

        private const string DEFAULT_SELECTED_PATH = "D:\\123\\test";
        private const string DEFAULT_VIDEO_COLLECTION_PATH = "G:\\123";
        private const string DEFAULT_XML_PATH = "D:\\File Organizer\\File Organizer\\File Organizer\\bin\\Debug\\net8.0-windows\\people.xml";

        #endregion

        #region Fields

        private readonly System.Timers.Timer timer = new System.Timers.Timer(2000);
        private Random random = new Random();
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

        public string SelectedPath { get; set; } = DEFAULT_SELECTED_PATH;
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

        public MainWindowViewModel()
        {
            StartStopButtonCommand = new DelegateCommand<object>(OnStartStopButtonClick);
            SelectDirectoryCommand = new DelegateCommand<object>(OnSelectDirectory);
            OrganizeCommand = new DelegateCommand<object>(OnOrganize);
            UpdatePeopleCommand = new DelegateCommand<object>(OnUpdatePeople);
            DrawCommand = new DelegateCommand<object>(OnDraw);
            MuteButtonClickCommand = new DelegateCommand<object>(OnMuteButtonClick);

            SelectedPath = DEFAULT_SELECTED_PATH;
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
            var writer = new StreamWriter(DEFAULT_XML_PATH, false);
            serializer.Serialize(writer, people);
        }

        private void OnMuteButtonClick(object _)
        {
            IsDisplayedVideoMuted = !IsDisplayedVideoMuted;
            OnPropertyChanged(nameof(IsDisplayedVideoMuted));
            OnPropertyChanged(nameof(MuteButtonText));
        }

        private void Organize(string path)
        {
            if (path == string.Empty)
                return;

            var dirName = Path.GetFileName(path);
            var dirHor = Directory.CreateDirectory($"{path}\\{dirName} 横屏");
            var dirVer = Directory.CreateDirectory($"{path}\\{dirName} 竖屏");
            var numHor = Directory.GetFiles(dirHor.FullName).Length;
            var numVer = Directory.GetFiles(dirVer.FullName).Length;
            var hasPng = false;
            var hasJpeg = false;

            ReorderFiles(dirHor.FullName);
            ReorderFiles(dirVer.FullName);

            foreach (var fileFullName in Directory.EnumerateFiles(path))
            {
                var fileName = Path.GetFileName(fileFullName);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileFullName);
                var fileExtension = Path.GetExtension(fileFullName).ToLower();
                bool isHorizontal = true;

                if (fileExtension != ".jpg")
                {
                    if (fileExtension == ".png")
                        hasPng = true;
                    else if (fileExtension == ".jpeg")
                        hasJpeg = true;
                    continue;
                }

                // must dispose the bitmap before moving the file
                using (var bitmap = new Bitmap(fileFullName))
                {
                    if (bitmap.Height > bitmap.Width)
                        isHorizontal = false;
                }

                if (isHorizontal)
                    File.Move(fileFullName, $"{dirHor}\\{dirName} 横屏 ({++numHor}).jpg");
                else
                    File.Move(fileFullName, $"{dirVer}\\{dirName} 竖屏 ({++numVer}).jpg");
            }

            if (hasPng)
            {
                RenameFileExtensions(path, ".png", ".jpg");
                Organize(path);
            }
            if (hasJpeg)
            {
                RenameFileExtensions(path, ".jpeg", ".jpg");
                Organize(path);
            }

            // logic can be modified.
        }

        private void OnOrganize(object _)
        {
            Task.Run(() => Organize(SelectedPath));
        }

        private static void RenameFileExtensions(string path, string inExtension, string outExtension)
        {
            if (path == string.Empty)
                return;

            foreach (var fileFullName in Directory.EnumerateFiles(path))
            {
                if (Path.GetExtension(fileFullName) == inExtension)
                    File.Move(fileFullName, $"{path}\\{Path.GetFileNameWithoutExtension(fileFullName)}{outExtension}");
            }
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
            DisplayedImagePath = files[random.Next(count)];
            ++current;
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(DisplayedImagePath));
        }

        private bool Deserialize()
        {
            if (!File.Exists(DEFAULT_XML_PATH))
                return false;

            var serializer = new XmlSerializer(typeof(People));
            using (var reader = new StreamReader(DEFAULT_XML_PATH))
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
                if (MessageBox.Show("No people.xml file found. Update people.xml?",
                                    "Update people.xml", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    UpdatePeople();
                else
                    return;
            }
            
            var drawnPerson = people.DrawPerson();

            if (drawnPerson.IsFolder)
            {
                var random = new Random();
                var index = random.Next(drawnPerson.NumOfFiles);
                DisplayedVideoPath = Directory.GetFiles(DEFAULT_VIDEO_COLLECTION_PATH + "\\" + drawnPerson.Name)[index];
            }
            else
            {
                DisplayedVideoPath = DEFAULT_VIDEO_COLLECTION_PATH + "\\" + drawnPerson.Name + ".mp4";
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
