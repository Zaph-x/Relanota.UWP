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
    public class MathMode : TextComponent
    {
        string Text { get; set; }
        string CachePath { get; set; }
        public MathMode(string text, string cachePath)
        {
            this.Text = text;
            this.CachePath = cachePath;
        }

        public override string WriteComponent()
        {
            //foreach (string line in Text.Split(Environment.NewLine.ToCharArray()))
            //{
            MatchCollection matches = Regex.Matches(Text, @"(?<!`)(\$(.+?(?<!\\))\$)", RegexOptions.None);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string encodedString = Uri.EscapeUriString(Regex.Replace(match.Groups[2].Value.Trim(), @"\s+", " "))
                        .Replace("(", "%28")
                        .Replace(")", "%29");
                    string md5 = ("https://latex.codecogs.com/png.latex?" + encodedString).CreateMD5();
                    if (md5 == "7D96D13FD3EFC5D8E09C59C200E7B304") // Empty query
                        return Text;
                    if (!File.Exists($@"{CachePath}\{md5}.jpg"))
                    {
                        if (match.Groups[2].Value.Length > 0)
                            Text = Text.Replace(match.Groups[1].Value, "![latex math](https://latex.codecogs.com/png.latex?" + encodedString + ")");
                    }
                    else
                    {
                        Text = Text.Replace(match.Groups[1].Value, $@"![cached image]({CachePath}\{md5}.jpg)");
                    }

                }

            }
            return Text;
        }
    }
}
