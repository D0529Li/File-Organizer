using System.Windows.Controls;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for PictureSelectorUserControl.xaml
    /// </summary>
    public partial class PictureSelectorUserControl : UserControl
    {
        private const string DEFAULT_SELECTED_PATH = "D:\\123\\test";


        public PictureSelectorUserControl()
        {
            InitializeComponent();
            DataContext = new PictureSelectorViewModel(DEFAULT_SELECTED_PATH);
        }
    }
}
