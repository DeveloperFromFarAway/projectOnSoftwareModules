using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.Win32;

namespace PR3
{
    /// <summary>
    /// Окно для отображения списка сотрудников/пользователей системы
    /// </summary>
    public partial class EmployeeWindow : Window
    {
        /// <summary>
        /// Конструктор окна сотрудников
        /// </summary>
        public EmployeeWindow()
        {
            InitializeComponent();
            // Заполнение ScrollViewer данными при инициализации
            PopulateScrollViewer(ScrollViewer1);
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
        /// Заполняет ScrollViewer карточками сотрудников/пользователей
        /// </summary>
        /// <param name="scrollViewer">Элемент ScrollViewer для заполнения</param>
        /// <exception cref="ArgumentNullException">Если scrollViewer равен null</exception>
        public void PopulateScrollViewer(ScrollViewer scrollViewer)
        {
            // Проверка входного параметра
            if (scrollViewer == null)
            {
                throw new ArgumentNullException(nameof(scrollViewer), "ScrollViewer не должен быть null");
            }

            // Создание контейнера Grid для карточек
            Grid grid = new Grid();
            grid.Margin = new Thickness(10);

            // Максимальное количество колонок в сетке
            int maxColumns = 3;

            // Добавление колонок в Grid
            for (int c = 0; c < maxColumns; c++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            // Получение данных о пользователях
            Helper userScetch = new Helper();
            var userDataList = userScetch.GetUserAccountData();

            // Создание карточек для каждого пользователя
            for (int i = 0; i < userDataList.Count; i++)
            {
                /*
                 * Локальная переменная для корректного захвата в замыкании
                 * при создании обработчика события MouseDown
                 */
                int localIndex = i;

                // Расчет позиции карточки в сетке
                int row = localIndex / maxColumns;
                int column = localIndex % maxColumns;

                // Добавление строк при необходимости
                if (grid.RowDefinitions.Count <= row)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                // Создание рамки для карточки пользователя
                Border border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(5),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(5),
                    Background = Brushes.Transparent
                };

                // Обработчик клика по карточке
                border.MouseDown += (sender, e) =>
                {
                    var selectedUserData = userDataList[localIndex];
                    // Определяем тип ID (аккаунт или сотрудник)
                    bool isAccountId = selectedUserData.IsAccount;
                    int idToPass = isAccountId
                        ? selectedUserData.AccountId ?? 0
                        : selectedUserData.EmployeeId ?? 0;

                    // Открываем окно редактирования
                    AddingUser addingUserWindow = new AddingUser(isAccountId, idToPass, 1);
                    addingUserWindow.ShowDialog();
                };

                // Создание контейнера для элементов карточки
                StackPanel stackPanel = new StackPanel();

                // Заглушка для изображения пользователя
                System.Windows.Controls.Image placeholderImage = new System.Windows.Controls.Image();
                placeholderImage.Source = new BitmapImage(new Uri("user.jpg", UriKind.Relative));
                placeholderImage.Height = 100;
                placeholderImage.Width = 100;

                // Создание элементов информации о пользователе
                Label labelRole = new Label { Content = userDataList[i].Role };
                Label labelName = new Label { Content = userDataList[i].Name ?? "Нет имени" };
                Label labelEmail = new Label { Content = userDataList[i].Email ?? "Нет email" };
                Label labelLogin = new Label
                {
                    Content = userDataList[i].Login != null ? $"Логин: {userDataList[i].Login}" : "Логин не назначен"
                };

                // Добавление элементов в StackPanel
                stackPanel.Children.Add(placeholderImage);
                stackPanel.Children.Add(labelRole);
                stackPanel.Children.Add(labelName);
                stackPanel.Children.Add(labelEmail);
                stackPanel.Children.Add(labelLogin);

                // Помещаем StackPanel в Border
                border.Child = stackPanel;

                // Установка позиции в Grid
                Grid.SetRow(border, row);
                Grid.SetColumn(border, column);

                // Добавление карточки в Grid
                grid.Children.Add(border);
            }

            // Помещаем Grid в ScrollViewer
            scrollViewer.Content = grid;
        }

        private void PrintingList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Указываем путь к шрифту с поддержкой кириллицы
                string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

                // Создаем базовый шрифт с поддержкой Unicode
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font titleFont = new Font(baseFont, 16, Font.BOLD, BaseColor.DARK_GRAY);
                Font headerFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
                Font cellFont = new Font(baseFont, 9);
                Font cellFontBold = new Font(baseFont, 9, Font.BOLD);

                // Получаем данные
                Helper helper = new Helper();
                var userDataList = helper.GetUserAccountData();

                // Создаем документ
                Document document = new Document(PageSize.A4.Rotate());

                // Диалог сохранения
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = "Сотрудники_" + DateTime.Now.ToString("yyyyMMdd")
                };

                if (saveDialog.ShowDialog() == true)
                {
                    PdfWriter.GetInstance(document, new FileStream(saveDialog.FileName, FileMode.Create));
                    document.Open();

                    // Заголовок с кириллицей
                    document.Add(new iTextSharp.text.Paragraph("СПИСОК СОТРУДНИКОВ", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    });

                    // Таблица
                    PdfPTable table = new PdfPTable(5);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1.5f, 3f, 3f, 2.5f, 2f });

                    // Заголовки таблицы
                    AddCell(table, "ИД", headerFont, BaseColor.GRAY);
                    AddCell(table, "ФИО / НАЗВАНИЕ", headerFont, BaseColor.GRAY);
                    AddCell(table, "КОНТАКТЫ", headerFont, BaseColor.GRAY);
                    AddCell(table, "ЛОГИН", headerFont, BaseColor.GRAY);
                    AddCell(table, "ТИП", headerFont, BaseColor.GRAY);

                    // Данные
                    foreach (var user in userDataList)
                    {
                        string id = user.IsEmployee ? $"С{user.EmployeeId}" : $"А{user.AccountId}";
                        string type = user.IsEmployee ? "Сотрудник" : "Аккаунт";

                        AddCell(table, id, cellFont);
                        AddCell(table, user.Name ?? "-", cellFont);
                        AddCell(table, user.Email ?? "-", cellFont);
                        AddCell(table, user.Login ?? "-", cellFont);
                        AddCell(table, type, cellFont);
                    }

                    document.Add(table);
                    document.Close();

                    // Открываем созданный PDF
                    Process.Start(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void AddCell(PdfPTable table, string text, Font font, BaseColor bgColor = null)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.BorderWidth = 0.5f;
            if (bgColor != null) cell.BackgroundColor = bgColor;
            table.AddCell(cell);
        }
    }
}
