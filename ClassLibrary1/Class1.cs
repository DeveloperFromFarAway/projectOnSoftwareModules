using System;
using System.Security.Cryptography;
using System.Text;

namespace ClassLibrary1
{
    public class Class1
    {
        public string HashPassword(string password) // password принимает методом в виде аргумента
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] sourceBytePassw = Encoding.UTF8.GetBytes(password);
                byte[] hashSourceBytePassw = sha256Hash.ComputeHash(sourceBytePassw);
                string hashPassw = BitConverter.ToString(hashSourceBytePassw).Replace("-", string.Empty);
                return hashPassw; // возвращаемое методом строковое значение
            }
        }
    }
}