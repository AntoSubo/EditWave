using EditWave.Abstractions;
using EditWave.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace EditWave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IAudioEngine _engine;
        private readonly IAudioEditor _editor;
        private readonly IUndoManager _undo;
        private readonly IFileExporter _exporter;
        private readonly IWaveformProvider _waveformProvider;
        private readonly IDialogService _dialog;
        private readonly IProjectService _projectService;

        private string _currentTime = "0";
        private double _currentPosition;
        private double _currentPositionNormalized;
        private double _duration;
        private double _volume = 1.0;
        private double _gain;
        private ObservableCollection<Project> _projectsList;
        private Project? _selectedProject;
        private double _selectionStart;
        private double _selectionEnd;
        private float[]? _waveformSamples;
        private bool _isProcessing;

        public bool IsPlaying => _engine.IsPlaying;
        public string FormatInfo => _engine.FormatInfo;
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Project> ProjectsList
        {
            get => _projectsList;
            set { _projectsList = value; OnPropertyChanged(); }
        }
        private bool _canUndo;
        public bool CanUndo
        {
            get => _canUndo;
            set { _canUndo = value; OnPropertyChanged(); }
        }
        private bool _canRedo;
        public bool CanRedo
        {
            get => _canRedo;
            set { _canRedo = value; OnPropertyChanged(); }
        }
        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set { _hasUnsavedChanges = value; OnPropertyChanged(); }
        }

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged();
                if (value != null) LoadProject(value);
            }
        }

        public double SelectionStart
        {
            get => _selectionStart;
            set { _selectionStart = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectionDuration)); }
        }

        public double SelectionEnd
        {
            get => _selectionEnd;
            set { _selectionEnd = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectionDuration)); }
        }

        public string SelectionDuration
        {
            get
            {
                if (SelectionStart >= SelectionEnd) return "";
                double duration = SelectionEnd - SelectionStart;
                return $"Выделение: {TimeSpan.FromSeconds(duration):mm\\:ss\\.ff}";
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set { if (_currentTime != value) { _currentTime = value; OnPropertyChanged(); } }
        }

        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    OnPropertyChanged();
                    _engine.SetPosition(value);
                }
            }
        }

        public double CurrentPositionNormalized
        {
            get => _currentPositionNormalized;
            set { _currentPositionNormalized = value; OnPropertyChanged(); }
        }

        public double Duration
        {
            get => _duration;
            set { if (_duration != value) { _duration = value; OnPropertyChanged(); } }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged();
                    _engine.SetVolume((float)value);
                }
            }
        }

        public double Gain
        {
            get => _gain;
            set { if (_gain != value) { _gain = value; OnPropertyChanged(); } }
        }

        private double _speed = 1.0;
        public double Speed
        {
            get => _speed;
            set { if (_speed != value) { _speed = value; OnPropertyChanged(); } }
        }

        private double _pitch = 1.0;
        public double Pitch
        {
            get => _pitch;
            set { if (_pitch != value) { _pitch = value; OnPropertyChanged(); } }
        }

        private double _bassGain;
        public double BassGain
        {
            get => _bassGain;
            set { if (_bassGain != value) { _bassGain = value; OnPropertyChanged(); } }
        }

        private double _trebleGain;
        public double TrebleGain
        {
            get => _trebleGain;
            set { if (_trebleGain != value) { _trebleGain = value; OnPropertyChanged(); } }
        }

        public float[]? WaveformSamples
        {
            get => _waveformSamples;
            set { _waveformSamples = value; OnPropertyChanged(); }
        }

        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand TrimCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ApplyGainCommand { get; }
        public ICommand ApplyReverseCommand { get; }
        public ICommand ApplyFadeInCommand { get; }
        public ICommand ApplyFadeOutCommand { get; }
        public ICommand ApplyNormalizeCommand { get; }
        public ICommand ApplySpeedCommand { get; }
        public ICommand ApplyPitchCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand RenameProjectCommand { get; }
        public ICommand ToggleLoopCommand { get; }
        public ICommand InsertSilenceCommand { get; }
        public ICommand CopySelectionCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand EditMetadataCommand { get; }
        public ICommand TrimSilenceCommand { get; }
        public ICommand ApplyEQCommand { get; }
        public ICommand ToMonoCommand { get; }
        public ICommand ToStereoCommand { get; }

        private bool _isLoopEnabled;
        public bool IsLoopEnabled
        {
            get => _isLoopEnabled;
            set { _isLoopEnabled = value; OnPropertyChanged(); }
        }

        public MainViewModel(
            IAudioEngine engine,
            IAudioEditor editor,
            IUndoManager undo,
            IFileExporter exporter,
            IWaveformProvider waveformProvider,
            IDialogService dialog,
            IProjectService projectService)
        {
            _engine = engine;
            _editor = editor;
            _undo = undo;
            _exporter = exporter;
            _waveformProvider = waveformProvider;
            _dialog = dialog;
            _projectService = projectService;

            _engine.PositionChanged += OnPositionChanged;
            _engine.PlaybackStopped += OnPlaybackStopped;
            _undo.UndoStateChanged += UpdateUndoRedoState;
            _engine.SetVolume((float)_volume);
            UpdateUndoRedoState();

            PlayCommand = new RelayCommand(Play);
            PauseCommand = new RelayCommand(Pause);
            StopCommand = new RelayCommand(Stop);
            TrimCommand = new RelayCommand(Trim);
            DeleteCommand = new RelayCommand(Delete);
            ApplyGainCommand = new RelayCommand(ApplyGain);
            ApplyReverseCommand = new RelayCommand(ApplyReverse);
            ApplyFadeInCommand = new RelayCommand(ApplyFadeIn);
            ApplyFadeOutCommand = new RelayCommand(ApplyFadeOut);
            ApplyNormalizeCommand = new RelayCommand(ApplyNormalize);
            ApplySpeedCommand = new RelayCommand(ApplySpeed);
            ApplyPitchCommand = new RelayCommand(ApplyPitch);
            SaveProjectCommand = new RelayCommand(SaveProject);
            UndoCommand = new RelayCommand(Undo);
            RedoCommand = new RelayCommand(Redo);
            OpenProjectCommand = new RelayCommand(OpenProject);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ExitCommand = new RelayCommand(Exit);
            ExportCommand = new RelayCommand(ExportAudio);
            DeleteProjectCommand = new RelayCommand(DeleteProject);
            RenameProjectCommand = new RelayCommand(RenameProject);
            ToggleLoopCommand = new RelayCommand(_ => IsLoopEnabled = !IsLoopEnabled);
            InsertSilenceCommand = new RelayCommand(InsertSilence);
            CopySelectionCommand = new RelayCommand(CopySelection);
            PasteCommand = new RelayCommand(Paste);
            EditMetadataCommand = new RelayCommand(EditMetadata);
            TrimSilenceCommand = new RelayCommand(TrimSilence);
            ApplyEQCommand = new RelayCommand(ApplyEQ);
            ToMonoCommand = new RelayCommand(ToMono);
            ToStereoCommand = new RelayCommand(ToStereo);

            _projectsList = new ObservableCollection<Project>();
            LoadProjectsFromDb();
        }

        private void UpdateUndoRedoState()
        {
            CanUndo = _undo.CanUndo();
            CanRedo = _undo.CanRedo();
        }

        private void InitializeUndoStack(string filePath, bool isTemporary)
        {
            if (!isTemporary || (!_undo.CanUndo() && !_undo.CanRedo()))
                _undo.Initialize(filePath);
        }

        public void SeekBy(double seconds)
        {
            double newPos = CurrentPosition + seconds;
            if (newPos < 0) newPos = 0;
            if (newPos > Duration) newPos = Duration;
            CurrentPosition = newPos;
        }

        public void LoadAudioFromPath(string path, string displayName)
        {
            if (_engine.LoadFile(path))
            {
                InitializeUndoStack(_engine.GetCurrentFilePath(), _engine.IsTemporaryFile());
                Duration = _engine.Duration;
                CurrentPosition = 0;
                CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
                LoadWaveform();
                OnPropertyChanged(nameof(FormatInfo));
                HasUnsavedChanges = false;
                _dialog.ShowMessage(displayName);
            }
            else _dialog.ShowMessage("Не удалось загрузить файл");
        }

        private async Task ExecuteEdit(Func<Task<string?>> operation, string confirmMessage, string? successMessage = null)
        {
            if (IsProcessing) return;
            if (!_dialog.ShowConfirmation(confirmMessage)) return;
            IsProcessing = true;
            try
            {
                _engine.Stop();
                string? tempFile = await Task.Run(operation);
                if (tempFile != null)
                {
                    _undo.PushState(tempFile);
                    _engine.LoadFile(tempFile, isTemporary: true);
                    Duration = _engine.Duration;
                    LoadWaveform();
                    HasUnsavedChanges = true;
                    if (successMessage != null) _dialog.ShowMessage(successMessage);
                }
                SelectionStart = 0;
                SelectionEnd = 0;
            }
            catch (Exception ex)
            {
                _dialog.ShowMessage($"Ошибка: {ex.Message}", "Ошибка", DialogMessageImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task NavigateHistory(Func<string?> navigateAction)
        {
            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                _engine.Stop();
                string? file = await Task.Run(navigateAction);
                if (file != null)
                {
                    _engine.LoadFile(file, isTemporary: true);
                    Duration = _engine.Duration;
                    CurrentPosition = 0;
                    LoadWaveform();
                    CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
                }
            }
            catch (Exception ex)
            {
                _dialog.ShowMessage($"Ошибка: {ex.Message}", "Ошибка", DialogMessageImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void OnPositionChanged()
        {
            _currentPosition = _engine.CurrentPosition;
            OnPropertyChanged(nameof(CurrentPosition));
            CurrentTime = $"{TimeSpan.FromSeconds(CurrentPosition):mm\\:ss}/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
            if (Duration > 0) CurrentPositionNormalized = CurrentPosition / Duration;

            if (IsLoopEnabled && SelectionStart < SelectionEnd && _engine.IsPlaying)
            {
                if (_currentPosition >= SelectionEnd)
                {
                    _engine.SetPosition(SelectionStart);
                }
            }
        }

        private void OnPlaybackStopped()
        {
            OnPropertyChanged(nameof(IsPlaying));
        }

        private void Play(object parameter) => _engine.Play();
        private void Pause(object parameter) => _engine.Pause();
        private void Stop(object parameter)
        {
            _engine.Stop();
            CurrentPosition = 0;
            CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
        }

        private async void Trim(object parameter)
        {
            if (SelectionStart >= SelectionEnd)
            {
                _dialog.ShowMessage("Сначала выделите фрагмент");
                return;
            }
            double start = SelectionStart, end = SelectionEnd;
            await ExecuteEdit(
                () => Task.FromResult(_editor.Trim(start, end)),
                "Обрезать выделенный фрагмент?",
                "Фрагмент обрезан");
        }

        private async void Delete(object parameter)
        {
            if (SelectionStart >= SelectionEnd)
            {
                _dialog.ShowMessage("Сначала выделите фрагмент");
                return;
            }
            double start = SelectionStart, end = SelectionEnd;
            await ExecuteEdit(
                () => Task.FromResult(_editor.DeleteSelection(start, end)),
                "Удалить выделенный фрагмент?",
                "Фрагмент удалён");
        }

        private async void ApplyGain(object parameter)
        {
            float gainFactor = (float)(Gain / 100.0);
            if (SelectionStart < SelectionEnd)
            {
                double start = SelectionStart, end = SelectionEnd;
                await ExecuteEdit(
                    () => Task.FromResult(_editor.ApplyGainToSelection(gainFactor, start, end)),
                    "Применить усиление к аудио?",
                    $"Усиление применено: {Gain}%");
            }
            else
            {
                await ExecuteEdit(
                    () => Task.FromResult(_editor.ApplyGain(gainFactor)),
                    "Применить усиление к аудио?",
                    $"Усиление применено: {Gain}%");
            }
        }

        private async void ApplyReverse(object parameter)
        {
            if (SelectionStart < SelectionEnd)
            {
                double start = SelectionStart, end = SelectionEnd;
                await ExecuteEdit(
                    () => Task.FromResult(_editor.ApplyReverseToSelection(start, end)),
                    "Применить реверс к аудио?",
                    "Реверс применён");
            }
            else
            {
                await ExecuteEdit(
                    () => Task.FromResult(_editor.ApplyReverse()),
                    "Применить реверс к аудио?",
                    "Реверс применён");
            }
        }

        private async void ApplyFadeIn(object parameter)
        {
            double start = SelectionStart < SelectionEnd ? SelectionStart : 0;
            double end = SelectionStart < SelectionEnd ? SelectionEnd : _engine.Duration;
            await ExecuteEdit(
                () => Task.FromResult(_editor.ApplyFadeIn(start, end)),
                "Применить плавное начало?",
                "Плавное начало применено");
        }

        private async void ApplyFadeOut(object parameter)
        {
            double start = SelectionStart < SelectionEnd ? SelectionStart : 0;
            double end = SelectionStart < SelectionEnd ? SelectionEnd : _engine.Duration;
            await ExecuteEdit(
                () => Task.FromResult(_editor.ApplyFadeOut(start, end)),
                "Применить плавное затухание?",
                "Плавное затухание применено");
        }

        private async void ApplyNormalize(object parameter)
        {
            if (SelectionStart < SelectionEnd)
            {
                double start = SelectionStart, end = SelectionEnd;
                await ExecuteEdit(
                    () => Task.FromResult(_editor.ApplyNormalizeToSelection(start, end)),
                    "Нормализовать выделение?",
                    "Нормализация применена");
            }
            else
            {
                await ExecuteEdit(
                    () => Task.FromResult(_editor.ApplyNormalize()),
                    "Нормализовать всё аудио?",
                    "Нормализация применена");
            }
        }

        private async void ApplySpeed(object parameter)
        {
            float speedFactor = (float)Speed;
            await ExecuteEdit(
                () => Task.FromResult(_editor.ApplySpeed(speedFactor)),
                $"Изменить скорость на {Speed:F1}x?",
                $"Скорость изменена: {Speed:F1}x");
        }

        private async void ApplyPitch(object parameter)
        {
            float pitchFactor = (float)Pitch;
            await ExecuteEdit(
                () => Task.FromResult(_editor.ApplyPitch(pitchFactor)),
                $"Изменить тон на {Pitch:F2}x?",
                $"Тон изменён: {Pitch:F2}x");
        }

        private void ExportAudio(object parameter)
        {
            if (string.IsNullOrEmpty(_engine.GetCurrentFilePath()))
            {
                _dialog.ShowMessage("Нет аудио для экспорта");
                return;
            }
            string? filePath = _dialog.ShowSaveFileDialog("WAV файлы|*.wav|MP3 файлы|*.mp3", "Экспорт аудио");
            if (filePath == null) return;

            try
            {
                bool hasSelection = SelectionStart < SelectionEnd;
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".mp3")
                {
                    string? bitrateStr = _dialog.ShowInputDialog("Битрейт MP3 (kbps):", "Экспорт", "192");
                    if (bitrateStr == null || !int.TryParse(bitrateStr, out int bitrate)) bitrate = 192;

                    if (hasSelection)
                        _exporter.ExportSelected(filePath, SelectionStart, SelectionEnd, bitrate);
                    else
                        _exporter.Export(filePath, bitrate);
                }
                else
                {
                    if (hasSelection)
                        _exporter.ExportSelected(filePath, SelectionStart, SelectionEnd);
                    else
                        _exporter.Export(filePath);
                }
                _dialog.ShowMessage("Экспорт завершён");
            }
            catch (Exception ex)
            {
                _dialog.ShowMessage($"Ошибка: {ex.Message}", "Ошибка", DialogMessageImage.Error);
            }
        }

        private void SaveProject(object parameter)
        {
            if (string.IsNullOrEmpty(_engine.GetCurrentFilePath()))
            {
                _dialog.ShowMessage("Сначала загрузите аудиофайл", "Ошибка", DialogMessageImage.Warning);
                return;
            }
            string? projectName = _dialog.ShowInputDialog("Введите название проекта:", "Сохранение проекта", "Мой проект");
            if (projectName == null || string.IsNullOrWhiteSpace(projectName)) return;

            string currentFilePath = _engine.GetCurrentFilePath();
            string projectFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");
            Directory.CreateDirectory(projectFolder);
            string savePath = Path.Combine(projectFolder, projectName + ".wav");

            try
            {
                var existingProject = _projectService.GetAllProjects().FirstOrDefault(p => p.Name == projectName);
                if (existingProject != null)
                {
                    if (!_dialog.ShowConfirmation($"Проект \"{projectName}\" уже существует. Перезаписать?"))
                        return;
                    existingProject.FilePath = savePath;
                    existingProject.LastModified = DateTime.Now;
                    _projectService.SaveProject(existingProject);
                }
                else
                {
                    var project = new Project { Name = projectName, FilePath = savePath, LastModified = DateTime.Now };
                    _projectService.SaveProject(project);
                }
                if (currentFilePath != savePath)
                    File.Copy(currentFilePath, savePath, true);

                LoadProjectsFromDb();
                HasUnsavedChanges = false;
                _dialog.ShowMessage($"Проект \"{projectName}\" сохранён!", "Успех");
            }
            catch (Exception ex)
            {
                _dialog.ShowMessage($"Ошибка сохранения: {ex.Message}", "Ошибка", DialogMessageImage.Error);
            }
        }

        private void OpenProject(object parameter)
        {
            string? filePath = _dialog.ShowOpenFileDialog("Аудио файлы|*.wav;*.mp3|Все файлы|*.*", "Открытие файла");
            if (filePath != null)
                LoadAudioFromPath(filePath, $"Файл загружен: {Path.GetFileName(filePath)}");
        }

        public void LoadProject(Project project)
        {
            LoadAudioFromPath(project.FilePath, $"Проект загружен: {project.Name}");
        }

        private void LoadProjectsFromDb()
        {
            var projects = _projectService.GetAllProjects();
            ProjectsList.Clear();
            foreach (var project in projects) ProjectsList.Add(project);
        }

        private void ShowAbout(object parameter)
        {
            var aboutWindow = new Views.AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.ShowDialog();
        }

        private void Exit(object parameter) => Application.Current.Shutdown();

        private void LoadWaveform() => WaveformSamples = _waveformProvider.GetWaveformSamples();

        public void Clean()
        {
            _undo.Clear();
            _engine.Dispose();
        }

        public bool HandleClosing()
        {
            if (!HasUnsavedChanges) return true;
            if (_dialog.ShowConfirmation("Есть несохранённые изменения. Сохранить перед выходом?", "Выход"))
            {
                SaveProject(null!);
            }
            return true;
        }

        private void DeleteProject(object parameter)
        {
            if (parameter is int projectId)
            {
                var project = _projectService.GetProjectById(projectId);
                if (project == null) return;
                if (_dialog.ShowConfirmation($"Удалить проект \"{project.Name}\"? Аудиофайл останется."))
                {
                    _projectService.DeleteProject(projectId);
                    LoadProjectsFromDb();
                    if (SelectedProject?.Id == projectId) SelectedProject = null;
                    _dialog.ShowMessage("Проект удалён");
                }
            }
        }

        private void RenameProject(object parameter)
        {
            if (parameter is Project project)
            {
                string? newName = _dialog.ShowInputDialog("Новое название:", "Переименование", project.Name);
                if (newName == null) return;
                if (!string.IsNullOrWhiteSpace(newName) && newName != project.Name)
                {
                    var existing = _projectService.GetAllProjects().FirstOrDefault(p => p.Name == newName);
                    if (existing != null && existing.Id != project.Id)
                    {
                        _dialog.ShowMessage("Проект с таким названием уже существует");
                        return;
                    }
                    string oldPath = project.FilePath;
                    string folder = Path.GetDirectoryName(oldPath) ?? string.Empty;
                    string newPath = Path.Combine(folder, newName + ".wav");
                    try
                    {
                        if (File.Exists(oldPath)) File.Move(oldPath, newPath);
                        project.Name = newName;
                        project.FilePath = newPath;
                        project.LastModified = DateTime.Now;
                        _projectService.SaveProject(project);
                        LoadProjectsFromDb();
                        SelectedProject = project;
                        _dialog.ShowMessage($"Проект переименован в \"{newName}\"");
                    }
                    catch (Exception ex)
                    {
                        _dialog.ShowMessage($"Ошибка при переименовании: {ex.Message}", "Ошибка", DialogMessageImage.Error);
                    }
                }
            }
        }

        private async void Undo(object parameter)
        {
            if (!_undo.CanUndo()) return;
            await NavigateHistory(() => _undo.Undo());
        }

        private async void Redo(object parameter)
        {
            if (!_undo.CanRedo()) return;
            await NavigateHistory(() => _undo.Redo());
        }

        private async void InsertSilence(object parameter)
        {
            string? durationStr = _dialog.ShowInputDialog("Длительность тишины (секунды):", "Вставка тишины", "1.0");
            if (durationStr == null || !double.TryParse(durationStr, out double silenceDuration) || silenceDuration <= 0) return;

            double position = SelectionStart < SelectionEnd ? SelectionStart : CurrentPosition;
            await ExecuteEdit(
                () => Task.FromResult(_editor.InsertSilence(position, silenceDuration)),
                $"Вставить {silenceDuration:F1}с тишины?",
                "Тишина вставлена");
        }

        private async void CopySelection(object parameter)
        {
            if (SelectionStart >= SelectionEnd)
            {
                _dialog.ShowMessage("Сначала выделите фрагмент");
                return;
            }
            _editor.CopySelection(SelectionStart, SelectionEnd);
            _dialog.ShowMessage($"Скопировано: {SelectionDuration}");
        }

        private async void Paste(object parameter)
        {
            if (!_editor.HasClipboard)
            {
                _dialog.ShowMessage("Буфер обмена пуст");
                return;
            }
            double position = SelectionStart < SelectionEnd ? SelectionStart : CurrentPosition;
            double clipDuration = _editor.ClipboardDuration;
            await ExecuteEdit(
                () => Task.FromResult(_editor.PasteAt(position)),
                $"Вставить фрагмент ({TimeSpan.FromSeconds(clipDuration):mm\\:ss\\.ff})?",
                "Фрагмент вставлен");
        }

        private void EditMetadata(object parameter)
        {
            if (string.IsNullOrEmpty(_engine.GetCurrentFilePath()))
            {
                _dialog.ShowMessage("Сначала загрузите аудиофайл");
                return;
            }
            string ext = System.IO.Path.GetExtension(_engine.GetCurrentFilePath()).ToLower();
            if (ext != ".mp3")
            {
                _dialog.ShowMessage("Метаданные поддерживаются только для MP3 файлов");
                return;
            }

            _editor.ReadMetadata(out string? title, out string? artist, out string? album, out string? year);

            string? titleResult = _dialog.ShowInputDialog("Название:", "Метаданные", title ?? "");
            string? artistResult = _dialog.ShowInputDialog("Исполнитель:", "Метаданные", artist ?? "");
            string? albumResult = _dialog.ShowInputDialog("Альбом:", "Метаданные", album ?? "");
            string? yearResult = _dialog.ShowInputDialog("Год:", "Метаданные", year ?? "");

            try
            {
                _editor.WriteMetadata(titleResult, artistResult, albumResult, yearResult);
                _dialog.ShowMessage("Метаданные сохранены");
            }
            catch (Exception ex)
            {
                _dialog.ShowMessage($"Ошибка: {ex.Message}", "Ошибка", DialogMessageImage.Error);
            }
        }

        private async void TrimSilence(object parameter)
        {
            string? thresholdStr = _dialog.ShowInputDialog("Порог тишины (0.01 - 0.5):", "Обрезка тишины", "0.01");
            if (thresholdStr == null || !float.TryParse(thresholdStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float threshold)) threshold = 0.01f;
            threshold = Math.Clamp(threshold, 0.001f, 0.5f);

            await ExecuteEdit(
                () => Task.FromResult(_editor.TrimSilence(threshold)),
                "Обрезать тишину в начале и конце?",
                "Тишина обрезана");
        }

        private async void ApplyEQ(object parameter)
        {
            float bass = (float)BassGain;
            float treble = (float)TrebleGain;
            if (bass == 0 && treble == 0)
            {
                _dialog.ShowMessage("Настройте басы или высокие");
                return;
            }
            await ExecuteEdit(
                () => Task.FromResult(_editor.ApplyEQ(bass, treble)),
                $"Применить эквалайзер? (Басы: {bass:F1}, Высокие: {treble:F1})",
                "Эквалайзер применён");
        }

        private async void ToMono(object parameter)
        {
            await ExecuteEdit(
                () => Task.FromResult(_editor.ToMono()),
                "Конвертировать в моно?",
                "Конвертировано в моно");
        }

        private async void ToStereo(object parameter)
        {
            await ExecuteEdit(
                () => Task.FromResult(_editor.ToStereo()),
                "Конвертировать в стерео?",
                "Конвертировано в стерео");
        }
    }
}
