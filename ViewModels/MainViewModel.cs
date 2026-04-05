using EditWave.Services;
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using EditWave.Views;

namespace EditWave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AudioService _audioService;
        private string _currentTime = "0";
        private double _currentPosition;
        private double _duration;
        private double _volume;
        private double _gain;

        // TODO: для выделения фрагмента (потом заменить на реальные значения)
        private double _selectionStart = 0;
        private double _selectionEnd = 0;


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
            _audioService.Trim(_selectionStart, _selectionEnd);
            Duration = _audioService.Duration;
            MessageBox.Show("Фрагмент обрезан", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Delete(object parameter)
        {
            if (_audioService == null) return;
            _audioService.DeleteSelection(_selectionStart, _selectionEnd);
            Duration = _audioService.Duration;
            MessageBox.Show("Фрагмент удалён", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ApplyGain(object parameter)
        {
            if (_audioService == null) return;
            float volume = (float)(Gain / 100);
            _audioService.SetVolume(volume);
            MessageBox.Show($"Громкость изменена на {Gain}%");
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
            if (_audioService == null) return;

            var dialog = new SaveFileDialog();
            dialog.Filter = "WAV файлы|*.wav|MP3 файлы|*.mp3|Все файлы|*.*";
            dialog.Title = "Сохранить аудиофайл";

            if (dialog.ShowDialog() == true)
            {
                _audioService.Export(dialog.FileName);
                MessageBox.Show($"Файл сохранён: {System.IO.Path.GetFileName(dialog.FileName)}", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void OpenProject(object parameter)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Аудио файлы|*.wav;*.mp3|Все файлы|*.*";
            dialog.Title = "Выберите аудиофайл";
            if (dialog.ShowDialog() == true)
            {
                if (_audioService.LoadFile(dialog.FileName))
                {
                    Duration = _audioService.Duration;
                    CurrentPosition = 0;
                    CurrentTime = $"00:00/{TimeSpan.FromSeconds(Duration):mm\\:ss}";
                    MessageBox.Show($"Файл загружен: {System.IO.Path.GetFileName(dialog.FileName)}");
                }
                else
                {
                    MessageBox.Show("Сегодня никакой музыки (не удалось загрузить файл)");
                }
            }

        }
        private void ShowAbout(object parameter)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.ShowDialog();
        }
    }
}