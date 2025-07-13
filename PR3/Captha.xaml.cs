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

namespace PR3
{
    /// <summary>
    /// Окно для ввода капчи с проверкой введенного кода
    /// </summary>
    public partial class Captha : Window
    {
        /// <summary>
        /// Сгенерированный код для проверки
        /// </summary>
        private string verificationCode;

        /// <summary>
        /// Конструктор окна капчи
        /// </summary>
        /// <param name="parentWindow">Родительское окно (для позиционирования)</param>
        public Captha(Window parentWindow)
        {
            InitializeComponent();

            // Генерация и отображение кода проверки
            verificationCode = GenerateRandomCode();
            lbCod.Content = verificationCode;
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
        /// Генерация случайного кода для капчи
        /// </summary>
        /// <returns>6-символьная случайная строка</returns>
        private string GenerateRandomCode()
        {
            // Все возможные символы для кода
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";

            var random = new Random();

            /*
             * Генерация строки из 6 случайных символов:
             * 1. Enumerable.Repeat создает последовательность из 6 элементов chars
             * 2. Select выбирает случайный символ из каждой копии chars
             * 3. ToArray преобразует в массив символов
             * 4. new string создает строку из массива
             */
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Проверка введенного кода капчи
        /// </summary>
        private void Confirmation_Click(object sender, RoutedEventArgs e)
        {
            if (tbCod.Text == verificationCode)
            {
                // Код верный - возвращаем DialogResult = true
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                // Код неверный - очищаем поле и возвращаем false
                tbCod.Clear();
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
