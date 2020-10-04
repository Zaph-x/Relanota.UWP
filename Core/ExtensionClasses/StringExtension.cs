
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
    }
}