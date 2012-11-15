using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBrowser
{
    public class DataCache
    {
        private class CacheEntry
        {
            internal Object Data { get; set; }
            internal DateTime Expires { get; set; }
        }

        private Dictionary<string, CacheEntry> _cacheEntries;
        private object _lock = new object();

        private static DataCache _instance;

        public DataCache()
        {
            _cacheEntries = new Dictionary<string, CacheEntry>();
        }

        public static DataCache Instance
        {
            get
            {
                if (_instance == null) _instance = new DataCache();
                return _instance;
            }
        }

        public T Lookup<T>(string key) where T : class
        {
            lock (_lock)
            {
                CacheEntry entry;
                _cacheEntries.TryGetValue(key, out entry);
                if (entry == null) return null;
                if (entry.Expires < DateTime.Now)
                {
                    _cacheEntries.Remove(key);
                    return null;
                }
                return entry.Data as T;
            }
        }

        public void Cache(string key, object data)
        {
            lock (_lock)
            {
                if (_cacheEntries.ContainsKey(key))
                {
                    var cacheEntry = _cacheEntries[key];
                    cacheEntry.Data = data;
                    cacheEntry.Expires = DateTime.Now.AddMinutes(5);
                }
                else
                {
                    var cacheEntry = new CacheEntry();
                    cacheEntry.Data = data;
                    cacheEntry.Expires = DateTime.Now.AddMinutes(5);
                    _cacheEntries.Add(key, cacheEntry);
                }
            }
        }

    }
}
