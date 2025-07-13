using BlockerLibrary;
using ClassLibrary1;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TimeCheckLibrary;

namespace PR3
{
    /// <summary>
    /// Главное окно приложения, отвечающее за авторизацию пользователей
    /// </summary>
    public partial class MainWindow : Window
    {
        // Таймер для проверки рабочего времени
        DispatcherTimer timer = new DispatcherTimer();

        // Сервис блокировки окна при неудачных попытках входа
        WindowBlocker block = new WindowBlocker();

        // Счетчик неудачных попыток авторизации
        private int failedAttempts = 0;

        /// <summary>
        /// Команда для кастомных действий (пример использования)
        /// </summary>
        public ICommand CustomCommand { get; }

        /// <summary>
        /// Конструктор главного окна
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Проверка и восстановление состояния блокировки
            block.CheckAndRestoreBlock(this);

            // Настройка таймера для проверки рабочего времени
            timer.Interval = TimeSpan.FromSeconds(60);
            timer.Tick += CheckWorkTime;
            timer.Start();

            // Первоначальная проверка рабочего времени
            CheckWorkTime(null, null);

            // Инициализация команды с привязкой к методу ExecuteCustomCommand
            CustomCommand = new RelayCommand(ExecuteCustomCommand);
            DataContext = this;
        }

        /// <summary>
        /// Метод выполнения кастомной команды (пример использования)
        /// </summary>
        private void ExecuteCustomCommand(object parameter)
        {
            // Тестовый вызов авторизации (имитация успешного входа администратора)
            HandlePostAuthenticationActions(true, null, "Администратор", "Егор", "Admin");
        }

        /// <summary>
        /// Реализация команды ICommand для MVVM
        /// </summary>
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Func<object, bool> _canExecute;

            /// <summary>
            /// Конструктор команды
            /// </summary>
            /// <param name="execute">Метод выполнения</param>
            /// <param name="canExecute">Метод проверки возможности выполнения</param>
            public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            /// <summary>
            /// Проверка возможности выполнения команды
            /// </summary>
            public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

            /// <summary>
            /// Выполнение команды
            /// </summary>
            public void Execute(object parameter)
            {
                _execute(parameter);
            }

            /// <summary>
            /// Событие изменения состояния команды
            /// </summary>
            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }

        /// <summary>
        /// Проверка рабочего времени и управление доступностью авторизации
        /// </summary>
        private void CheckWorkTime(object sender, EventArgs e)
        {
            if (!TimeCheck.IsWorkingTime())
            {
                // Режим вне рабочего времени
                btnAuthorization.IsEnabled = false;
                ldEndWorkingDay.Content = "Рабочий день окончен, авторизация невозможна";
            }
            else
            {
                // Рабочее время
                btnAuthorization.IsEnabled = true;
                ldEndWorkingDay.Content = "";
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
            this.Close();
        }

        /// <summary>
        /// Обработчик нажатия кнопки авторизации
        /// </summary>
        private void btnAuthorization_Click(object sender, RoutedEventArgs e)
        {
            if (cbCAPTCHA.IsChecked == true)
            {
                // Вариант с прохождением CAPTCHA
                var verificationWindow = new Captha(this);
                bool? result = verificationWindow.ShowDialog();

                if (result == true)
                {
                    // CAPTCHA пройдена - попытка авторизации
                    Helper authorization = new Helper();
                    var (isAuthenticated, message, positionName, userName, userLogin) =
                        authorization.AuthenticateUser(tbLogin.Text, tbPassword.Text);
                    HandlePostAuthenticationActions(isAuthenticated, message, positionName, userName, userLogin);
                }
                else
                {
                    // Обработка неудачной CAPTCHA
                    failedAttempts++;

                    if (failedAttempts == 3)
                    {
                        // Блокировка на 5 минут после 3 неудач
                        block.BlockWindow(300, this);
                    }
                    else if (failedAttempts == 2)
                    {
                        MessageBox.Show("Вы дважды не прошли проверку, при следующей неудачной попытке окно будет заблокированно на 5 минут");
                    }
                    else
                    {
                        MessageBox.Show("Проверка не пройдена.");
                    }
                }

                this.IsEnabled = true;
                cbCAPTCHA.IsChecked = false;
            }
            else
            {
                // Вариант без CAPTCHA
                failedAttempts++;

                if (failedAttempts == 3)
                {
                    // Блокировка на 1 минуту после 3 отказов от CAPTCHA
                    block.BlockWindow(60, this);
                }
                else if (failedAttempts == 2)
                {
                    MessageBox.Show("Вы дважды не согласились пройти проверку, в следующий раз окно будет заблокированно на 1 минуту");
                }
                else
                {
                    MessageBox.Show("Вы не согласны пройти проверку.");
                }
            }
        }

        /// <summary>
        /// Обработка результатов авторизации
        /// </summary>
        private void HandlePostAuthenticationActions(bool isAuthenticated, string message, string positionName, string userName, string userLogin)
        {
            if (!isAuthenticated)
            {
                // Неудачная авторизация
                failedAttempts++;
                MessageBox.Show(message);
                return;
            }

            if (!string.IsNullOrEmpty(positionName))
            {
                // Успешная авторизация
                failedAttempts = 0;
                var newForm = new UserWindow(userName, userLogin, positionName);
                newForm.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Учетные данные верны, но действие не определено для данной роли.");
            }
        }

        /// <summary>
        /// Обработчик кнопки регистрации
        /// </summary>
        private void btnRegistration_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow newWindow = new RegistrationWindow();
            newWindow.Owner = this;
            newWindow.ShowDialog();
        }

        /// <summary>
        /// Обработчик кнопки входа как гостя
        /// </summary>
        private void btnGuest_Click(object sender, RoutedEventArgs e)
        {
            GuestWindow userWindow = new GuestWindow();
            userWindow.Show();
            this.Close();
        }
    }
} 