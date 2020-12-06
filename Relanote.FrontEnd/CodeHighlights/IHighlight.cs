using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Documents;

namespace UWP.FrontEnd.CodeHighlights
{
    public interface IHighlight
    {
        string[] Keywords { get; set; }
        List<Run> Runs { get; set; }
        string CodeBlock { get; set; }

        public void Parse();

        public List<Run> Get();
    }
}
