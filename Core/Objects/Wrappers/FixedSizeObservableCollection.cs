using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Core.Objects.Wrappers
{
    public class FixedSizeObservableCollection<T> : ObservableCollection<T>
    {
        private object Lock = new object();
        public int Size { get; set; }
        public int LastRemoveIndex { get; private set; } = -1;
        public FixedSizeObservableCollection(int size)
        {
            Size = size;
        }

        public void Insert(T obj)
        {
            bool preRemoved = false;
            if (base.Items.Any(o => o.Equals(obj)))
            {
                preRemoved = true;
                base.Items.Remove(obj);
            }
            base.Insert(0, obj);
            if (preRemoved) return;
            
            lock (Lock)
            {
                for (int i = base.Count-1; i >= Size; i--)
                {
                    base.RemoveAt(i);
                }
            }
        }

        public new void Remove(T obj)
        {
            LastRemoveIndex = base.IndexOf(obj);
            base.RemoveAt(LastRemoveIndex);
        }
    }
}
