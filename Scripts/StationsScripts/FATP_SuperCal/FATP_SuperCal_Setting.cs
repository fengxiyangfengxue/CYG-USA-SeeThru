using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Test._Definitions;

namespace Test
{


    [XmlRoot("SuperCal_Setting")]
    public class SuperCal_Setting
    {

        // 探针最大使用次数
        [XmlElement("ProbeCntMax")]
        public int ProbeCntMax { get; set; }


        [XmlElement("CntCount")]
        public int CntCount { get; set; }

        [XmlElement("Record")]
        public string Record { get; set; }


        [XmlElement("Convert")]
        public string Convert { get; set; }

        [XmlElement("zip_file_name")]
        public string zip_file_name { get; set; }

        [XmlElement("Product")]
        public string Product { get; set; }

        [XmlElement("section")]
        public string section { get; set; }

        [XmlElement("main_app_ver")]
        // FW内应包含的版本号
        public string main_app_ver { get; set; }


        [XmlElement("turbocal_raw_data")]
        // raw 文件的最小大小（用于判断）
        public int rawDataFile_minSize { get; set; } = 1024;

        [XmlElement("cam_num")]
        // 产品数量
        public int cam_num { get; set; } = 4;

        [XmlElement("hcPlcIP")]
        // 汇川PLCIP
        public string hcPlcIP { get; set; } = "192.168.1.88";

        [XmlElement("img_no_min")]

        public int img_no_min { get; set; }

        [XmlElement("img_no_max")]

        public int img_no_max { get; set; }


        // urta 的Sn数据格式
        // 使用自定义类型来序列化字典
        [XmlArray("uart_sn_nest_dict")]
        [XmlArrayItem("uart_sn")]
        public List<UartSnItem> uart_sn_nest_dict { get; set; }

        // 线别和站别
        [XmlElement("Line")]
        public string Line { get; set; }

        [XmlElement("Station")]
        public string Station { get; set; }

        public SuperCal_Setting()
        {
            uart_sn_nest_dict = new List<UartSnItem>();

        }
        public class UartSnItem
        {
            [XmlAttribute("key")]
            public string Key { get; set; }

            [XmlText]
            public string Value { get; set; }

        }


    }
     

}
