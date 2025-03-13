using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Test._Definitions;

namespace Test.ScriptSettings
{
    public class CommonSetting
    {
        public CommonSetting()
        { 
            AuditSN = "111,222,333";
            OPIDLength = 7;
            SNLength = 14;
            AuditSNCount = 2;
            LoopTimes = 0;
            SummaryPath = "D:\\TestLog\\\"Product\"_\"ScriptMode\"\\\"SFISMode\"\\Summary\\[\"Product\"][\"StationName\"][\"LineName\"][\"StationDesc\"][\"yyyy-MM-dd\"][\"SlotID\"].csv";
            QdfLogFolderPath = $"D:\\TestLog\\\"Product_\"ScriptMode\"\"\\\"SFISMode\"\\\"StationDesc\"\\\"yyyy-MM-dd\"\\\"Result\"\\\"CM\"_\"Product\"_\"LineID\"_\"StationNumber\"_\"SN\"_\"TSRID\"";
            QdfFileName = "\"CM\"_\"Product\"_\"LineID\"_\"StationNumber\"_\"SN\"_\"Result\"_\"TestStartTime\".qdf.csv";

            QdfServicePath = @"C:\Caesar\QDFService";
            QdfMonitorPath = @"C:\Caesar\QDFMonitor";
        }


        public int OPIDLength { get; set; }
        public int SNLength { get; set; }
        public int AuditSNCount { get; set; } 
        public int LoopTimes { get; set; } 
        public string AuditSN { get; set; }
        public string SummaryPath { get; set; }
        public string QdfLogFolderPath { get; set; }
        public string QdfFileName { get; set; }

        public string QdfServicePath { get; set; }
        public string QdfMonitorPath { get; set; }

    }
 
}
