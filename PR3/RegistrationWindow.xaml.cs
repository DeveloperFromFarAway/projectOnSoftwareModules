using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
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
using System.Windows.Shapes;
using BlockerLibrary;
using ClassLibrary1;
using PR3.Models;

namespace PR3
{
    /// <summary>
    /// Окно регистрации новых пользователей в системе
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        // Сервис блокировки окна при неудачных попытках
        WindowBlocker block = new WindowBlocker();

        // Счетчик неудачных попыток ввода CAPTCHA
        private int failedAttempts = 0;

        /// <summary>
        /// Конструктор окна регистрации
        /// </summary>
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик перемещения окна при захвате заголовка
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove(); // Стандартный метод перемещения окна
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
        /// Обработчик кнопки регистрации
        /// </summary>
        private void btnRegistration_Click(object sender, RoutedEventArgs e)
        {
            // Получаем введенные данные
            string login = tbLogin.Text;
            string password = tbPassword.Text;

            // Проверка заполнения обязательных полей
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Проверка согласия на CAPTCHA
            if (cbCAPTCHA.IsChecked == true)
            {
                // Создание и отображение окна CAPTCHA
                var verificationWindow = new Captha(this);
                bool? result = verificationWindow.ShowDialog();

                if (result == true)
                {
                    // Если CAPTCHA пройдена успешно
                    try
                    {
                        // Создание учетной записи
                        Helper helper = new Helper();
                        helper.CreateAccount(login, password);

                        this.Close(); // Закрытие окна после успешной регистрации
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Произошла ошибка: " + ex.Message);
                    }
                }
                else
                {
                    // Обработка неудачной попытки CAPTCHA
                    failedAttempts++;

                    if (failedAttempts == 3)
                    {
                        // Блокировка на 1 минуту после 3 неудач
                        block.BlockWindow(60, this);
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

                // Восстановление состояния окна
                this.IsEnabled = true;
                cbCAPTCHA.IsChecked = false;
            }
            else
            {
                // Обработка отказа от CAPTCHA
                failedAttempts++;

                if (failedAttempts == 3)
                {
                    // Блокировка на 1 минуту после 3 отказов
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
    }
}