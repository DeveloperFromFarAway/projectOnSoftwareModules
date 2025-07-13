using PR3.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary1;
using System.Windows;
using System.Data.Entity.Infrastructure;

namespace PR3
{
    /// <summary>
    /// Вспомогательный класс для работы с базой данных и бизнес логикой приложения
    /// </summary>
    internal class Helper
    {
        /// <summary>
        /// Создает и возвращает новый контекст базы данных
        /// </summary>
        /// <returns>Экземпляр DOPSEntities для работы с БД</returns>
        public static DOPSEntities GetContext()
        {
            return new DOPSEntities();
        }

        /*
         * Хэширует пароль с помощью внешнего класса Class1
         * Внимание: Class1 должен реализовывать надежный алгоритм хэширования
         */
        private string HashPassword(string password)
        {
            Class1 hasher = new Class1();
            return hasher.HashPassword(password); // Возвращает хэш пароля
        }

        /// <summary>
        /// Создает новую учетную запись в системе
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль в открытом виде</param>
        public void CreateAccount(string login, string password)
        {
            using (var context = GetContext())
            {
                var hashedPassword = HashPassword(password);

                // Получаем максимальный ID для генерации нового
                int maxID = context.Table_Account.Any() ? context.Table_Account.Max(a => a.ID_Account) : 0;

                var account = new Table_Account
                {
                    ID_Account = maxID + 1, // Автоинкремент вручную
                    Login = login,
                    Password = hashedPassword,
                    ID_Position = 6,         // Значение по умолчанию
                    Permission = 0           // Обычные права доступа
                };

                context.Table_Account.Add(account);
                context.SaveChanges(); // Сохранение изменений в БД
            }
        }

        /// <summary>
        /// Аутентифицирует пользователя в системе
        /// </summary>
        /// <param name="login">Логин или контактная информация</param>
        /// <param name="password">Пароль в открытом виде</param>
        /// <returns>Кортеж с результатом аутентификации и дополнительной информацией</returns>
        public (bool isAuthenticated, string message, string positionName, string userName, string userLogin) AuthenticateUser(string login, string password)
        {
            using (var context = GetContext())
            {
                var hashedPassword = HashPassword(password);

                /*
                 * Сложный запрос на поиск аккаунта:
                 * - Ищет по логину ИЛИ контактной информации сотрудника
                 * - Проверяет соответствие пароля
                 */
                var account = context.Table_Account
                    .FirstOrDefault(a =>
                        (a.Login == login ||
                         context.Table_Employees.Any(e => e.ContactInfo == login && e.ID_Account == a.ID_Account)) &&
                         a.Password == hashedPassword);

                if (account == null)
                    return (false, "Неверные учетные данные", null, null, null);

                // Проверка прав доступа (1 - разрешенный доступ)
                if (account.Permission != 1)
                    return (false, "Недостаточно прав для входа", null, null, null);

                var employee = context.Table_Employees.FirstOrDefault(e => e.ID_Account == account.ID_Account);

                // Получаем название должности из связанной таблицы
                string positionName = account.Table_Position.Name.ToString();

                if (string.IsNullOrEmpty(positionName))
                    return (false, "Позиция не найдена", null, employee?.Name, account.Login);

                return (true, "Успешно", positionName, employee?.Name, account.Login);
            }
        }

        /// <summary>
        /// Класс для хранения данных о пользователе
        /// </summary>
        public class UserData
        {
            public bool IsEmployee { get; set; }    // Является ли сотрудником
            public bool IsAccount { get; set; }     // Имеет ли аккаунт
            public string Name { get; set; }        // ФИО сотрудника
            public string Email { get; set; }       // Контактная информация
            public string Role { get; set; }        // Роль/должность
            public string Login { get; set; }      // Логин аккаунта
            public int? AccountId { get; set; }     // ID аккаунта
            public int? EmployeeId { get; set; }    // ID сотрудника
        }

