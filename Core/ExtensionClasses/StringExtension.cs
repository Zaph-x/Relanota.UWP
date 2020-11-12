
using System.Text;

namespace Core.ExtensionClasses
{
    public static class StringExtensions
    {
        public static string Repeat(this string str, int times)
        {
            string newString = str;
            for (int i = 0; i < times-1; i++)
            {
                newString += str;
            }
            return newString;
        }

        public static string CreateMD5(this string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}