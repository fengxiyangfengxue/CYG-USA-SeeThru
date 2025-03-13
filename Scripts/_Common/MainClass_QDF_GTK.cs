using System;
using System.Collections.Generic;
using System.IO;
using UserHelpers.Helpers;
using Test._App;
using LitJson;
using System.Text.RegularExpressions;
using GTKWebServices;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using Test.Definition;
using Test._ScriptExtensions;
using Test._Definitions;
using System.Windows.Media;
using System.Threading;
using GitignoreParserNet;
using System.Diagnostics;
using Test._ScriptHelpers;
using System.Net;
using Test.StationsScripts.Shared;
using MetaHelpers.ScriptHelpers;

namespace Test
{
    public partial class MainClass
    {

        QDFContext _qdfContext = new QDFContext();
        public static readonly object QDFHashLocker = new object();

        [MainClassConstructor(TEST_STATION.ANY_STATION, level: 0)]
        public int MainClassContructor_QDF()
        {
            lock(QDFHashLocker)
            {
                string program_hash = Project.AppDictionary.TryGetValue<string>(ConstKeys.QDF_Program_Hash);
                if (string.IsNullOrEmpty(program_hash))
                {
                    var files = GetHashFiles();
                    if (files.Count == 0)
                        throw new Exception("GetHashFiles count = 0!");

                    program_hash = GetFilesHash(files).ToUpper();
                    Project.AppDictionary[ConstKeys.QDF_Program_Hash] = program_hash;
                }
            }
             
            return 0;
        }

        [ScriptInitialize(TEST_STATION.ANY_STATION, level: 10)]
        public int Script_Initialize_QDF(ITestItem item)
        {
            _qdfContext.Reset();
            return 0;
        }

