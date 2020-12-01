using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace UWP.FrontEnd.CodeHighlights
{
    class PythonHighlighting : IHighlight
    {
        public string[] Keywords { get; set; } = { "def", "str", "int", "import",
            "from", "as", "in", "float", "assert", "False", "await", "True", "if",
            "else", "None", "elif", "try", "catch", "finally", "break", "class",
            "for", "lambda", "nonlocal", "while", "pass", "except", "raise" };
        public List<Run> Runs { get; set; }
        public string CodeBlock { get; set; }

        public PythonHighlighting(string codeBlock)
        {
            CodeBlock = codeBlock;
        }

        public void Parse()
        {
            string word = "";
            bool isComment = false;
            int i = 0;
            while (i < CodeBlock.Length)
            {
                if ('#' == CodeBlock[i])
                {
                    isComment = true;
                    Runs.Add(new Run() { Text = CodeBlock[i].ToString(), Foreground = new SolidColorBrush(Color.FromArgb(255, 50, 255, 0))});
                } else if (" \n\t.:;()[]{}-+*/".Contains(CodeBlock[i]))
                {
                    Runs.Add(new Run() { Text = CodeBlock[i].ToString() });
                    if (CodeBlock[i] == '\n') isComment = false;
                }

            }
        }

        public List<Run> Get()
        {
            throw new NotImplementedException();
        }
    }
}
