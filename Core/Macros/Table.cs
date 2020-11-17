using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Macros
{
    public class Table : TextComponent
    {
        private int Columns { get; set; }
        private int Rows { get; set; }
        string Text { get; set; }
        
        public Table(string text)
        {
            Text = text;
        }

        public override string WriteComponent()
        {
            string res = "";
            MatchCollection matches = Regex.Matches(Text, @".*?(?<!`)(\\table\{(\d+,\d+)})");
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string[] colAndRow = match.Groups[2].Value.Split(',');
                    this.Columns = int.Parse(colAndRow[0]);
                    this.Rows = int.Parse(colAndRow[1]);
                    res += DrawRow();
                    res += DrawSpacer();
                    for (int i = 0; i < Rows; i++)
                    {
                        res += DrawRow();
                    }
                    Text = Text.Replace(match.Groups[1].Value, res);
                }
            }

            return Text;
        }


        private string DrawRow()
        {
            string row = "";
            row += "|";
            for (int i = 0; i < Columns; i++)
            {
                row += "    |";
            }
            row += Environment.NewLine;
            return row;
        }

        private string DrawSpacer()
        {
            string row = "";
            row += "|";
            for (int i = 0; i < Columns; i++)
            {
                row += "----|";
            }
            row += Environment.NewLine;
            return row;
        }
    }
}
