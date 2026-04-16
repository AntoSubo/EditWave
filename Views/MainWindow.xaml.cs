using EditWave.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EditWave.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(); // перестать забывать про эту строку, без нее ниче не робит
        }

        public void OnWaveformSelectionChanged(double startSeconds, double endSeconds) // подумать как бы убрать ее отсюда
        {
           
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SelectionStart = startSeconds;
                viewModel.SelectionEnd = endSeconds;
            }
        }

        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is int projectId)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.DeleteProject(projectId);
                }
            }
        }
    
        //private void MainWindow_Closing(object parameter, System.ComponentModel.CancelEventArgs e)
        //{
        //    if (DataContext is MainViewModel viewModel)
        //    {
        //        viewModel.Clean();
        //    }
        //}
    }
}

//TODO сделать окно красивее, функциональнее, лаконичнее