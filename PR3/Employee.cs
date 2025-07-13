using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace PR3
{
    public class Employee
    {
        [Required(ErrorMessage = "ФИО обязательно для заполнения")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "ФИО должно быть от 3 до 100 символов")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Контактная информация обязательна")]
        [StringLength(100, ErrorMessage = "Контактная информация не должна превышать 100 символов")]
        public string ContactInfo { get; set; }

        [Required(ErrorMessage = "Дата приема на работу обязательна")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
        public DateTime HireDate { get; set; }

        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Логин может содержать только буквы и цифры")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        [Range(1, int.MaxValue, ErrorMessage = "Выберите роль")]
        public int RoleId { get; set; }
    }
}
