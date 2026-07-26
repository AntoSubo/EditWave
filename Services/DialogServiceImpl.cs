using EditWave.Abstractions;
using EditWave.Views;
using Microsoft.Win32;
using System.Windows;

namespace EditWave.Services
{
    public class WpfDialogService : IDialogService
    {
        public string? ShowInputDialog(string prompt, string title, string defaultValue)
        {
            return Views.DialogService.ShowInputDialog(prompt, title, defaultValue);
        }

        public string? ShowSaveFileDialog(string filter, string title)
        {
            var dialog = new SaveFileDialog { Filter = filter, Title = title };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowOpenFileDialog(string filter, string title)
        {
            var dialog = new OpenFileDialog { Filter = filter, Title = title };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowMessage(string message, string title = "", DialogMessageImage image = DialogMessageImage.Information)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, ToMessageBoxImage(image));
        }

        public bool ShowConfirmation(string message, string title = "Подтверждение")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private static MessageBoxImage ToMessageBoxImage(DialogMessageImage image) => image switch
        {
            DialogMessageImage.Information => MessageBoxImage.Information,
            DialogMessageImage.Warning => MessageBoxImage.Warning,
            DialogMessageImage.Error => MessageBoxImage.Error,
            DialogMessageImage.Question => MessageBoxImage.Question,
            _ => MessageBoxImage.None
        };
    }
}
