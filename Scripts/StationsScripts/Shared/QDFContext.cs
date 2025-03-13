
using GTKWebServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test._Definitions;
using Test._ScriptExtensions;
using Test._ScriptHelpers;
using Test.Definition;
using UserHelpers.Helpers;

namespace Test.StationsScripts.Shared
{
    public class QDF_LineData
    { 
        public QDF_LineData(string parameter, string value, string lcl, string ucl, string unit, string testTime)
        {
            Parameter = parameter;
            Value = value;
            LowLimit = lcl;
            HighLimit = ucl;
            Unit = unit;
            TestTime = testTime; 
        }

        public string Parameter { get; set; }
        public string Value { get; set; }
        public string LowLimit { get; set; }
        public string HighLimit { get; set; }
        public string Unit { get; set; }
        public string TestTime { get; set; }
    }


 
    public class QDFContext
    {  
        public string program_hash { get; set; }
         
        public string header_hash { get; set; }
         
        public string cm { get; set; }
        public string factory_id { get; set; }
        public string product_name { get; set; }
        public string build_config { get; set; }
        public string assembly_phase { get; set; }
        public string build_phase { get; set; }
        public string line_id { get; set; }
        public string station_type { get; set; }
        public string station_id { get; set; }
        public int station_sequence { get; set; }
        public int slot { get; set; }
        /// <summary>
        /// List<string>
        /// </summary>
        public string fixture { get; set; }
        /// <summary>
        /// 1,2,3 mes获取
        /// </summary>
        public int test_count { get; set; }
        /// <summary>
        /// 1-PASS, 2-FAIL, 3-ERROR.
        /// </summary>
        public int test_result { get; set; }
        /// <summary>
        /// 1 represents PRIME
        /// 2 represents FA
        /// 3 represents REWORK
        /// 4 represents GR&R
        /// 5 represents REL
        /// </summary>
        public int test_status { get; set; }
        public int mes_status { get; set; }
        /**
         Type: List(string)
Description: List of failing tests separated by semicolon
Example ‘LSPK_FR_100;LSPK_FR_200; etc…’ (Note: It is only required to have the first
failure reported, subsequent failures are optional report)
         */
        /// <summary>
        /// 
        /// </summary>
        public string failures { get; set; }
        /// <summary>
        /// Type: List(string)
        /// </summary>
        public string errors { get; set; }
        /// <summary>
        ///Description: Time in UTC time zone.Must match format YYYY:MM:DD HH:MM:SS
        /// </summary>
        public string start_time { get; set; }
        /// <summary>
        /// Description: Time duration in seconds (SS.SSS) indicating the time taken for the test run
        /// </summary>
        public string duration { get; set; }
        public string hostname { get; set; }
        public string operator_id { get; set; }
        public string serial_number { get; set; }
        /// <summary>
        /// Description: List of names of *.zip containing test run files. Note that the name of zip files should follow the naming convention mentioned here
        /// </summary>

        public string zip_files { get; set; }


        /// <summary>
        /// dut_os_version gtk os 产品的系统版本号
        /// Description: Version details of the software running on the DUT; refers to factory image
        /// </summary>
        public string diags_version { get; set; }
        /// <summary>
        /// Description: Device firmware version number separated using semicolon.;
        /// </summary>
        public string firmware_version { get; set; }
        /// <summary>
        /// meta os version
        /// Description: Device operating system version number; refers to shipping image
        /// </summary>
        public string os_version { get; set; }
         

        public QDFContext()
        {
            Reset();
        }
         
        public void Reset()
        {
            program_hash = string.Empty;
            header_hash = string.Empty;
            cm = string.Empty;
            factory_id = string.Empty;
            product_name = string.Empty;
            build_config = string.Empty;
            assembly_phase = string.Empty;
            build_phase = string.Empty;


            line_id = string.Empty;
            station_type = string.Empty;
            station_id = string.Empty;
            station_sequence = 0;
            slot = 0;
            fixture = string.Empty;
            test_count = 0;
            test_result = 0;
            test_status = (int)Test_Mode.PRIME;
            mes_status = 0;
            failures = string.Empty;
            errors = string.Empty;
            start_time = string.Empty;
            duration = string.Empty;
            hostname = string.Empty;
            operator_id = string.Empty;
            serial_number = string.Empty;
            zip_files = string.Empty;
            diags_version = string.Empty;
            firmware_version = string.Empty;
            os_version = string.Empty; 
        }

