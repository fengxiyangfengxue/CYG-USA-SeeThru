using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;
using Test.Definition;
using System.Text;
using LitJson;
using Test._ScriptExtensions;
using Test._Definitions;

namespace Test._ScriptHelpers
{
    public class JsonSettingHelper
    {

        //public static string ConfigPath = "C:\\Caesar\\Configs\\";
        //public static T LoadSetting<T>() where T : class, new()
        //{
        //    var type = typeof(T);
        //    string jsonFileName = type.Name + ".json";
        //    var attr = ((JsonFileNameAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(JsonFileNameAttribute)));
        //    if (attr != null)
        //        jsonFileName = attr.FileName;
  
        //    return LoadSetting<T>(jsonFileName);
        //}

        //public static T LoadSettingFile<T>(string jsonFileName) where T : class, new()
        //{
        //    return LoadSetting<T>(ConfigPath, jsonFileName);
        //}

        public static T LoadSetting<T>(string configPath) where T : class, new()
        {
            var type = typeof(T);
            string jsonFileName = type.Name + ".json";
            var attr = ((JsonFileNameAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(JsonFileNameAttribute)));
            if (attr != null)
                jsonFileName = attr.FileName;

            return LoadSetting<T>(configPath, jsonFileName);
        }

        //public static T LoadSetting<T>(TEST_STATION station) where T : class, new()
        //{
        //    var type = typeof(T);
        //    string jsonFileName = type.Name + ".json";
        //    var attr = ((JsonFileNameAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(JsonFileNameAttribute)));
        //    if (attr != null)
        //        jsonFileName = attr.FileName;

        //    return LoadSetting<T>(Path.Combine(ConfigPath, station.ToString()), jsonFileName);
        //}

        //public static T LoadSetting<T>(TEST_STATION station, string jsonFileName) where T : class, new()
        //{
        //    return LoadSetting<T>(Path.Combine(ConfigPath, station.ToString()), jsonFileName);
        //}

        public static T LoadSetting<T>(string configPath, TEST_STATION station) where T : class, new()
        {
            var type = typeof(T);
            string jsonFileName = type.Name + ".json";
            var attr = ((JsonFileNameAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(JsonFileNameAttribute)));
            if (attr != null)
                jsonFileName = attr.FileName;

            return LoadSetting<T>(configPath, station, jsonFileName);
        }

        public static T LoadSetting<T>(string configPath, TEST_STATION station, string jsonFileName) where T : class, new()
        {
            return LoadSetting<T>(Path.Combine(configPath, station.ToString()), jsonFileName);
        }

        public static T LoadSetting<T>(string configPath, string jsonFileName) where T : class, new()
        {
            T entity = null;
            string jsonPath = Path.Combine(configPath, jsonFileName);
            var fi = new FileInfo(jsonPath);
            if (!Path.GetExtension(fi.FullName).ToLower().Equals(".json"))
                fi = new FileInfo(Path.Combine(fi.Directory.FullName, Path.GetFileNameWithoutExtension(fi.FullName)) + ".json");

            if (!fi.Exists)
            {
                if (!fi.Directory.Exists)
                    fi.Directory.Create();

                var info = new FileInfo(jsonPath);
                if (!info.Directory.Exists)
                    Directory.CreateDirectory(info.Directory.FullName);

                entity = new T(); 
                string json = JsonMapper.ToJson(entity).BeautifyJson(); 
                File.WriteAllText(fi.FullName, json);
            }
            else
            {
                string json = File.ReadAllText(fi.FullName);
                entity = JsonMapper.ToObject<T>(json);
            }

            return entity;
        }
         
        public static string Serialize<T>(T setting) where T : class, new()
        { 
            string json = JsonMapper.ToJson(setting).BeautifyJson(); 
            return json;
        }

        public static T Deserialize<T>(string jsonString) where T : class, new()
        {
            T instance = JsonMapper.ToObject<T>(jsonString);  
            return instance;
        }
 
    }
}
