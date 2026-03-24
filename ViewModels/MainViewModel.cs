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

        public MainViewModel()
        {
            PlayCommand = new RelayCommand(Play);
        }
        private void Play(object parameter)
        {
            MessageBox.Show("чекаю как дела с привязками вообще"); // TODO потом здесь будет норм метод и другие норм методы 
        }
    }
}