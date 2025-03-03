using DesktopHelper.ViewModels;
using System.Windows;

namespace DesktopHelper.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}