        public void QDF_CollectData()
        {

            _qdfContext.program_hash = Project.AppDictionary.TryGetValue<string>(ConstKeys.QDF_Program_Hash);
            _qdfContext.header_hash = HashHelper.GetStringHash(_Limits.GetHashedString(), HashType.SHA256).ToUpper();

            _qdfContext.cm = _Config.CM;
            _qdfContext.factory_id = _buildSetting.FactoryId;
            _qdfContext.product_name = _Config.Product;
            _qdfContext.assembly_phase = _buildSetting.AssemblyPhase.ToString();
            _qdfContext.build_phase = _buildSetting.BuildPhase;
            _qdfContext.line_id = _mesSetting.LineID;
            _qdfContext.station_id = _mesSetting.StationNumbers[Project.ProjectIndex].Value;
            _qdfContext.station_sequence = int.TryParse(_buildSetting.StationSequencing, out int sequencing) ? sequencing : 0;
            _qdfContext.hostname = Dns.GetHostEntry("localhost").HostName;
            _qdfContext.operator_id = _Context.OperatorID;



            //_qdfContext.station_type = _Context.Variables.TryGetValue<string>(ConstKeys.QDF_Station_Type);

            string station = string.Empty;
            var dt = _Context.MESClient.mes.Search("BASE_SECTION", "SECTION_CUSTOMER", $"SECTION_CODE=\'{_Context.MESClient.mes.CurrentSectionCode}\'", "");
            if (dt != null && dt.Rows.Count > 0)
            {
                station = dt.Rows[0]["SECTION_CUSTOMER"]?.ToString() ?? "";
            }

            //station = "Selest_FATP_SWDL";
            if (string.IsNullOrEmpty(station))
            {
                throw new Exception("请联系MES工程师配置SECTION_CUSTOMER信息");
            }
            this._qdfContext.station_type = station.Substring(station.LastIndexOf("_") + 1);
            _Context.Variables[ConstKeys.MES_Station_Type] = station;
            _Context.Variables[ConstKeys.QDF_Station_Type] = this._qdfContext.station_type;



            _qdfContext.fixture = ""; //正常填写治具COM口，没有就留空
            _qdfContext.slot = Project.ProjectIndex + 1;
            //_qdfContext.test_status = _Context.Variables.TryGetValue<int>(ConstKeys.QDF_TestStatus);
            //_qdfContext.test_count = _Context.Variables.TryGetValue<int>(ConstKeys.QDF_TestCount);
            var info = GetPRIMEInfoBySN();
            _qdfContext.test_status = info.Item1;
            _qdfContext.test_count = info.Item2;

            _qdfContext.test_result = Project.HasFailed ? 2 : 1;
            _qdfContext.mes_status = Project.IsOnLine ? 1 : 2;
            _qdfContext.failures = Project.HasFailed ? _Context.FirstFailData.TestName : string.Empty;
            _qdfContext.errors = Project.HasFailed ? _Context.FirstFailData.Value : string.Empty;

            _qdfContext.start_time = _Context.TestStartTime.ToString("yyyy-MM-dd HH:mm:ss+08:00");
            _qdfContext.build_config = _Context.DUT_Config;
            _qdfContext.duration = (_Context.TestEndTime - _Context.TestStartTime).TotalSeconds.ToString("F3");
            _qdfContext.serial_number = string.IsNullOrEmpty(Project.SerialNumber) ? "NoSerialNumber" : Project.SerialNumber;
            string version = _Context.Variables.TryGetValue<string>(ConstKeys.OS_Version);
            if (!string.IsNullOrEmpty(version))
            {
                if (version.StartsWith("ver"))
                    _qdfContext.os_version = version;
                else
                    _qdfContext.diags_version = version;
            }
            _qdfContext.firmware_version = _Context.Variables.TryGetValue<string>(ConstKeys.Firmware_Version);

        }
        private (int, int) GetPRIMEInfoBySN()
        {
            //1 represents PRIME
            //2 represents FA
            //3 represents REWORK
            //4 represents GR&R
            //5 represents REL
            int test_status = 1;
            int test_count = 0;
            if (_Context.IsAudit)
            {
                test_status = 6;
                test_count = 0;
                return (test_status, test_count);
            }
            if (!Project.IsOnLine)
            {
                test_status = (int)_Config.TestMode;
                test_count = 0;
                return (test_status, test_count);
            }
            if (string.IsNullOrEmpty(Project.SerialNumber))
                return (test_status, 0);
            string json = _Context.MESClient.mes.GetSnLogInfo(Project.SerialNumber, _Context.MESClient.mes.CurrentSectionCode, _Context.MESClient.mes.CurrentStationCode);
            JsonData jd = JsonMapper.ToObject(json);
            if (jd == null)
                return (test_status, 0);
            bool flag = true;
            if (jd.ContainsKey("REWORK"))
            {
                string REWORK = jd["REWORK"].ToString().ToLower();
                if (REWORK.Equals("true"))
                {
                    flag = false;
                    test_status = 3;
                }
            }
            if (jd.ContainsKey("PRIME") && flag)
            {
                string PRIME = jd["PRIME"].ToString().ToLower();
                if (PRIME.Equals("true"))
                    test_status = 1;
            }
            if (jd.ContainsKey("TEST_COUNT"))
            {
                string count = jd["TEST_COUNT"]?.ToString() ?? "0";
                if (int.TryParse(count, out int i))
                    test_count = i;
            }
            return (test_status, test_count);
        }
        //要放在AfterSavingLog, 不要放在AfterTesting,因为当出现未处理异常时AfterTesting仍然会执行，但异常时我们不希望产生QDF数据 
        //[AfterSavingLog(station: TEST_STATION.ANY_STATION, level: 0)]   todo:暂不上传qdf
        public int AfterSavingLog_QDF() 
        {
            bool result = false;
            try
            {
                if (string.IsNullOrEmpty(Project.SerialNumber))
                {
                    result = true;
                    return 0;
                }

                //CheckRoute失败的不提交QDF
                if (Project.IsOnLine && !_Context.IsCheckRoutePass)
                {
                    result = true;
                    return 0;
                }

                if (!Project.IsFailToSFIS && Project.HasFailed)
                {
                    result = true;
                    return 0;
                }

                QDF_CollectData();

                string qdfPath = Project.ParsePath(_commonSetting.QdfLogFolderPath);
                string qdfFileName = Project.ParsePath(_commonSetting.QdfFileName);
                var files = Path.Combine(qdfPath, qdfFileName);

                _Context.Variables[ConstKeys.QDF_Zip_Folder] = qdfPath;

                var collector = (DataCollector)App_GetDataCollector();
                var resultData = collector.GetTestResultData();
                _qdfContext.SaveCSV(files, resultData);

                if (_Context.QDF_ZipFiles.Count > 0)
                    files += ";" + _Context.QDF_ZipFiles.CombineToString(";");

                string qdfZipFolder = _Context.Variables.TryGetValue<string>(ConstKeys.QDF_Zip_Folder);
                using (var _service = new Plugin.Sqlite.QdfService())
                {
                    result = _service.Create(Project.SerialNumber,
                        qdfZipFolder,
                        files,
                        _Config.Product,
                        _Context.Variables.TryGetValue<string>(ConstKeys.MES_Station_Type),
                        Project.IsOnLine ? 1 : 2,
                        _Context.TestStartTime);
                }

                result = true;
            }
            catch (Exception ex)
            {
                SaveExtraLogs(ex.ToString());
                UIMessageBox.Show(Project, ex.ToString());
            }
            finally
            {
                if (!result)
                {
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterTesting AfterTesting_QDF failed!", "AfterTesting AfterTesting_QDF fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }


            return 0;
        }

        List<string> GetHashFiles()
        {
            string config = File.ReadAllText(Path.Combine(LocalConfigPath, "QDF.ignore"));
            GitignoreParser ignore = new GitignoreParser(config);
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var files = di.GetFiles("*.*", SearchOption.AllDirectories).Select(f => f.FullName.Substring(AppDomain.CurrentDomain.BaseDirectory.Length)).ToList();
            files = ignore.Accepted(files).ToList();
            return files;
        }

        string GetFilesHash(List<string> files)
        {
            List<string> hashes = new List<string>();

            //并行计算Hash
            Parallel.For(0, files.Count, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount + 2 }, i =>
            {
                var hash = HashHelper.GetFileHash(files[i], HashType.SHA256);
                hashes.Add(hash);
            });

            //Hash结果排序 
            hashes.Sort();

            //所有文件Hash合并成一个字串
            string hashString = hashes.CombineToString();

            //再计算合并的字串的Hash
            return HashHelper.GetStringHash(hashString, HashType.SHA256);
        }


    }
}
