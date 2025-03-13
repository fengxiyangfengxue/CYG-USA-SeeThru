using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace Test._ScriptExtensions
{
    public static class ConcurrentDictionaryExtension
    {
        public static T TryGetValue<T>(this ConcurrentDictionary<string, object> dic, string key)
        {
            object obj;
            if (dic.TryGetValue(key, out obj))
            {
                return (T)obj;
            }
            else
            {
                if (typeof(T) == typeof(string))
                {
                    obj = string.Empty;
                    return (T)obj;
                }
                return default(T);
            }
        }

        public static void TryAddValue(this ConcurrentDictionary<string, object> dic, string key, int i)
        {
            object o;
            if (dic.TryGetValue(key, out o))
            {
                var v = (int)o;
                dic[key] = v + i;
            }
            else
            {
                dic[key] = i;
            }
        }

        /// <summary>
        /// 往list<T>中增加T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="i"></param>
        public static void TryAddList<T>(this ConcurrentDictionary<string, object> dic, string key, T i)
        {
            object o;
            if (dic.TryGetValue(key, out o))
            {
                var v = o as List<T>;
                if (!v.Contains(i))
                {
                    v.Add(i);
                    dic[key] = v;
                }
            }
            else
            {
                dic[key] = new List<T> { i };
            }
        }
    }
}
