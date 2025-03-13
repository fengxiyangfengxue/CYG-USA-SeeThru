using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;
using Test.Definition;
using System.Text;
using Test._Definitions;

namespace Test._ScriptHelpers
{
    public class XmlSettingHelper
    {

        public static T LoadSetting<T>(string configPath) where T : class, new()
        {
            var type = typeof(T);
            string xmlFileName = type.Name + ".xml";
            var attr = ((XmlFileNameAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(XmlFileNameAttribute)));
            if (attr != null)
                xmlFileName = attr.FileName;

            return LoadSetting<T>(configPath, xmlFileName);
        }

        public static T LoadSetting<T>(string configPath, TEST_STATION station) where T : class, new()
        {
            var type = typeof(T);
            string xmlFileName = type.Name + ".xml";
            var attr = ((XmlFileNameAttribute)type.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(XmlFileNameAttribute)));
            if (attr != null)
                xmlFileName = attr.FileName;

            return LoadSetting<T>(configPath, station, xmlFileName);
        }

        public static T LoadSetting<T>(string configPath, TEST_STATION station, string xmlFileName) where T : class, new()
        {
            return LoadSetting<T>(Path.Combine(configPath, station.ToString()), xmlFileName);
        }

        public static T LoadSetting<T>(string configPath, string xmlFileName) where T : class, new()
        {
            T entity = null;
             
            string xmlPath = Path.Combine(configPath, xmlFileName);
            var fi = new FileInfo(xmlPath);
            if (!Path.GetExtension(fi.FullName).ToLower().Equals(".xml"))
                fi = new FileInfo(Path.Combine(fi.Directory.FullName, Path.GetFileNameWithoutExtension(fi.FullName)) + ".xml");

            if (!fi.Exists)
            {
                if (!fi.Directory.Exists)
                    fi.Directory.Create();

                var info = new FileInfo(xmlPath);
                if (!info.Directory.Exists)
                    Directory.CreateDirectory(info.Directory.FullName);

                entity = new T();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                XmlWriterSettings setting = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(xmlPath, setting))
                {
                    xmlSerializer.Serialize(xmlWriter, entity, namespaces);
                    xmlWriter.Close();
                }
            }
            else
            {
                string text = File.ReadAllText(fi.FullName);
                using (StringReader reader = new StringReader(text))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    entity = xmlSerializer.Deserialize(reader) as T;
                    reader.Close();
                }
            }

            return entity;
        }
         
        public static string Serialize<T>(T setting) where T : class, new()
        {
            string xml = string.Empty;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            XmlWriterSettings wSetting = new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true };

            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, wSetting))
                {
                    xmlSerializer.Serialize(xmlWriter, setting, namespaces);
                    xml = stringWriter.ToString();
                }
            }

            return xml;
        }

        public static T Deserialize<T>(string xmlString) where T : class, new()
        {
            T instance = null;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader stringReader = new StringReader(xmlString))
            {
                instance = (T)xmlSerializer.Deserialize(stringReader);
            }
            return instance;
        }
 
    }
}
