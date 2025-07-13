using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
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
using Xceed.Words.NET;
using static PR3.Helper;

namespace PR3
{
    /// <summary>
    /// Окно для добавления/редактирования пользователей в системе
    /// </summary>
    public partial class AddingUser : Window
    {
        /// <summary>
        /// Режим работы окна: 0 - создание, 1 - редактирование
        /// </summary>
        private int Mode { get; set; }

        /// <summary>
        /// Флаг, указывающий что переданный ID относится к аккаунту
        /// </summary>
        public static bool isAccountIdPublic;

        /// <summary>
        /// ID пользователя или аккаунта для редактирования
        /// </summary>
        public int idUser;

        /// <summary>
        /// Конструктор окна добавления/редактирования пользователя
        /// </summary>
        /// <param name="isAccountId">True если передан ID аккаунта</param>
        /// <param name="id">ID пользователя или аккаунта</param>
        /// <param name="mode">Режим работы (0-создание, 1-редактирование)</param>
        public AddingUser(bool isAccountId, int id, int mode)
        {
            InitializeComponent();

            idUser = id;
            Mode = mode;
            isAccountIdPublic = isAccountId;

            // Если режим редактирования - загружаем данные
            if (Mode == 1)
            {
                ViewingMode(isAccountId, id);
            }
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
        /// Загрузка данных при открытии окна
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Заполнение выпадающего списка ролей
            List<Role> roles = GetRoles();
            cbRole.ItemsSource = roles;
            cbRole.DisplayMemberPath = "Name"; // Отображаемое поле
            cbRole.SelectedValuePath = "ID";   // Значение поля
        }

        /// <summary>
        /// Получает список ролей из базы данных
        /// </summary>
        /// <returns>Список объектов Role</returns>
        public List<Role> GetRoles()
        {
            using (var context = Helper.GetContext())
            {
                return context.Table_Position
                    .Select(p => new Role
                    {
                        ID = p.ID_Position,
                        Name = p.Name
                    }).ToList();
            }
        }

        /// <summary>
        /// Класс для представления роли пользователя
        /// </summary>
        public class Role
        {
            public int ID { get; set; }      // Идентификатор роли
            public string Name { get; set; }  // Название роли
        }

        /// <summary>
        /// Обработчик сохранения данных пользователя
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Создание объекта сотрудника из данных формы
            var employee = new Employee
            {
                FullName = tbFullName.Text,
                ContactInfo = tbContactInfo.Text,
                Login = tbLogin.Text,
                RoleId = cbRole.SelectedIndex + 1, // +1 так как индексы начинаются с 0
                HireDate = DateTime.TryParseExact(tbHireDate.Text, "dd.MM.yyyy", null,
                            System.Globalization.DateTimeStyles.None, out var date)
                            ? date
                            : DateTime.MinValue // Значение по умолчанию при ошибке парсинга
            };

            // Валидация данных
            var validationContext = new ValidationContext(employee);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (!Validator.TryValidateObject(employee, validationContext, validationResults, true))
            {
                // Сбор всех ошибок валидации в одну строку
                string errorMessages = string.Join("\n", validationResults.Select(r => r.ErrorMessage));
                MessageBox.Show($"Ошибки валидации:\n{errorMessages}");
                return;
            }