        /// <summary>
        /// Получает объединенные данные о сотрудниках и аккаунтах
        /// </summary>
        /// <returns>Список пользователей системы</returns>
        public List<UserData> GetUserAccountData()
        {
            using (var context = GetContext())
            {
                /*
                 * Запрос для сотрудников с аккаунтами (LEFT JOIN)
                 * Включает даже сотрудников без аккаунтов
                 */
                var employeesQuery = from employee in context.Table_Employees
                                     join account in context.Table_Account
                                     on employee.ID_Account equals account.ID_Account into empWithAcc
                                     from acc in empWithAcc.DefaultIfEmpty()
                                     select new UserData
                                     {
                                         IsEmployee = true,
                                         IsAccount = acc != null,
                                         Name = employee.Name,
                                         Email = employee.ContactInfo,
                                         Role = "Сотрудник",
                                         Login = acc == null ? null : acc.Login,
                                         AccountId = acc != null ? acc.ID_Account : (int?)null,
                                         EmployeeId = employee.ID_Employees
                                     };

                // Запрос для аккаунтов без привязанных сотрудников
                var accountsWithoutEmployeesQuery = from account in context.Table_Account
                                                    where !(from employee in context.Table_Employees
                                                            select employee.ID_Account).Contains(account.ID_Account)
                                                    select new UserData
                                                    {
                                                        IsEmployee = false,
                                                        IsAccount = true,
                                                        Name = (string)null,
                                                        Email = (string)null,
                                                        Role = "Аккаунт",
                                                        Login = account.Login,
                                                        AccountId = account.ID_Account,
                                                        EmployeeId = (int?)null
                                                    };

                // Объединение двух выборок
                var query = employeesQuery.Union(accountsWithoutEmployeesQuery);

                return query.ToList(); // Материализация запроса
            }
        }

