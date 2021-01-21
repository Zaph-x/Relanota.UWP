using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWP.FrontEnd.Views.Interfaces
{
    interface IContextInteractible<T>
    {
        void Save();
        T Load(int key);
    }
}
