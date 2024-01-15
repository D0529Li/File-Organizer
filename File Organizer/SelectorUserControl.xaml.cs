using System.Windows.Controls;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for SelectorUserControl.xaml
    /// </summary>
    public partial class SelectorUserControl : UserControl
    {
        public SelectorUserControl()
        {
            InitializeComponent();
            DataContext = new SelectorViewModel(Constants.DEFAULT_SELECTED_PATH);
        }
    }
}
