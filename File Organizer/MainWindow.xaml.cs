using System.Windows;
using System.Windows.Controls;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void OpenPictureSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new PictureSelector();
            window.Show();

            // VideoControl.
        }
    }
}
