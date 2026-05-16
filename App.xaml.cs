using System.Windows;
using NAudio.MediaFoundation;

namespace EditWave
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MediaFoundationApi.Startup();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            MediaFoundationApi.Shutdown();
            base.OnExit(e);
        }
    }
}