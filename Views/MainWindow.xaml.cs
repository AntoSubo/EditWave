using EditWave.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EditWave.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            this.Closing += MainWindow_Closing;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                if (DataContext is MainViewModel vm) vm.UndoCommand?.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
            {
                if (DataContext is MainViewModel vm) vm.RedoCommand?.Execute(null);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Clean();
        }

        public void OnWaveformSelectionChanged(double startSeconds, double endSeconds)
        {
            _viewModel.SelectionStart = startSeconds;
            _viewModel.SelectionEnd = endSeconds;
        }

        private void ProjectListBox_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is EditWave.Models.Project project)
            {
                _viewModel.LoadProject(project);
            }
        }
    }
}