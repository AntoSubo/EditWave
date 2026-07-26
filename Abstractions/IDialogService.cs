namespace EditWave.Abstractions
{
    public enum DialogMessageImage
    {
        None,
        Information,
        Warning,
        Error,
        Question
    }

    public interface IDialogService
    {
        string? ShowInputDialog(string prompt, string title, string defaultValue);
        string? ShowSaveFileDialog(string filter, string title);
        string? ShowOpenFileDialog(string filter, string title);
        void ShowMessage(string message, string title = "", DialogMessageImage image = DialogMessageImage.Information);
        bool ShowConfirmation(string message, string title = "Подтверждение");
    }
}
