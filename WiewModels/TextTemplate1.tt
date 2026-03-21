ViewModelBase — базовый класс для всех ViewModel
Что это:
ViewModelBase — это абстрактный класс, от которого будут наследоваться все наши ViewModel (MainViewModel, AboutViewModel и т.д.). Он реализует интерфейс INotifyPropertyChanged.

Зачем нужен:
В WPF интерфейс не обновляется автоматически, когда меняются данные в коде. Если ты изменишь свойство CurrentTime в ViewModel, окно об этом не узнает, пока ты явно не "скажешь": "Эй, свойство изменилось!".

INotifyPropertyChanged — это встроенный в .NET интерфейс, который решает эту проблему. Когда свойство меняется, ViewModel вызывает событие PropertyChanged, а WPF подписан на это событие и обновляет интерфейс.

Почему в нашем проекте:
Мы используем MVVM. В MVVM ViewModel должна уведомлять View об изменениях. ViewModelBase — это удобная база, чтобы не писать один и тот же код в каждой ViewModel.

Как это работает (упрощённо):

В XAML ты пишешь {Binding CurrentTime}

WPF подписывается на событие PropertyChanged у ViewModel

Когда ты меняешь CurrentTime, вызываешь OnPropertyChanged()

WPF получает уведомление и обновляет текст на экране