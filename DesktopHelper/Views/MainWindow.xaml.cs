using DesktopHelper.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace DesktopHelper.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TaskListGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.SaveTasks();
            }
        }
    }
}