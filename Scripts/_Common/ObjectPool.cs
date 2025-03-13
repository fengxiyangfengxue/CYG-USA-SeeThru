using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Test._Definitions
{
    public class ObjectPool
    {
        private ConcurrentDictionary<Type, Dictionary<string, object>> pool = new ConcurrentDictionary<Type, Dictionary<string, object>>();

        public void Clear<T>()
        {
            Type typeFromHandle = typeof(T);
            if (pool.ContainsKey(typeFromHandle))
            {
                pool[typeFromHandle].Clear();
            }
        }

        private Dictionary<string, object> CreateValue(Type key)
        {
            return new Dictionary<string, object>();
        }

        public void Add<T>(string key, T obj)
        {
            Type typeFromHandle = typeof(T);
            Dictionary<string, object> orAdd = pool.GetOrAdd(typeFromHandle, CreateValue);
            if (orAdd.ContainsKey(key))
            {
                throw new Exception($"ObjectPool有重复的键{key},{typeFromHandle.FullName}");
            }

            orAdd.Add(key, obj);
        }

        public void Add<T>(T obj)
        {
            Add(string.Empty, obj);
        }

        public void Update<T>(T obj)
        {
            Update(string.Empty, obj);
        }

        public void Update<T>(string key, T obj)
        {
            Type typeFromHandle = typeof(T);
            pool.GetOrAdd(typeFromHandle, CreateValue)[key] = obj;
        }
        /// <summary>
        /// 自动累加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        public void AccumulationUpdate(string key, int obj)
        {
            Type typeFromHandle = typeof(int);
            var orAdd = pool.GetOrAdd(typeFromHandle, CreateValue);
            if (orAdd.ContainsKey(key))
            {
                int value = (int)orAdd[key];
                Update(key, value + obj);
            }
            else
            {
                orAdd[key] = obj;
            }
        }
        public void Remove<T>(string key)
        {
            Type typeFromHandle = typeof(T);
            pool.GetOrAdd(typeFromHandle, CreateValue).Remove(key);
        }

        public void Remove<T>()
        {
            Remove<T>(string.Empty);
        }

        public T Find<T>(string key)
        {
            Type typeFromHandle = typeof(T);
            Dictionary<string, object> orAdd = pool.GetOrAdd(typeFromHandle, CreateValue);
            if (!orAdd.ContainsKey(key))
            {
                return default(T);
            }
            return (T)orAdd[key];
        }

        public T Find<T>()
        {
            return Find<T>(string.Empty);
        }

        public T Get<T>()
        {
            return Get<T>(string.Empty);
        }

        public T Get<T>(string key)
        {
            Type typeFromHandle = typeof(T);
            return (T)pool.GetOrAdd(typeFromHandle, CreateValue)[key];
        }

        public object Get(Type tt, string key = "")
        {
            return pool.GetOrAdd(tt, CreateValue)[key];
        }

        public object Find(Type tt, string key = "")
        {
            Dictionary<string, object> orAdd = pool.GetOrAdd(tt, CreateValue);
            if (!orAdd.ContainsKey(key))
            {
                return null;
            }

            return orAdd[key];
        }

        public void Update(Type tt, string key, object obj)
        {
            pool.GetOrAdd(tt, CreateValue)[key] = obj;
        }

        public void ForEach(Action<Dictionary<string, object>> callback)
        {
            foreach (KeyValuePair<Type, Dictionary<string, object>> item in pool)
            {
                if (item.Key.IsInterface || item.Key.IsClass)
                {
                    callback?.Invoke(item.Value);
                }
            }
        }

        public void ForEach(Action<object> callback)
        {
            foreach (KeyValuePair<Type, Dictionary<string, object>> item in pool)
            {
                if (!item.Key.IsInterface && !item.Key.IsClass)
                {
                    continue;
                }

                foreach (object value in item.Value.Values)
                {
                    if (value != null)
                    {
                        callback?.Invoke(value);
                    }
                }
            }
        }
    }
}
