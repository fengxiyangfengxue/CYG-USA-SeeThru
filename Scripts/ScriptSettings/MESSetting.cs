using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Test._Definitions;
using Test._ScriptExtensions;

namespace Test.ScriptSettings
{
    [XmlRoot("MESSetting")]
    public class MESSetting
    {
        public MESSetting()
        {
            LineName = "FT03_BUTTON_FFTCAL_L_01_02";
            LineID = "01";
            UserName = "Test@NM01";
            PWD = "Test@NM01";
            App_WebService = "http://10.10.36.93:8080/WebService/App_DllSvc.asmx";
            Wip_WebService = "http://10.10.36.93:8080/WebService/Wip_TestSvr.asmx";
            IWebService_WebService = "http://10.10.47.107:8175/soap/IWebService";
            Description = "mes online setting";

            StationName = "Barista_FATP_Button-FFTcal-L";
            TesterNames = new TextValue[ConstKeys.XmlConfigredSlots];
            StationDescs = new TextValue[ConstKeys.XmlConfigredSlots];
            StationNumbers = new TextValue[ConstKeys.XmlConfigredSlots];

            for (int i = 0; i < ConstKeys.XmlConfigredSlots; i++)
            {
                TesterNames[i] = new TextValue() { Value = "Button_" + (i + 1).ToString("D2") };
                StationDescs[i] = new TextValue() { Value = "Button_" + (i + 1).ToString("D2") };
                StationNumbers[i] = new TextValue() { Value = (i + 1).ToString("D2") };
            }
        }

        public TextValue[] TesterNames { get; set; } = new TextValue[] { new TextValue() { Value = "Button_01" } };



        public string LineName { get; set; }
        public string LineID { get; set; }

        public string StationName { get; set; }
        public TextValue[] StationDescs { get; set; } = new TextValue[] { new TextValue() { Value = "Button_01" } };
        public TextValue[] StationNumbers { get; set; } = new TextValue[] { new TextValue() { Value = "01" } };

        public string UserName { get; set; }
        public string PWD { get; set; }
        public string App_WebService { get; set; }
        public string Wip_WebService { get; set; }
        public string IWebService_WebService { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

    }

    [XmlType("Text")]
    public class TextValue
    {

        //[XmlAttribute("slot")]
        //public int SlotId { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
