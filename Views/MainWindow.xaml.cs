using EditWave.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EditWave.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            this.Closing += MainWindow_Closing;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
            {
                _viewModel.OpenProjectCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                _viewModel.SaveProjectCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                _viewModel.ExportCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.T)
            {
                _viewModel.TrimCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                _viewModel.DeleteCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                _viewModel.UndoCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
            {
                _viewModel.RedoCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.G)
            {
                _viewModel.ApplyGainCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                _viewModel.ApplyReverseCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                _viewModel.SeekBy(-5);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                _viewModel.SeekBy(5);
                e.Handled = true;
            }
            else if (e.Key == Key.F1)
            {
                _viewModel.ShowAboutCommand.Execute(null);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show("Есть несохранённые изменения. Сохранить перед выходом?", "Выход", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.SaveProjectCommand.Execute(null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            _viewModel.Clean();
        }

        public void OnWaveformSelectionChanged(double startSeconds, double endSeconds)
        {
            _viewModel.SelectionStart = startSeconds;
            _viewModel.SelectionEnd = endSeconds;
        }

    }
}