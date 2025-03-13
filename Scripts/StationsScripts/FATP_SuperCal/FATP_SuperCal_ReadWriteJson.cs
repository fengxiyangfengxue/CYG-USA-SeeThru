using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Test.StationsScripts.FATP_SuperCal
{
    class ReadWriteJson
    {
        public ReadWriteJson()
        {

        }

        /// <summary>
        /// 从文件加载配置到字典
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static Dictionary<string,object> LoadJsonConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, object>();   // 如果文件不存在，返回空字典
            }
            else
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }

        }

        public static void SaveJsonConfig(string filepath, Dictionary<string,object> config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filepath, json);
        }

    }
}

