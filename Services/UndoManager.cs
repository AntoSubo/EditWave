using System.IO;
using EditWave.Abstractions;

namespace EditWave.Services
{
    public class UndoManager : IUndoManager
    {
        private readonly AudioContext _context;
        private readonly List<string> _undoStack = new();
        private int _undoIndex = -1;
        private const int MaxUndo = 5;

        public event Action? UndoStateChanged;

        public UndoManager(AudioContext context)
        {
            _context = context;
        }

        public bool CanUndo() => _undoIndex > 0;
        public bool CanRedo() => _undoIndex < _undoStack.Count - 1;

        public void PushState(string filePath)
        {
            if (_undoIndex < _undoStack.Count - 1)
            {
                for (int i = _undoStack.Count - 1; i > _undoIndex; i--)
                {
                    try { File.Delete(_undoStack[i]); } catch { }
                }
                _undoStack.RemoveRange(_undoIndex + 1, _undoStack.Count - _undoIndex - 1);
            }

            _undoStack.Add(filePath);
            _undoIndex = _undoStack.Count - 1;

            while (_undoStack.Count > MaxUndo)
            {
                try { File.Delete(_undoStack[0]); } catch { }
                _undoStack.RemoveAt(0);
                _undoIndex = _undoStack.Count - 1;
            }
            UndoStateChanged?.Invoke();
        }

        public string? Undo()
        {
            if (!CanUndo()) return null;
            _undoIndex--;
            string prevFile = _undoStack[_undoIndex];
            UndoStateChanged?.Invoke();
            return File.Exists(prevFile) ? prevFile : null;
        }

        public string? Redo()
        {
            if (!CanRedo()) return null;
            _undoIndex++;
            string nextFile = _undoStack[_undoIndex];
            UndoStateChanged?.Invoke();
            return File.Exists(nextFile) ? nextFile : null;
        }

        public void Initialize(string filePath)
        {
            foreach (var file in _undoStack)
            {
                try { File.Delete(file); } catch { }
            }
            _undoStack.Clear();
            _undoIndex = -1;

            string initCopy = _context.CreateTempFilePath();
            File.Copy(filePath, initCopy, true);
            _undoStack.Add(initCopy);
            _undoIndex = 0;
        }

        public void Clear()
        {
            foreach (var file in _undoStack)
            {
                try { if (File.Exists(file)) File.Delete(file); } catch { }
            }
            _undoStack.Clear();
            _undoIndex = -1;
        }
    }
}
