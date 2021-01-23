using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWP.FrontEnd.Components.Helpers
{
    public class Formatter
    {
        public static (string Text, int Start, int End) ApplyFormatting(string text, string formatter, int index, int length)
        {
            text = text.Insert(index, formatter);
            text = text.Insert(index + length + formatter.Length, formatter);
            index += formatter.Length;
            return (text, index, length);
        }
    }
}
