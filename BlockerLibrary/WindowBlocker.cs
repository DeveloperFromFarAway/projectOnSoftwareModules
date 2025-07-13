using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlockerLibrary
{
    /// <summary>
    /// Класс для управления блокировкой окон приложения
    /// </summary>
    public class WindowBlocker
    {
        // Имя файла для хранения времени окончания блокировки
        private const string FileName = "block_end_time.txt";

        // Элементы UI для блокировки
        private Grid blockerGrid;
        private System.Windows.Controls.Label timeLabel;

        // Таймер для отслеживания времени блокировки
        private DispatcherTimer timer;

        // Время окончания блокировки
        private DateTime endTime;

        /// <summary>
        /// Блокирует окно на указанное количество секунд
        /// </summary>
        /// <param name="seconds">Длительность блокировки в секундах</param>
        /// <param name="currentElement">Элемент окна для блокировки</param>
        public void BlockWindow(int seconds, DependencyObject currentElement)
        {
            var currentWindow = Window.GetWindow(currentElement);

            // Устанавливаем время окончания блокировки
            endTime = DateTime.Now.AddSeconds(seconds);

            // Сохраняем время в файл
            SaveEndTime(endTime);

            // Добавляем блокирующий элемент
            AddBlockingElement(currentWindow);

            // Запускаем таймер отсчета
            StartTimer();
        }

        /// <summary>
        /// Проверяет и восстанавливает состояние блокировки при запуске приложения
        /// </summary>
        /// <param name="currentElement">Элемент окна для проверки</param>
        public void CheckAndRestoreBlock(DependencyObject currentElement)
        {
            var currentWindow = Window.GetWindow(currentElement);

            if (LoadEndTime(out endTime))
            {
                if (DateTime.Now < endTime)
                {
                    // Если время блокировки еще не истекло
                    AddBlockingElement(currentWindow);
                    StartTimer();
                }
                else
                {
                    // Если блокировка уже закончилась
                    DeleteEndTimeFile();
                }
            }
        }

        /// <summary>
        /// Добавляет полупрозрачный слой с таймером поверх окна
        /// </summary>
        /// <param name="window">Окно для блокировки</param>
        private void AddBlockingElement(Window window)
        {
            if (window == null) return;

            // Создаем полупрозрачный Grid для блокировки
            blockerGrid = new Grid
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 35, 0, 0) // Отступ сверху для заголовка окна
            };

            // Создаем Label для отображения времени
            timeLabel = new System.Windows.Controls.Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            };
            blockerGrid.Children.Add(timeLabel);

            // Добавляем блокирующий элемент в окно
            if (window.Content is Grid mainGrid)
            {
                // Если контент окна уже Grid - добавляем в него
                mainGrid.Children.Add(blockerGrid);
            }
            else
            {
                // Если контент не Grid - создаем новый Grid
                var newGrid = new Grid();
                var existingContent = window.Content;
                window.Content = null;

                newGrid.Children.Add(existingContent as UIElement);
                newGrid.Children.Add(blockerGrid);
                window.Content = newGrid;
            }
        }

        /// <summary>
        /// Запускает таймер для отсчета времени блокировки
        /// </summary>
        private void StartTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Обновление каждую секунду
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        /// <summary>
        /// Обработчик тика таймера - обновляет отображение оставшегося времени
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            var remainingTime = endTime - DateTime.Now;

            if (remainingTime.TotalSeconds > 0)
            {
                // Обновляем Label с оставшимся временем
                timeLabel.Content = $"Осталось времени: {remainingTime.ToString(@"hh\:mm\:ss")}";
            }
            else
            {
                // Время блокировки истекло
                timer.Stop();
                RemoveBlockingElement();
                DeleteEndTimeFile();
            }
        }

        /// <summary>
        /// Удаляет блокирующий элемент из окна
        /// </summary>
        private void RemoveBlockingElement()
        {
            if (blockerGrid != null && blockerGrid.Parent is Grid mainGrid)
            {
                mainGrid.Children.Remove(blockerGrid);
                blockerGrid = null;
                timeLabel = null;
            }
        }

        /// <summary>
        /// Сохраняет время окончания блокировки в файл
        /// </summary>
        /// <param name="endTime">Время окончания блокировки</param>
        private void SaveEndTime(DateTime endTime)
        {
            try
            {
                // Используем формат ISO 8601 для надежного хранения
                File.WriteAllText(FileName, endTime.ToString("o"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения времени блокировки: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает время окончания блокировки из файла
        /// </summary>
        /// <param name="endTime">Переменная для сохранения времени</param>
        /// <returns>True, если файл существует и время успешно загружено</returns>
        private bool LoadEndTime(out DateTime endTime)
        {
            endTime = DateTime.MinValue;
            try
            {
                if (File.Exists(FileName))
                {
                    var timeString = File.ReadAllText(FileName);
                    // Парсим с учетом формата ISO 8601
                    endTime = DateTime.Parse(timeString, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Обработка возможных ошибок чтения/парсинга
                MessageBox.Show($"Ошибка загрузки времени блокировки: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Удаляет файл с временем окончания блокировки
        /// </summary>
        private void DeleteEndTimeFile()
        {
            try
            {
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
            }
            catch (Exception ex)
            {
                // Обработка возможных ошибок удаления файла
                MessageBox.Show($"Ошибка удаления файла блокировки: {ex.Message}");
            }
        }

        /// <summary>
        /// Блокирует окно на неопределенный срок (до конца рабочего дня)
        /// </summary>
        /// <param name="currentElement">Элемент окна для блокировки</param>
        public void BlockWindowIndefinitely(DependencyObject currentElement)
        {
            var currentWindow = Window.GetWindow(currentElement);
            timeLabel.Content = "Рабочий день окончен, возвращайтесь завтра в 10:00";
            AddBlockingElement(currentWindow);
        }

        /// <summary>
        /// Снимает неопределенную блокировку окна
        /// </summary>
        public void RemoveIndefiniteBlocking()
        {
            // Используем ту же логику, что и для обычной разблокировки
            if (blockerGrid != null && blockerGrid.Parent is Grid mainGrid)
            {
                mainGrid.Children.Remove(blockerGrid);
                blockerGrid = null;
                timeLabel = null;
            }
        }
    }
}