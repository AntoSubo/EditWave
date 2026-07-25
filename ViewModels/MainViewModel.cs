using EditWave.Models;
using EditWave.Services;
using EditWave.Views;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EditWave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {

        private readonly AudioService _audioService;
        private string _currentTime = "0";
        private double _currentPosition;
        private double _currentPositionNormalized;
        private double _duration;
        private double _volume;
        private double _gain;
        private readonly ProjectService _projectService;
        private ObservableCollection<Project> _projectsList;
        private Project _selectedProject;
        private double _selectionStart;
        private double _selectionEnd;
        private float[] _waveformSamples;
        private bool _isProcessing;
        public bool IsPlaying => _audioService.IsPlaying;
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

        public Project SelectedProject
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
            set { _selectionStart = value; OnPropertyChanged(); }
        }

        public double SelectionEnd
        {
            get => _selectionEnd;
            set { _selectionEnd = value; OnPropertyChanged(); }
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
                    _audioService.SetPosition(value);
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
                    _audioService.SetVolume((float)value);
                }
            }
        }

        public double Gain
        {
            get => _gain;
            set { if (_gain != value) { _gain = value; OnPropertyChanged(); } }
        }

        public float[] WaveformSamples
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
        public ICommand SaveProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand RenameProjectCommand { get; }

        public MainViewModel()
        {
            _audioService = new AudioService();
            _audioService.PositionChanged += OnPositionChanged;
            _audioService.UndoStateChanged += UpdateUndoRedoState;
            UpdateUndoRedoState();
            PlayCommand = new RelayCommand(Play);
            PauseCommand = new RelayCommand(Pause);
            StopCommand = new RelayCommand(Stop);
            TrimCommand = new RelayCommand(Trim);
            DeleteCommand = new RelayCommand(Delete);
            ApplyGainCommand = new RelayCommand(ApplyGain);
            ApplyReverseCommand = new RelayCommand(ApplyReverse);
            SaveProjectCommand = new RelayCommand(SaveProject);
            UndoCommand = new RelayCommand(Undo);
            RedoCommand = new RelayCommand(Redo);
            OpenProjectCommand = new RelayCommand(OpenProject);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ExitCommand = new RelayCommand(Exit);
            ExportCommand = new RelayCommand(ExportAudio);
            DeleteProjectCommand = new RelayCommand(DeleteProject);
            RenameProjectCommand = new RelayCommand(RenameProject);

            _projectsList = new ObservableCollection<Project>();
            _projectService = new ProjectService();
            LoadProjectsFromDb();
        }
        private void UpdateUndoRedoState()
        {
            CanUndo = _audioService.CanUndo();
            CanRedo = _audioService.CanRedo();
        }
        private void DeleteProject(object parameter)
        {
            if (parameter is int projectId)
            {
                var project = _projectService.GetProjectById(projectId);
                if (project == null) return;
                var result = MessageBox.Show($"Удалить проект \"{project.Name}\"? Аудиофайл останется.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _projectService.DeleteProject(projectId);
                    LoadProjectsFromDb();
                    if (SelectedProject?.Id == projectId) SelectedProject = null;
                    MessageBox.Show("Проект удалён");
                }
            }
        }

        private void RenameProject(object parameter)
        {
            if (parameter is Project project)
            {
                string newName = Interaction.InputBox("Новое название:", "Переименование", project.Name);
                if (!string.IsNullOrWhiteSpace(newName) && newName != project.Name)
                {
                    var existing = _projectService.GetAllProjects().FirstOrDefault(p => p.Name == newName);
                    if (existing != null && existing.Id != project.Id)
                    {
                        MessageBox.Show("Проект с таким названием уже существует");
                        return;
                    }
                    string oldPath = project.FilePath;
                    string folder = Path.GetDirectoryName(oldPath);
                    string newPath = Path.Combine(folder, newName + ".wav");
                    if (File.Exists(oldPath)) File.Move(oldPath, newPath);
                    project.Name = newName;
                    project.FilePath = newPath;
                    project.LastModified = DateTime.Now;
                    _projectService.SaveProject(project);
                    LoadProjectsFromDb();
                    SelectedProject = project;
                    MessageBox.Show($"Проект переименован в \"{newName}\"");
                }
            }
        }

        private void LoadProjectsFromDb()
        {
            var projects = _projectService.GetAllProjects();
            ProjectsList.Clear();
            foreach (var project in projects) ProjectsList.Add(project);
        }

        public void LoadProject(Project project)
        {
            if (_audioService.LoadFile(project.FilePath))
            {
                Duration = _audioService.Duration;
                CurrentPosition = 0;
                CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
                LoadWaveform();
                MessageBox.Show($"Проект загружен: {project.Name}");
            }
            else MessageBox.Show("Не удалось загрузить файл проекта");
        }

        private void Exit(object parameter) => Application.Current.Shutdown();

        private void OnPositionChanged()
        {
            CurrentPosition = _audioService.CurrentPosition;
            CurrentTime = $"{TimeSpan.FromSeconds(CurrentPosition):mm\\:ss}/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
            if (Duration > 0) CurrentPositionNormalized = CurrentPosition / Duration;
        }

        private void Play(object parameter) => _audioService.Play();
        private void Pause(object parameter) => _audioService.Pause();
        private void Stop(object parameter)
        {
            _audioService.Stop();
            CurrentPosition = 0;
            CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
        }

        private async void Trim(object parameter)
        {
            if (SelectionStart >= SelectionEnd)
            {
                MessageBox.Show("Сначала выделите фрагмент");
                return;
            }
            IsProcessing = true;
            try
            {
                await Task.Run(() => _audioService.Trim(SelectionStart, SelectionEnd));
                Duration = _audioService.Duration;
                LoadWaveform();
                SelectionStart = 0;
                SelectionEnd = 0;
                MessageBox.Show("Фрагмент обрезан");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void Delete(object parameter)
        {
            if (SelectionStart >= SelectionEnd)
            {
                MessageBox.Show("Сначала выделите фрагмент");
                return;
            }
            IsProcessing = true;
            try
            {
                await Task.Run(() => _audioService.DeleteSelection(SelectionStart, SelectionEnd));
                Duration = _audioService.Duration;
                LoadWaveform();
                SelectionStart = 0;
                SelectionEnd = 0;
                MessageBox.Show("Фрагмент удалён");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void ApplyGain(object parameter)
        {
            IsProcessing = true;
            try
            {
                await Task.Run(() =>
                {
                    float gainFactor = (float)(Gain / 100.0);
                    if (SelectionStart < SelectionEnd)
                        _audioService.ApplyGainToSelection(gainFactor, SelectionStart, SelectionEnd);
                    else
                        _audioService.ApplyGain(gainFactor);
                });
                Duration = _audioService.Duration;
                LoadWaveform();
                MessageBox.Show($"Усиление применено: {Gain}%");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void ApplyReverse(object parameter)
        {
            IsProcessing = true;
            try
            {
                await Task.Run(() =>
                {
                    if (SelectionStart < SelectionEnd)
                        _audioService.ApplyReverseToSelection(SelectionStart, SelectionEnd);
                    else
                        _audioService.ApplyReverse();
                });
                Duration = _audioService.Duration;
                LoadWaveform();
                MessageBox.Show("Реверс применён", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExportAudio(object parameter)
        {
            if (string.IsNullOrEmpty(_audioService.GetCurrentFilePath()))
            {
                MessageBox.Show("Нет аудио для экспорта");
                return;
            }
            var dialog = new SaveFileDialog();
            dialog.Filter = "WAV файлы|*.wav|MP3 файлы|*.mp3";
            dialog.Title = "Экспорт аудио";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _audioService.Export(dialog.FileName);
                    MessageBox.Show("Экспорт завершён");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveProject(object parameter)
        {
            if (string.IsNullOrEmpty(_audioService.GetCurrentFilePath()))
            {
                MessageBox.Show("Сначала загрузите аудиофайл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string projectName = Interaction.InputBox("Введите название проекта:", "Сохранение проекта", "Мой проект");
            if (string.IsNullOrWhiteSpace(projectName)) return;

            string currentFilePath = _audioService.GetCurrentFilePath();
            bool isTemporary = _audioService.IsTemporaryFile();
            string projectFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");
            Directory.CreateDirectory(projectFolder);
            string savePath = Path.Combine(projectFolder, projectName + ".wav");

            var existingProject = _projectService.GetAllProjects().FirstOrDefault(p => p.Name == projectName);
            if (existingProject != null)
            {
                var result = MessageBox.Show($"Проект \"{projectName}\" уже существует. Перезаписать?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                if (File.Exists(existingProject.FilePath)) File.Delete(existingProject.FilePath);
                existingProject.FilePath = savePath;
                existingProject.LastModified = DateTime.Now;
                _projectService.SaveProject(existingProject);
            }
            else
            {
                var project = new Project { Name = projectName, FilePath = savePath, LastModified = DateTime.Now };
                _projectService.SaveProject(project);
            }
            if (isTemporary || !File.Exists(savePath)) File.Copy(currentFilePath, savePath, true);
            LoadProjectsFromDb();
            MessageBox.Show($"Проект \"{projectName}\" сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenProject(object parameter)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Аудио файлы|*.wav;*.mp3|Все файлы|*.*";
            if (dialog.ShowDialog() == true)
            {
                if (_audioService.LoadFile(dialog.FileName))
                {
                    Duration = _audioService.Duration;
                    CurrentPosition = 0;
                    CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
                    LoadWaveform();
                    MessageBox.Show($"Файл загружен: {Path.GetFileName(dialog.FileName)}");
                }
                else MessageBox.Show("Не удалось загрузить файл");
            }
        }

        private void ShowAbout(object parameter)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.ShowDialog();
        }

        private void LoadWaveform() => WaveformSamples = _audioService.GetWaveformSamples();

        public void Clean() => _audioService.Dispose();
        private async void Undo(object parameter)
        {
            if (!_audioService.CanUndo()) return;
            IsProcessing = true;
            try
            {
                await Task.Run(() => _audioService.Undo());
                Duration = _audioService.Duration;
                CurrentPosition = 0;
                LoadWaveform();
                CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void Redo(object parameter)
        {
            if (!_audioService.CanRedo()) return;
            IsProcessing = true;
            try
            {
                await Task.Run(() => _audioService.Redo());
                Duration = _audioService.Duration;
                CurrentPosition = 0;
                LoadWaveform();
                CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }

}