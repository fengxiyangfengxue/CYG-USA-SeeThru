using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Test._ScriptHelpers
{
    public class XmlSettingCacheHelper
    {
        //加载mes配置
        private static string pathBase = "C:\\Caesar\\Configs";

        public static T Instance<T>() where T : class, new()
        {
            var type = typeof(T);
            string name = ((XmlRootAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(XmlRootAttribute))).ElementName;
            string path = $"{pathBase}\\" + name + ".xml";

            if (File.Exists(path))
            {
                //通过MemoryCache缓存来存储setting值,MemoryCache会自动监控setting中的变化,如果监控到变化则将会清空cache
                //此时需要重新对MemoryCache进行赋值,不需要重新打开框架
                var value = CacheHelper.Get<T>(name);
                if (value != null)
                {
                    return value;
                }
                string text = File.ReadAllText(path);
                using (StringReader sr = new StringReader(text))
                {
                    XmlSerializer xz = new XmlSerializer(typeof(T));
                    value = xz.Deserialize(sr) as T;
                }
                if (value != null)
                {
                    CacheHelper.Set<T>(name, value, path);
                    return value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var info = new FileInfo(path);
                if (!info.Directory.Exists)
                {
                    Directory.CreateDirectory(info.Directory.FullName);
                }

                var entity = new T();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                XmlWriterSettings setting = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(path, setting))
                {
                    xmlSerializer.Serialize(xmlWriter, entity, namespaces);
                }
                CacheHelper.Set<T>(name, entity, path);
                return entity;
            }
        }

        /// <summary>
        /// 每个slot用不同的setting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T InstanceByName<T>(string name) where T : class, new()
        {
            //加载mes配置
            string path = $"{pathBase}\\{name}.xml";
            if (File.Exists(path))
            {
                //通过MemoryCache缓存来存储setting值,MemoryCache会自动监控setting中的变化,如果监控到变化则将会清空cache
                //此时需要重新对MemoryCache进行赋值,不需要重新打开框架
                var value = CacheHelper.Get<T>(name);
                if (value != null)
                {
                    return value;
                }
                string text = File.ReadAllText(path);
                using (StringReader sr = new StringReader(text))
                {
                    XmlSerializer xz = new XmlSerializer(typeof(T));
                    value = xz.Deserialize(sr) as T;
                }
                if (value != null)
                {
                    CacheHelper.Set<T>(name, value, path);
                    return value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var info = new FileInfo(path);
                if (!info.Directory.Exists)
                {
                    Directory.CreateDirectory(info.Directory.FullName);
                }

                var entity = new T();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                XmlWriterSettings setting = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(path, setting))
                {
                    xmlSerializer.Serialize(xmlWriter, entity, namespaces);
                }
                CacheHelper.Set<T>(name, entity, path);
                return entity;
            }
        }

        public static T Instance<T>(string localPath) where T : class, new()
        {
            var type = typeof(T);
            string name = ((XmlRootAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(XmlRootAttribute))).ElementName;
            //加载mes配置
            string path = localPath + "\\" + name + ".xml";
            if (File.Exists(path))
            {
                //通过MemoryCache缓存来存储setting值,MemoryCache会自动监控setting中的变化,如果监控到变化则将会清空cache
                //此时需要重新对MemoryCache进行赋值,不需要重新打开框架
                var value = CacheHelper.Get<T>(name);
                if (value != null)
                {
                    return value;
                }
                string text = File.ReadAllText(path);
                using (StringReader sr = new StringReader(text))
                {
                    XmlSerializer xz = new XmlSerializer(typeof(T));
                    value = (T)xz.Deserialize(sr);
                }
                if (value != null)
                {
                    CacheHelper.Set<T>(name, value, path);
                    return value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var info = new FileInfo(path);
                if (!info.Directory.Exists)
                {
                    Directory.CreateDirectory(info.Directory.FullName);
                }

                var entity = new T();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                XmlWriterSettings setting = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(path, setting))
                {
                    xmlSerializer.Serialize(xmlWriter, entity, namespaces);
                }
                CacheHelper.Set<T>(name, entity, path);
                return entity;
            }
        }

        public static T Instance<T>(string localPath, string name) where T : class, new()
        {
            var type = typeof(T);
            //加载mes配置
            string path = localPath + "\\" + name + ".xml";
            if (File.Exists(path))
            {
                //通过MemoryCache缓存来存储setting值,MemoryCache会自动监控setting中的变化,如果监控到变化则将会清空cache
                //此时需要重新对MemoryCache进行赋值,不需要重新打开框架
                var value = CacheHelper.Get<T>(name);
                if (value != null)
                {
                    return value;
                }
                string text = File.ReadAllText(path);
                using (StringReader sr = new StringReader(text))
                {
                    XmlSerializer xz = new XmlSerializer(typeof(T));
                    value = (T)xz.Deserialize(sr);
                }
                if (value != null)
                {
                    CacheHelper.Set<T>(name, value, path);
                    return value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var info = new FileInfo(path);
                if (!info.Directory.Exists)
                {
                    Directory.CreateDirectory(info.Directory.FullName);
                }

                var entity = new T();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                XmlWriterSettings setting = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(path, setting))
                {
                    xmlSerializer.Serialize(xmlWriter, entity, namespaces);
                }
                CacheHelper.Set<T>(name, entity, path);
                return entity;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="localPath"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T DeserializeInstance<T>(string localPath, string key) where T : class, new()
        {
            if (File.Exists(localPath))
            {
                //通过MemoryCache缓存来存储setting值,MemoryCache会自动监控setting中的变化,如果监控到变化则将会清空cache
                //此时需要重新对MemoryCache进行赋值,不需要重新打开框架
                var value = CacheHelper.Get<T>(localPath);
                if (value != null)
                {
                    return value;
                }
                string text = File.ReadAllText(localPath);
                value = JsonConvert.DeserializeObject<T>(text);
                if (value != null)
                {
                    CacheHelper.Set<T>(key, value, localPath);
                    return value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var info = new FileInfo(localPath);
                if (!info.Directory.Exists)
                {
                    Directory.CreateDirectory(info.Directory.FullName);
                }
                var entity = new T();
                string txt = JsonConvert.SerializeObject(entity);
                File.WriteAllText(localPath, txt);
                CacheHelper.Set<T>(key, entity, localPath);
                return entity;
            }
        }

        //internal static ISerialPortSetting Instance<T>(object value)
        //{
        //    throw new NotImplementedException();
        //}
    }
}