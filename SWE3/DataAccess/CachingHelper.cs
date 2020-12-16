using System.Collections.Generic;
using System.Linq;
using SWE3.BusinessLogic.Entities;

namespace SWE3.DataAccess
{
    /// <summary>
    /// Caching up to X items, while also providing a little bit of change tracking.
    /// </summary>
    public static class CachingHelper
    {
        private static Dictionary<(string table, int id),dynamic> cachedItems = new Dictionary<(string, int),dynamic>();
        private const int MaxCacheCount = 15;

        public static void Set(string table, int key, dynamic value)
        {
            var clone = value is Cloneable cloneable ? cloneable.Clone<dynamic>() : value;
            cachedItems.Add((table,key), clone);
            cleanCache();
        }

        public static void Set(int key, dynamic value)
        {
            Set(value.GetType().Name, key, value);
            cleanCache();
        }

        public static dynamic Get<T>(string table, int key)
        {
            cachedItems.TryGetValue((table,key), out object item);
            return (T) item;
        }

        public static dynamic Get<T>(int key)
        {
            return Get<T>(typeof(T).Name, key);
        }

        public static void Remove(string table, int key)
        {
            cachedItems.Remove((table,key));
        }

        public static void ClearCache()
        {
            cachedItems.Clear();
        }

        private static void cleanCache()
        {
            for(var i = cachedItems.Count; i > MaxCacheCount; i--)
            {
                cachedItems.Remove(cachedItems.FirstOrDefault().Key);
            }
        }
    }
}