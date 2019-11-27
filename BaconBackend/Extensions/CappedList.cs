using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pancetta.Common.Extensions
{
    public class CappedList<T> : List<T>
    {
        public int MaxSize { get; set; }

        public new void Add(T item)
        {
            base.Add(item);

            while (this.Count > MaxSize)
            {
                this.RemoveAt(0);
            }
        }
    }
}
