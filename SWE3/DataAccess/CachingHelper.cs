using System.Collections.Generic;
using System.Linq;
using SWE3.BusinessLogic.Entities;

namespace SWE3.DataAccess
{
    /// <summary>
    /// Caching up to X items.
    /// If the user allows, a clone is cached,
    /// which keeps the cache in sync with the database, instead of the objects initially cached instance
    /// </summary>
    public static class CachingHelper
    {
        private static Dictionary<(string table, int id),dynamic> cachedItems = new Dictionary<(string, int),dynamic>();
        private const int MaxCacheCount = 15;

        /// <summary>
        /// Caches an item (with table and key, as key)
        /// </summary>
        public static void Set(string table, int key, dynamic value)
        {
            var clone = value is Cloneable cloneable ? cloneable.Clone<dynamic>() : value;
            cachedItems.Add((table,key), clone);
            cleanCache();
        }

        /// <summary>
        /// Caches an item, as seen above, with an inferred table-name
        /// </summary>
        public static void Set(int key, dynamic value)
        {
            Set(value.GetType().Name, key, value);
            cleanCache();
        }

        /// <summary>
        /// Gets an item from the cache
        /// and returns it as a typed object
        /// </summary>
        public static dynamic Get<T>(string table, int key)
        {
            cachedItems.TryGetValue((table,key), out object item);
            return (T) item;
        }

        /// <summary>
        /// Gets an item from the cache
        /// with an inferred table-name
        /// and return it as a typed object
        /// </summary>
        public static dynamic Get<T>(int key)
        {
            return Get<T>(typeof(T).Name, key);
        }

        /// <summary>
        /// Removes a cahced item from the cache
        /// </summary>
        public static void Remove(string table, int key)
        {
            cachedItems.Remove((table,key));
        }

        /// <summary>
        /// Removes all items from the cache
        /// </summary>
        public static void ClearCache()
        {
            cachedItems.Clear();
        }

        /// <summary>
        /// Removes items from the cache, so it does not exceed the maximum amount
        /// </summary>
        private static void cleanCache()
        {
            for(var i = cachedItems.Count; i > MaxCacheCount; i--)
            {
                cachedItems.Remove(cachedItems.FirstOrDefault().Key);
            }
        }
    }
}