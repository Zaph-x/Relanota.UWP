using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWP.FrontEnd.Views.Interfaces
{
    public interface IEditorView
    {
        bool AreTextboxesEmpty();
        (int, int) CalculatePoint(string text, int currentIndex);
        void Update(string content);
    }
}
