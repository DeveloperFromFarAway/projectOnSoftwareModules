
using BlockerLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using TimeCheckLibrary;

namespace PR3
{
    /// <summary>
    /// Окно пользователя системы, предоставляющее доступ к функционалу в зависимости от роли
    /// </summary>
    public partial class UserWindow : Window
    {
        // Сервис блокировки окна
        WindowBlocker block = new WindowBlocker();

        // Таймер для проверки рабочего времени
        DispatcherTimer timer = new DispatcherTimer();

        // Таймер для отображения оставшегося времени
        DispatcherTimer timerEndWork = new DispatcherTimer();

        /// <summary>
        /// Конструктор окна пользователя
        /// </summary>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="userLogin">Логин пользователя</param>
        /// <param name="positionName">Название должности/роли</param>
        public UserWindow(string userName, string userLogin, string positionName)
        {
            InitializeComponent();

            // Установка приветственного сообщения
            lblGreeting.Content = $"{GetGreeting()}! {userName}\n" +
                                $"Вы зашли под логином {userLogin} с ролью {positionName}";

            /* Закомментированная инициализация таймеров
            timer.Interval = TimeSpan.FromSeconds(60);
            timer.Tick += CheckWorkTime;
            timer.Start();

            timerEndWork.Interval = TimeSpan.FromSeconds(1);
            timerEndWork.Tick += UpdateTimeDisplay;
            UpdateTimeDisplay(null, null);
            timerEndWork.Start();
            */

            // Инициализация списка доступных функций
            FunctionList.ItemsSource = new List<FunctionItem>
        {
            new FunctionItem { Name = "Просмотр сотрудников", TargetWindow = "EmployeeWindow" },
            new FunctionItem { Name = "Продажа товаров", TargetWindow = "SalesWindow" },
            new FunctionItem { Name = "Создание сотрудника", TargetWindow = "CreateEmployeeWindow" },
        };
        }

        /// <summary>
        /// Класс элемента списка функций
        /// </summary>
        public class FunctionItem
        {
            /// <summary>Название функции</summary>
            public string Name { get; set; }

            /// <summary>Имя окна для перехода</summary>
            public string TargetWindow { get; set; }
        }

        /// <summary>
        /// Обработчик двойного клика по элементу списка функций
        /// </summary>
        private void FunctionList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Получаем выбранный элемент
            if (FunctionList.SelectedItem is FunctionItem selectedItem)
            {
                // Переход на целевое окно
                SwitchToWindow(selectedItem.TargetWindow);
            }
        }

        /// <summary>
        /// Переключение на указанное окно
        /// </summary>
        /// <param name="windowName">Имя окна для перехода</param>
        private void SwitchToWindow(string windowName)
        {
            Window newWindow = null;

            // Выбор окна по имени
            switch (windowName)
            {
                case "EmployeeWindow":
                    newWindow = new EmployeeWindow();
                    break;
                case "CreateEmployeeWindow":
                    newWindow = new AddingUser(false, 0, 0);
                    break;
                default:
                    MessageBox.Show("Неизвестная функция!");
                    return;
            }

            // Открываем новое окно как диалоговое
            newWindow.Owner = this;
            newWindow.ShowDialog();
        }

        /// <summary>
        /// Генерация приветствия в зависимости от времени суток
        /// </summary>
        /// <returns>Строка с приветствием</returns>
        private string GetGreeting()
        {
            var now = DateTime.Now.TimeOfDay;

            if (now >= new TimeSpan(10, 0, 0) && now < new TimeSpan(12, 0, 0))
                return "Доброе утро";
            if (now >= new TimeSpan(12, 1, 0) && now < new TimeSpan(17, 0, 0))
                return "Добрый день";
            return "Добрый вечер";
        }

        /// <summary>
        /// Проверка рабочего времени и блокировка/разблокировка окна
        /// </summary>
        private void CheckWorkTime(object sender, EventArgs e)
        {
            if (TimeCheck.IsWorkingTime())
            {
                block.RemoveIndefiniteBlocking();
            }
            else
            {
                block.BlockWindowIndefinitely(this);
            }
        }

        /// <summary>
        /// Обработчик перемещения окна при захвате заголовка
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Закрытие окна при клике на иконку выхода
        /// </summary>
        private void ldExit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            timer.Stop(); // Останавливаем таймер при закрытии окна
            this.Close();
        }

        /// <summary>
        /// Обновление отображения оставшегося рабочего времени
        /// </summary>
        private void UpdateTimeDisplay(object sender, EventArgs e)
        {
            var now = DateTime.Now.TimeOfDay;

            if (now >= new TimeSpan(10, 0, 0) && now <= new TimeSpan(19, 0, 0))
            {
                // Расчет оставшегося времени до конца рабочего дня
                var endOfDay = new TimeSpan(19, 0, 0);
                var timeRemaining = endOfDay - now;
                ldTime.Content = timeRemaining.ToString(@"hh\:mm\:ss");
            }
            else
            {
                ldTime.Content = "00:00:00";
            }
        }
    }
}
