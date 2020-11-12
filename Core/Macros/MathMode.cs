using Core.ExtensionClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Core.Macros
{
    public class MathMode
    {
        string Text { get; set; }
        public MathMode(string text)
        {
            this.Text = text;
        }

        public string Process(string cachePath)
        {
            MatchCollection matches = Regex.Matches(Text, @"\$(.+?)\$");
            foreach (Match match in matches)
                if (match.Success)
                {
                    string encodedString = HttpUtility.UrlEncode(match.Groups[1].Value.Replace(" ", ""))
                        .Replace("(", "%28")
                        .Replace(")", "%29");
                    string md5 = ("https://latex.codecogs.com/png.latex?" + encodedString).CreateMD5();
                    //if (!File.Exists(ApplicationData.Current.LocalFolder.Path + @"\" + md5 + ".jpg"))
                    if (!File.Exists(cachePath))
                    {
                        if (match.Groups[1].Value.Length > 0)
                            Text = Text.Replace(match.Groups[0].Value, "![latex math](https://latex.codecogs.com/png.latex?" + encodedString + ")");
                    }
                    else
                    {
                        Text = Text.Replace(match.Groups[0].Value, $@"![cached image]({cachePath}\{md5}.jpg)");
                    }

                }
            return Text;
        }
    }
}
