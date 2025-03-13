using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Caching;

namespace Test._ScriptHelpers
{
    public class CacheHelper
    {
        public static T Get<T>(string key) where T : class
        {
            ObjectCache cache = MemoryCache.Default;
            var value = cache.Get(key) as T;
            return value == null ? default(T) : value;
        }

        public static void Set<T>(string key, object value, string filePath) where T : class
        {
            MemoryCache cache = MemoryCache.Default;
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.RemovedCallback = (CacheEntryRemovedArguments arge) =>
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(DateTime.Now.ToString() + "===>" + arge.CacheItem.Key + ":" + arge.RemovedReason);
                stringBuilder.AppendLine(arge.CacheItem.Key + ":" + (arge.CacheItem.Value as T).ToString());
                FileInfo info = new FileInfo(filePath);
                //string path = info.DirectoryName + "\\" + info.Name.Split('.')[0] + ".log";
                string path = Path.GetPathRoot(info.FullName) + "CacheLog\\" + info.Name.Split('.')[0] + ".log";
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                File.AppendAllText(path, stringBuilder.ToString());
            };
            policy.ChangeMonitors.Add(new HostFileChangeMonitor(new List<string> { filePath }));
            cache.Set(key, value, policy);
        }
    }
}