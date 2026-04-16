using EditWave.Models;
using EditWave.Services;
using EditWave.Views;
using LiteDB;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
namespace EditWave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AudioService _audioService;
        private string _currentTime = "0";
        private double _currentPosition;
        private double _duration;
        private double _volume;
        private string _workingFilePath;
        private double _gain;
        private readonly ProjectService _projectService;
        private ObservableCollection<Project> _projectsList;
        private Project _selectedProject;
        public ObservableCollection<Project> ProjectsList
        {
            get => _projectsList;
            set
            {
                _projectsList = value;
                OnPropertyChanged();
            }
        }
        public Project SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadProject(value);
                }
            }
        }
        private double _selectionStart;
        private double _selectionEnd;
        public double SelectionStart
        {
            get => _selectionStart;
            set
            {
                _selectionStart = value;
                OnPropertyChanged();
            }
        }
        public double SelectionEnd
        {
            get => _selectionEnd;
            set
            {
                _selectionEnd = value;
                OnPropertyChanged();
            }
        }
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged();
                }
            }
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
        public double Duration
        {
            get => _duration;
            set 
            {
                if (_duration != value)
                {
                    _duration = value;
                    OnPropertyChanged();
                }
            }
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
            set
            {
               if (_gain != value)
                {
                    _gain = value;
                    OnPropertyChanged();
                }
            }
        }
        private float[] _waveformSamples;
        public float[] WaveformSamples
        {
            get => _waveformSamples;
            set
            {
                _waveformSamples = value;
                OnPropertyChanged();
            }
        }
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand TrimCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ApplyGainCommand { get; }
        public ICommand ApplyReverseCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            _audioService = new AudioService();
            _audioService.PositionChanged += OnPositionChanged;
            PlayCommand = new RelayCommand(Play);
            PauseCommand = new RelayCommand(Pause);
            StopCommand = new RelayCommand(Stop);
            TrimCommand = new RelayCommand(Trim);
            DeleteCommand = new RelayCommand(Delete);
            ApplyGainCommand = new RelayCommand(ApplyGain);
            ApplyReverseCommand = new RelayCommand(ApplyReverse);
            SaveProjectCommand = new RelayCommand(SaveProject);
            OpenProjectCommand = new RelayCommand(OpenProject);
            ShowAboutCommand = new RelayCommand(ShowAbout);
            ExitCommand = new RelayCommand(Exit);
            _projectsList = new ObservableCollection<Project>();
            _projectService = new ProjectService();
            LoadProjectsFromDb();
        }
        private void LoadProjectsFromDb()
        {
            var projects = _projectService.GetAllProjects();
            ProjectsList.Clear();
            foreach (var project in projects)
            {
                ProjectsList.Add(project);
            }
        }
        private void LoadProject(Project project)
        {
            if (_audioService.LoadFile(project.FilePath))
            {
                Duration = _audioService.Duration;
                CurrentPosition = 0;
                CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
                LoadWaveform();
                MessageBox.Show($"Проект загружен: {project.Name}");
            }
            else
            {
                MessageBox.Show("Не удалось загрузить файл проекта");
            }
        }
        private void Exit(object parameter)
        {
            Application.Current.Shutdown();
        }
        private void OnPositionChanged()
        {
            CurrentPosition = _audioService.CurrentPosition;
            CurrentTime = $"{TimeSpan.FromSeconds(CurrentPosition):mm\\:ss}/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
        }
        private void Play(object parameter)
        {
            _audioService.Play();
        }
        private void Pause(object parameter)
        {
            _audioService.Pause();
        }
        private void Stop(object parameter)
        {
            _audioService.Stop();
            CurrentPosition = 0;
            CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
        }
        private void Trim(object parameter)
        {
            if (_audioService == null) return;
            if (SelectionStart >= SelectionEnd)
            {
                MessageBox.Show("Сначала выделите фрагмент на волновой форме", "Нет выделения");
                return;
            }

            try
            {
                _audioService.Trim(SelectionStart, SelectionEnd);
                Duration = _audioService.Duration;
                LoadWaveform();
                SelectionStart = 0;
                SelectionEnd = 0;
                MessageBox.Show("Фрагмент обрезан", "Готово");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }
        private void Delete(object parameter)
        {
            if (SelectionStart >= SelectionEnd)
            {
                MessageBox.Show("Сначала выделите фрагмент на волновой форме", "Нет выделения");
                return;
            }

            try
            {
                _audioService.DeleteSelection(SelectionStart, SelectionEnd);
                Duration = _audioService.Duration;
                LoadWaveform();
                SelectionStart = 0;
                SelectionEnd = 0;
                MessageBox.Show("Фрагмент удалён", "Готово");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }
        private void ApplyGain(object parameter)
        {
            if (_audioService == null) return;
            float gainFactor = (float)(Gain / 100.0); 
            _audioService.ApplyGain(gainFactor);
            Duration = _audioService.Duration;
            LoadWaveform();
            MessageBox.Show($"Усиление применено: {Gain}%");
        }
        private void ApplyReverse(object parameter)
        {
            if (_audioService == null) return;
            _audioService.ApplyReverse();
            Duration = _audioService.Duration;
            MessageBox.Show("Реверс применён", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void SaveProject(object parameter)
        {
            if (_audioService?.HasFile != true)
            {
                MessageBox.Show("Сначала загрузите аудиофайл");
                return;
            }

            var inputDialog = new InputDialog("Название проекта", "Введите название проекта:");
            if (inputDialog.ShowDialog() != true || string.IsNullOrEmpty(inputDialog.Answer))
                return;

            string projectName = inputDialog.Answer;
            string currentFilePath = _audioService.GetCurrentFilePath();
            bool isTemporary = _audioService.IsTemporaryFile();
            if (isTemporary)
            {
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                               "EditWave",
                                               projectName + ".wav");

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

              
                File.Copy(currentFilePath, savePath, true);
                currentFilePath = savePath;

            }
            var project = new Project
            {
                Name = projectName,
                FilePath = currentFilePath,
                LastModified = DateTime.Now
            };

            _projectService.SaveProject(project);
            LoadProjectsFromDb();
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
                    MessageBox.Show($"Файл загружен: {System.IO.Path.GetFileName(dialog.FileName)}");
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить файл");
                }
            }
        }
        private void ShowAbout(object parameter)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.ShowDialog();
        }
        private void LoadWaveform()
        {
            WaveformSamples = _audioService.GetWaveformSamples();
        }
        public void Clean(object parameter)
        {
            _audioService.Dispose();
        }
        public void DeleteProject(int projectId)
        {
            var result = MessageBox.Show("Удалить проект из списка? Аудиофайл останется на диске.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _projectService.DeleteProject(projectId);
                LoadProjectsFromDb();
            }
        }
    }
}