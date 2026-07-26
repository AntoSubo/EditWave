namespace EditWave.Abstractions
{
    public interface IUndoManager
    {
        bool CanUndo();
        bool CanRedo();
        string? Undo();
        string? Redo();
        void PushState(string filePath);
        void Initialize(string filePath);
        void Clear();
        event Action? UndoStateChanged;
    }
}
