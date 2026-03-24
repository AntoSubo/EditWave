using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EditWave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _currentTime = "0";

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
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand TrimCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ApplyGainCommand { get; }
        public ICommand ApplyReverseCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public MainViewModel()
        {
            PlayCommand = new RelayCommand(Play);
            PauseCommand = new RelayCommand(Pause);
            StopCommand = new RelayCommand(Stop);
            TrimCommand = new RelayCommand(Trim);
            DeleteCommand = new RelayCommand(Delete);
            ApplyGainCommand = new RelayCommand(ApplyGain);
            ApplyReverseCommand = new RelayCommand(ApplyReverse);
            SaveProjectCommand = new RelayCommand(SaveProject);
        }
        private void Play(object parameter)
        {
            MessageBox.Show("чекаю как дела с привязками вообще"); // TODO потом здесь будет норм метод и другие норм методы 
        }
        private void Pause(object parameter)
        {

        }
        private void Stop(object parameter)
        {

        }
        private void Trim(object parameter)
        {

        }
        private void Delete(object parameter)
        {

        }
        private void ApplyGain(object parameter)
        {

        }
        private void ApplyReverse(object parameter)
        {

        }
        private void SaveProject(object parameter)
        {

        }
    }
}