            try
            {
                Helper helper = new Helper();

                if (Mode == 1) // Режим редактирования
                {
                    // Получаем данные из формы
                    var employeeInfo = new EmployeeFullInfo
                    {
                        EmployeeId = !isAccountIdPublic ? idUser : (int?)null,
                        AccountId = isAccountIdPublic ? idUser : (int?)null,
                        FullName = tbFullName.Text,
                        ContactInfo = tbContactInfo.Text,
                        HireDate = DateTime.Parse(tbHireDate.Text),
                        WorkAddress = tbWorkAddress.Text,
                        ProbationPeriod = GetProbationPeriod(cbProbationPeriod.SelectedIndex),
                        PassportData = tbPassportData.Text,
                        INN = tbINN.Text,
                        SNILS = tbSNILS.Text,
                        TaxExemption = cbTaxExemption.IsChecked ?? false,
                        Login = tbLogin.Text,
                        PositionId = (int)cbRole.SelectedValue
                    };

                    helper.UpdateEmployeeWithAccount(employeeInfo);
                    MessageBox.Show("Данные успешно обновлены!");
                }
                else // Режим создания
                {
                    // Получаем данные из формы
                    var employeeData = new EmployeeFullData
                    {
                        FullName = tbFullName.Text,
                        ContactInfo = tbContactInfo.Text,
                        HireDate = DateTime.Parse(tbHireDate.Text),
                        WorkAddress = tbWorkAddress.Text,
                        ProbationPeriod = GetProbationPeriod(cbProbationPeriod.SelectedIndex),
                        PassportData = tbPassportData.Text,
                        INN = tbINN.Text,
                        SNILS = tbSNILS.Text,
                        TaxExemption = cbTaxExemption.IsChecked ?? false
                    };

                    helper.CreateEmployeeWithAccount(
                        employeeData,
                        tbLogin.Text,
                        tbPassword.Text,
                        (int)cbRole.SelectedValue);

                    MessageBox.Show("Сотрудник и аккаунт успешно созданы!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }
        private int GetProbationPeriod(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0: return 1;
                case 1: return 2;
                case 2: return 3;
                default: return 0;
            }
        }
        /// <summary>
        /// Загрузка данных для режима просмотра/редактирования
        /// </summary>
        /// <param name="isAccountId">True если передан ID аккаунта</param>
        /// <param name="id">ID пользователя или аккаунта</param>
        private void ViewingMode(bool? isAccountId, int? id)
        {
            // Скрываем ненужные элементы в режиме редактирования
            spPassword.Visibility = Visibility.Collapsed;
            btnClear.Visibility = Visibility.Collapsed;

            Helper helper = new Helper();
            EmployeeFullInfo info = helper.GetEmployeeOrAccountInfo(id, isAccountId);

            if (info != null)
            {
                // Заполняем поля данными
                tbFullName.Text = info.FullName ?? string.Empty;
                tbContactInfo.Text = info.ContactInfo ?? string.Empty;
                tbHireDate.Text = info.HireDate?.ToString("d") ?? string.Empty;
                tbLogin.Text = info.Login ?? string.Empty;

                // Настраиваем выпадающий список ролей
                var roles = GetRoles();
                cbRole.ItemsSource = roles;
                cbRole.DisplayMemberPath = "Name";
                cbRole.SelectedValuePath = "ID";

                // Устанавливаем роль "Пользователь" по умолчанию
                var userRole = roles.FirstOrDefault(r => r.Name == "Пользователь");
                cbRole.SelectedIndex = 3; // Жестко заданный индекс (потенциально хрупкое место)
                MessageBox.Show(cbRole.SelectedValue.ToString());
            }
            else
            {
                MessageBox.Show("Информация не найдена.");
            }
        }

        /// <summary>
        /// Обработчик загрузки фотографии пользователя
        /// </summary>
        private void btnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            // Создание и настройка диалогового окна выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.png)|*.jpg;*.png";

            // Отображение диалогового окна
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Загрузка выбранного изображения и установка его в элемент Image
                    BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    imgPhoto.Source = bitmap;
                }
                catch (Exception ex)
                {
                    // Обработка потенциальных ошибок
                    MessageBox.Show("Ошибка загрузки изображения: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Очистка всех полей формы
        /// </summary>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbFullName.Text = string.Empty;
            tbContactInfo.Text = string.Empty;
            tbHireDate.Text = string.Empty;
            tbLogin.Text = string.Empty;
            tbPassword.Text = string.Empty;
            cbRole.SelectedIndex = -1; // Сброс выбора
            imgPhoto.Source = null;    // Очистка изображения
        }

        private void btnGenerateContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Путь к шаблону и новому файлу
                string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blank-trudovogo-dogovora.docx");
                string saveDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Трудовые договоры");
                Directory.CreateDirectory(saveDirectory);

                string fileName = $"Трудовой_договор_{tbFullName.Text.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.docx";
                string fullPath = System.IO.Path.Combine(saveDirectory, fileName);

                // Загружаем шаблон
                using (DocX document = DocX.Load(templatePath))
                {
                    // Данные для подстановки
                    var replacements = new Dictionary<string, string>
                    {
                        // Данные компании
                        { "номер_договора", GenerateContractNumber() },
                        { "город", "Москва" },
                        { "день", DateTime.Now.Day.ToString() },
                        { "месяц", GetMonthName(DateTime.Now.Month) },
                        { "год", DateTime.Now.ToString("yy") },
                        { "название_компании", "ООО \"ТехноПрогресс\"" },
                        { "директор", "Иванов Иван Иванович" },
                        { "основание", "Устава" },
                
                        // Данные сотрудника
                        { "фио_сотрудника", tbFullName.Text },
                        { "должность", (cbRole.SelectedItem as Role)?.Name ?? "Сотрудник" },
                        { "адрес_работы", tbWorkAddress.Text },
                        { "день_начала", DateTime.Now.Day.ToString() },
                        { "месяц_начала", GetMonthName(DateTime.Now.Month) },
                        { "год_начала", DateTime.Now.ToString("yyyy") },
                        { "испытательный_срок", GetProbationPeriodText() },

                        // Данные по оплате
                        { "оклад_цифрами", "50000" },
                        { "оклад_прописью", "Пятьдесят тысяч" },
                        { "полное_название_компании", "Общество с ограниченной ответственностью \"ТехноПрогресс\"" },
    
                        // Паспортные данные
                        { "паспорт_серия", "4510" },
                        { "паспорт_номер", "123456" },
                        { "паспорт_выдан", "ОВД г. Москвы 01.01.2020" },
    
                        // Данные работодателя
                        { "инн_работодателя", "7701234567" },
                        { "кпп_работодателя", "770101001" },
                        { "адрес_работодателя", "г. Москва, ул. Ленина, д. 42" },
                        { "фио_директора", "Иванов И.И." },
    
                        // Подписание
                        { "дата_подписания", DateTime.Now.ToString("dd.MM.yyyy") },
                        { "подпись_сотрудника", tbFullName.Text }
                    };


                    // Заменяем плейсхолдеры в документе
                    foreach (var replacement in replacements)
                    {
                        document.ReplaceText("{" + replacement.Key + "}", replacement.Value);
                    }

                    // Сохраняем новый документ
                    document.SaveAs(fullPath);
                }

                MessageBox.Show($"Трудовой договор успешно сформирован:\n{fullPath}", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании договора: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательные методы
        private string GenerateContractNumber()
        {
            return $"ТД-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private string GetMonthName(int month)
        {
            string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня",
                       "июля", "августа", "сентября", "октября", "ноября", "декабря" };
            return months[month - 1];
        }

        private string GetProbationPeriodText()
        {
            switch (cbProbationPeriod.SelectedIndex)
            {
                case 0: return "1";
                case 1: return "2";
                case 2: return "3";
                default: return "0 (без испытательного срока)";
            }
        }
    }
}
 