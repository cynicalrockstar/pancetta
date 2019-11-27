using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pancetta.Common.Extensions
{
    public class CappedDictionary<K, V> : System.Collections.Generic.Dictionary<K, V>
    {
        public int MaxSize { get; set; }

        private CappedList<K> internalList;

        public CappedDictionary()
        {
            internalList = new CappedList<K>();
        }

        public new void Add(K key, V value)
        {
            base.Add(key, value);
            internalList.Add(key);

            while (this.Count > MaxSize)
            {
                var removeKey = internalList[0];
                internalList.RemoveAt(0);
                this.Remove(removeKey);
            }
        }
    }
}
