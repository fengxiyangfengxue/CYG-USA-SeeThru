using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Test._Definitions;

namespace Test.ScriptSettings
{
    public class ADBLocationSetting
    {
        public ADBLocationSetting()
        {  
            var adb_Locations = new List<USBLocation>();
            var bootloader_Locations = new List<USBLocation>();
            for (int i = 0; i < ConstKeys.XmlConfigredSlots; i++)
            {
                adb_Locations.Add(new USBLocation() { Location = "0000.0014.0000.003.003.000.000.000.000"});
                bootloader_Locations.Add(new USBLocation() { Location = "0000.0014.0000.003.003.000.000.000.000" });
            } 
            ADB_Locations = adb_Locations.ToArray();
            Bootloader_Locations = bootloader_Locations.ToArray();
        }

         
        public USBLocation[] ADB_Locations { get; set; }
        public USBLocation[] Bootloader_Locations { get; set; } 

    }

    [XmlType("Location")]
    public class USBLocation
    {
        [XmlText]
        public string Location { get; set; }
    }
}
