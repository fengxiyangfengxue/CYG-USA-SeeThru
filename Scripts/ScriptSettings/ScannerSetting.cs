using System.Collections.Generic;
using System.IO.Ports;
using System.Xml.Serialization;
using Test._Definitions;

namespace Test.ScriptSettings
{

    public class ScannerSetting
    {
        public ScannerSetting()
        {
            ComConfigs = new ScannerComConfig[ConstKeys.XmlConfigredSlots];
            for (int i = 0; i < ConstKeys.XmlConfigredSlots; i++)
            {
                ComConfigs[i] = new ScannerComConfig();
            } 
        }
        public ScannerComConfig[] ComConfigs { get; set; } = new ScannerComConfig[] { new ScannerComConfig() };
    }

    public class ScannerComConfig
    {
        public ScannerComConfig()
        {
            Port = "COM1";
            BaudRate = 115200;
        }

        //[XmlAttribute("slot")]
        //public int SlotId { get; set; }
        public string Port { get; set; }
        public int BaudRate { get; set; }  
    }




}