        List<QDF_LineData> ToLines()
        {
            List<QDF_LineData> lines = new List<QDF_LineData>();

            lines.Add(new QDF_LineData("program_hash", program_hash, "", "", "", program_hash));
            lines.Add(new QDF_LineData("header_hash", header_hash, "", "", "", header_hash));
            lines.Add(new QDF_LineData("cm", cm, "", "", "", cm));
            lines.Add(new QDF_LineData("factory_id", factory_id, "", "", "", factory_id));
            lines.Add(new QDF_LineData("product_name", product_name, "", "", "", product_name));
            lines.Add(new QDF_LineData("build_config", build_config, "", "", "", build_config));
            lines.Add(new QDF_LineData("assembly_phase", assembly_phase, "", "", "", assembly_phase));
            lines.Add(new QDF_LineData("build_phase", build_phase, "", "", "", build_phase));
            lines.Add(new QDF_LineData("line_id", line_id, "", "", "", line_id));
            lines.Add(new QDF_LineData("station_type", station_type, "", "", "", station_type));
            lines.Add(new QDF_LineData("station_id", station_id, "", "", "", station_id));
            lines.Add(new QDF_LineData("station_sequence", station_sequence.ToString(), "", "", "", station_sequence.ToString()));
            lines.Add(new QDF_LineData("slot", slot.ToString(), "", "", "", slot.ToString()));
            lines.Add(new QDF_LineData("fixture", fixture, "", "", "", fixture));
            lines.Add(new QDF_LineData("test_count", test_count.ToString(), "", "", "", test_count.ToString()));
            lines.Add(new QDF_LineData("test_result", test_result.ToString(), "", "", "", test_result.ToString()));
            lines.Add(new QDF_LineData("test_status", test_status.ToString(), "", "", "", test_status.ToString()));
            lines.Add(new QDF_LineData("mes_status", mes_status.ToString(), "", "", "", mes_status.ToString()));
            lines.Add(new QDF_LineData("failures", failures, "", "", "", failures));
            lines.Add(new QDF_LineData("errors", errors, "", "", "", errors));
            lines.Add(new QDF_LineData("start_time", start_time, "", "", "", start_time));
            lines.Add(new QDF_LineData("duration", duration, "", "", "", duration));
            lines.Add(new QDF_LineData("hostname", hostname, "", "", "", hostname));
            lines.Add(new QDF_LineData("operator_id", operator_id, "", "", "", operator_id));
            lines.Add(new QDF_LineData("serial_number", serial_number, "", "", "", serial_number));
            lines.Add(new QDF_LineData("zip_files", zip_files, "", "", "", zip_files));
            lines.Add(new QDF_LineData("diags_version", diags_version, "", "", "", diags_version));
            lines.Add(new QDF_LineData("firmware_version", firmware_version, "", "", "", firmware_version));
            lines.Add(new QDF_LineData("os_version", os_version, "", "", "", os_version)); 
             

            return lines;
        }


        public void SaveCSV(string fileName, List<IResultData> resultData)
        {
            var lines = ToLines();
            resultData.ForEach(r =>
            {
                string name = r.TestName;
                if (name.Length > 255)
                    name = name.Substring(0, 255);
                if (name.Length < 5)
                    name = name.PadRight(5, '0');
                 
                lines.Add(new QDF_LineData(name, r.Value, r.LowerLimit,
                                            r.UpperLimit, r.Unit, 
                                            (r.TestEndTime.Value - r.TestStartTime.Value).TotalSeconds.ToString("F3")));
                 
            });
             

            StringBuilder sb = new StringBuilder();

            sb.Append("Parameter," + lines.Select(d => d.Parameter).ToList().CombineToString(",") + Environment.NewLine);
            sb.Append("LowLimit," + lines.Select(d => d.LowLimit).ToList().CombineToString(",") + Environment.NewLine);
            sb.Append("HighLimit," + lines.Select(d => d.HighLimit).ToList().CombineToString(",") + Environment.NewLine);
            sb.Append("Unit," + lines.Select(d => d.Unit).ToList().CombineToString(",") + Environment.NewLine);
            sb.Append("Value," + lines.Select(d => d.Value).ToList().CombineToString(",") + Environment.NewLine);
            sb.Append("TestTime," + lines.Select(d => d.TestTime).ToList().CombineToString(",") + Environment.NewLine);

            var fi = new FileInfo(fileName);
            if (!fi.Directory.Exists)
                fi.Directory.Create();

            File.WriteAllText(fi.FullName, sb.ToString()); 
        }
         
        public void Dispose()
        { 
            Reset();
        }

    }
}
