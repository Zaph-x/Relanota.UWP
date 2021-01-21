using Core.Interfaces;
using Core.SqlHelper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UWP.FrontEnd.BindableWrappers
{
    class BindableTuple
    {
        public string Name { get; }
        public int Key { get; }

        public BindableTuple(string name, int key)
        {
            Name = name;
            Key = key;
        }

        public T Get<T>() where T : ISqlEntity
        {
            T obj = default;
            using (Database context = new Database())
            {
                obj = context.Set(obj).FirstOrDefault(o => o.Key == Key); 
            }
            return obj;
        }

        
    }

    
}
