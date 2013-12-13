using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class LinqExtention
    {
        public static void Each<T>(this IEnumerable<T> source,Action<T> func)
        {
            foreach (var item in source)
            {
                func(item);
            }
        }
        public static void Each<T>(this IEnumerable<T> source, Func<T, bool> func)
        {
            foreach (var item in source)
            {
                if (!func(item))
                    break;
            }
        }
        
    }
}
