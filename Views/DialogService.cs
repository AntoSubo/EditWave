using System.Windows;

namespace EditWave.Views
{
    public static class DialogService
    {
        public static string ShowInputDialog(string prompt, string title, string defaultValue = "")
        {
            var dialog = new InputDialog(prompt, title, defaultValue)
            {
                Owner = Application.Current.MainWindow
            };
            return dialog.ShowDialog() == true ? dialog.InputText : null;
        }
    }
}