        /// <summary>
        /// Создает сотрудника с привязанным аккаунтом и данными для договора
        /// </summary>
        /// <param name="employeeData">Данные сотрудника</param>
        /// <param name="login">Логин для аккаунта</param>
        /// <param name="password">Пароль в открытом виде</param>
        /// <param name="positionId">ID должности</param>
        public void CreateEmployeeWithAccount(EmployeeFullData employeeData, string login, string password, int positionId)
        {
            using (var context = GetContext())
            {
                var hashedPassword = HashPassword(password);

                // Создание аккаунта
                var account = new Table_Account
                {
                    Login = login,
                    Password = hashedPassword,
                    ID_Position = positionId,
                    Permission = 0 // Стандартные права
                };

                context.Table_Account.Add(account);
                context.SaveChanges();

                // Создание сотрудника с полными данными
                var employee = new Table_Employees
                {
                    ID_Position = positionId,
                    Name = employeeData.FullName,
                    ContactInfo = employeeData.ContactInfo,
                    HireDate = employeeData.HireDate,
                    WorkAddress = employeeData.WorkAddress,
                    ProbationPeriod = employeeData.ProbationPeriod,
                    PassportData = employeeData.PassportData,
                    INN = employeeData.INN,
                    SNILS = employeeData.SNILS,
                    TaxExemption = employeeData.TaxExemption,
                    ContractNumber = GenerateContractNumber(),
                    Isonduty = false,
                    PatrolledObjectsCount = 0,
                    ID_Account = account.ID_Account
                };

                context.Table_Employees.Add(employee);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Генерирует номер договора
        /// </summary>
        private string GenerateContractNumber()
        {
            return $"ТД-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        /// <summary>
        /// Полная модель данных сотрудника
        /// </summary>
        public class EmployeeFullData
        {
            public string FullName { get; set; }
            public string ContactInfo { get; set; }
            public DateTime HireDate { get; set; }
            public string WorkAddress { get; set; }
            public int ProbationPeriod { get; set; }
            public string PassportData { get; set; }
            public string INN { get; set; }
            public string SNILS { get; set; }
            public bool TaxExemption { get; set; }
        }

        /// <summary>
        /// Получает полную информацию о сотруднике или аккаунте
        /// </summary>
        public EmployeeFullInfo GetEmployeeOrAccountInfo(int? userId, bool? isAccountId)
        {
            using (var context = GetContext())
            {
                var result = (from employee in context.Table_Employees
                              join account in context.Table_Account on employee.ID_Account equals account.ID_Account into empWithAcc
                              from acc in empWithAcc.DefaultIfEmpty()
                              join position in context.Table_Position on employee.ID_Position equals position.ID_Position
                              where (isAccountId == true && acc.ID_Account == userId) ||
                                    (isAccountId != true && employee.ID_Employees == userId)
                              select new EmployeeFullInfo
                              {
                                  EmployeeId = employee.ID_Employees,
                                  AccountId = acc != null ? acc.ID_Account : (int?)null,
                                  FullName = employee.Name,
                                  ContactInfo = employee.ContactInfo,
                                  HireDate = employee.HireDate,
                                  WorkAddress = employee.WorkAddress,
                                  ProbationPeriod = employee.ProbationPeriod,
                                  PassportData = employee.PassportData,
                                  INN = employee.INN,
                                  SNILS = employee.SNILS,
                                  TaxExemption = employee.TaxExemption,
                                  ContractNumber = employee.ContractNumber,
                                  Login = acc != null ? acc.Login : null,
                                  PositionId = employee.ID_Position,
                                  PositionName = position.Name
                              }).FirstOrDefault();

                if (result == null && isAccountId == true)
                {
                    result = (from account in context.Table_Account
                              where account.ID_Account == userId
                              select new EmployeeFullInfo
                              {
                                  AccountId = account.ID_Account,
                                  Login = account.Login,
                                  PositionId = account.ID_Position
                              }).FirstOrDefault();
                }

                return result;
            }
        }

        /// <summary>
        /// Полная информация о сотруднике/аккаунте
        /// </summary>
        public class EmployeeFullInfo
        {
            public int? EmployeeId { get; set; }
            public int? AccountId { get; set; }
            public string FullName { get; set; }
            public string ContactInfo { get; set; }
            public DateTime? HireDate { get; set; }
            public string WorkAddress { get; set; }
            public int? ProbationPeriod { get; set; }
            public string PassportData { get; set; }
            public string INN { get; set; }
            public string SNILS { get; set; }
            public bool? TaxExemption { get; set; }
            public string ContractNumber { get; set; }
            public string Login { get; set; }
            public int? PositionId { get; set; }
            public string PositionName { get; set; }
        }

        /// <summary>
        /// Обновляет данные сотрудника и аккаунта
        /// </summary>
        public void UpdateEmployeeWithAccount(EmployeeFullInfo employeeInfo)
        {
            using (var context = GetContext())
            {
                if (employeeInfo.AccountId.HasValue)
                {
                    var account = context.Table_Account.FirstOrDefault(a => a.ID_Account == employeeInfo.AccountId);
                    if (account != null)
                    {
                        account.Login = employeeInfo.Login;
                        account.ID_Position = employeeInfo.PositionId ?? account.ID_Position;
                    }
                }

                if (employeeInfo.EmployeeId.HasValue)
                {
                    var employee = context.Table_Employees.FirstOrDefault(e => e.ID_Employees == employeeInfo.EmployeeId);
                    if (employee != null)
                    {
                        employee.Name = employeeInfo.FullName;
                        employee.ContactInfo = employeeInfo.ContactInfo;
                        employee.HireDate = employeeInfo.HireDate ?? employee.HireDate;
                        employee.WorkAddress = employeeInfo.WorkAddress;
                        employee.ProbationPeriod = employeeInfo.ProbationPeriod ?? employee.ProbationPeriod;
                        employee.PassportData = employeeInfo.PassportData;
                        employee.INN = employeeInfo.INN;
                        employee.SNILS = employeeInfo.SNILS;
                        employee.TaxExemption = employeeInfo.TaxExemption ?? employee.TaxExemption;
                        employee.ID_Position = employeeInfo.PositionId ?? employee.ID_Position;
                    }
                }

                context.SaveChanges();
            }
        }
    }
}
