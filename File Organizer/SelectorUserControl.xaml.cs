using System.Windows.Controls;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for SelectorUserControl.xaml
    /// </summary>
    public partial class SelectorUserControl : UserControl
    {
        private const string DEFAULT_SELECTED_PATH = "D:\\123\\test";

        public SelectorUserControl()
        {
            InitializeComponent();
            DataContext = new SelectorViewModel(DEFAULT_SELECTED_PATH);
        }
    }
}
