using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Test
{

    [XmlRoot("FATP_CameraSetting")]
    public class FATP_CameraSetting
    {
        [XmlArray("PYCConfigs")]
        public FixturePYCConfig[] PYCConfigs { get; set; } = new FixturePYCConfig[] { new FixturePYCConfig() };

        public override string ToString()
        {
            return $"PYCConfigs = {string.Join(",", PYCConfigs.Select(c => c.ToString()).ToArray())}";
        }
    }

    public class FixturePYCConfig
    {
        [XmlElement("PYCPath")]
        public string PYCPath { get; set; } = "InnorevCMTapi.pyc";

        [XmlElement("ConfigPaths")]
        public string ConfigPaths { get; set; } = @"C:\Caesar\Configs";


        public override string ToString()
        {
            return $"PYCPath = {PYCPath};" +
                        $"ConfigPaths = {ConfigPaths};";
        }
    }


    [XmlRoot("FATP_CameraBlemishSetting")]
    public class FATP_CameraBlemishSetting
    {
        [XmlElement("CanyonConfig")]
        public BlemishPythonSetting CanyonConfig { get; set; } = new BlemishPythonSetting();
        [XmlElement("JacksonConfig")]
        public BlemishPythonSetting JacksonConfig { get; set; } = new BlemishPythonSetting();

        public override string ToString()
        {
            return $"CanyonConfig = {CanyonConfig}; JacksonConfig = {JacksonConfig}";
        }
    }

    public class BlemishPythonSetting
    {
        [XmlElement("PythonExePath")]
        public string ExeFile { get; set; } = "python.exe";

        [XmlElement("PyPath")]
        public string PyPath { get; set; } = "xxx.py";


        public override string ToString()
        {
            return $"ExeFile = {ExeFile};" +
                        $"PyPath = {PyPath};";
        }
    }

    public class CameraTestCache
    {
        public CameraTestCache()
        {
            IsImageReady = false;
            IsTestEnd = false;
            CSVDataTable = new DataTable();
        }

        public bool IsImageReady { get; set; }
        public bool IsTestEnd { get; set; }
        public string ImagePath { get; set; }
        public string OutputCSV_Algo { get; set; }
        public string OutputCSV_PyBlemish { get; set; }
        public DataTable CSVDataTable = new DataTable();

        public void Clear()
        {
            IsImageReady = false;
            IsTestEnd = false;
            ImagePath = string.Empty;
            OutputCSV_Algo = string.Empty;
            OutputCSV_PyBlemish = string.Empty;
            CSVDataTable.Reset();
        }
    }
}
