using System.Windows;
using System.Windows.Controls;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for PictureSelector.xaml
    /// </summary>
    public partial class PictureSelector : Window
    {
        private const string DEFAULT_SELECTED_PATH = "D:\\123\\test";

        public PictureSelector(string selectedPath = DEFAULT_SELECTED_PATH)
        {
            InitializeComponent();
            DataContext = new PictureSelectorViewModel(selectedPath);
        }
    }
}
