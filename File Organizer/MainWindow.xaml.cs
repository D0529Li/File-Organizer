using System.Windows;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DEFAULT_SELECTED_PATH = "D:\\123\\test";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new OrganizerViewModel();
        }
    }
}
