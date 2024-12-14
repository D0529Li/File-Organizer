using System.ComponentModel;
using System.Windows;

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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            var viewModel = (MainWindowViewModel)DataContext;
            viewModel.Dispose();
        }
    }
}
