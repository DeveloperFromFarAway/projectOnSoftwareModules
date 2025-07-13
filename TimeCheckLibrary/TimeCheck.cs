using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeCheckLibrary
{
    /// <summary>
    /// Класс для проверки рабочего времени приложения
    /// </summary>
    public class TimeCheck
    {
        /// <summary>
        /// Проверяет, находится ли текущее время в рабочем интервале
        /// </summary>
        /// <returns>
        /// true - если текущее время между 10:00 и 19:00 (включительно),
        /// false - в противном случае
        /// </returns>
        public static bool IsWorkingTime()
        {
            // Получаем текущее время (без даты)
            var now = DateTime.Now.TimeOfDay;

            // Устанавливаем границы рабочего времени
            var workStart = new TimeSpan(10, 0, 0); // 10:00 утра
            var workEnd = new TimeSpan(19, 0, 0);   // 19:00 вечера

            // Проверяем попадание текущего времени в рабочий интервал
            return now >= workStart && now <= workEnd;
        }
    }
}
