using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Documents;

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

        }

        public List<Run> Get()
        {
            throw new NotImplementedException();
        }
    }
}
