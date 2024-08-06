using System.Windows.Controls;

namespace File_Organizer
{
    /// <summary>
    /// Interaction logic for OrganizerUserControl.xaml
    /// </summary>
    public partial class OrganizerUserControl : UserControl
    {
        public OrganizerUserControl()
        {
            InitializeComponent();
            DataContext = Global.OrganizerViewModel = new OrganizerViewModel();
        }
    }
}
