using System.Windows;
using EditWave.Abstractions;
using EditWave.Services;
using EditWave.ViewModels;
using EditWave.Views;
using NAudio.MediaFoundation;

namespace EditWave
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MediaFoundationApi.Startup();

            var context = new AudioContext();
            var engine = new AudioEngine(context);
            var editor = new AudioEditor(context);
            var undoManager = new UndoManager(context);
            var exporter = new AudioExporter(context, engine);
            var dialogService = new WpfDialogService();
            var projectService = new ProjectService();

            var viewModel = new MainViewModel(
                engine, editor, undoManager, exporter,
                editor, dialogService, projectService);

            var mainWindow = new MainWindow(viewModel);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            MediaFoundationApi.Shutdown();
            base.OnExit(e);
        }
    }
}
