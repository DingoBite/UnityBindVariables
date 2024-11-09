using System.Collections.Generic;

namespace Bind
{
    public class BindDict<TKey, TValue> : Bind<Dictionary<TKey, TValue>>
    {
        public BindDict()
        {
            V = new Dictionary<TKey, TValue>();
        }
    }
    
    public class BindList<TValue> : Bind<List<TValue>>
    {
        public BindList()
        {
            V = new List<TValue>();
        }
    }
}