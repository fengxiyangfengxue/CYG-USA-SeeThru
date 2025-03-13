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
    [XmlRoot("StageCalSetting")]
    public class StageCalSetting
    {
        public StageCalSetting()
        {

        }

        [XmlElement("cal_image_path")]
        // stagecal image path
        public string cal_image_path { get; set; }

        [XmlElement("Convert")]
        public string Convert { get; set; }

        [XmlElement("zip_stage_cal_file_name")]
        public string zip_stage_cal_file_name { get; set; }

    }





}
