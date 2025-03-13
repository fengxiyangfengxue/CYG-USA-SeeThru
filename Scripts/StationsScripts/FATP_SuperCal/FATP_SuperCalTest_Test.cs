using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.StationsScripts.FATP_SuperCal;
using UserHelpers.Helpers;
using Test._App;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetaHelpers.ScriptHelpers;
using Test.ModbusTCP;
using Test._ScriptExtensions;
using System.Diagnostics;
using System.Threading;
using NModbus;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Test
{
    public partial class MainClass
    {
        private static readonly string COMNUMBERTEMP = "COM19";
        public static bool soltTwoTestMake = false;
        private static readonly object filelock = new object();
        public string test_id;
        public bool SuperCalALgoOn = false;
        public static string SuperCalConfigPath = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\";

        //public readonly string pexFilePath = System.Windows.Forms.Application.StartupPath + "\\pex_SuperCal\\factory_commands.pex";
        public readonly string pexFilePath = "D:\\Python\\factory_commands.pex";
        
        static string superCalCmdPath = SuperCalConfigPath + "\\SuperCal_cmd.json";
        Dictionary<string, object> CMD_Command = ReadWriteJson.LoadJsonConfig(superCalCmdPath);
        // 创建一个字典存数据
        public Dictionary<string, object> savaDataDict = new Dictionary<string, object>();

        // 给线程使用
        private volatile bool _stopRequested;
        private Process cmdProcess;
        public bool do_rec_data = false;  //TODO:为什么需要设置这个标志？
        public CancellationTokenSource cts = new CancellationTokenSource();
        public List<Dictionary<string, object>> recData = new List<Dictionary<string, object>>();
        public string stringRecData = string.Empty;          // TODO：什么时候清空？


        public int recErrorDataCount = 0;

        public bool led_config_done = false;
        //private List<JObject> allShData = new List<JObject>();
        //private bool led_config_done = false;
        //private Thread _workThread;

        // socket通讯
        //public TcInt SocketCameraClient = new TcInt(20237);

        // 发给cam服务器的存图地址
        public string camImagePath = $"C:\\SuperCalRecord\\{DateTime.Now.ToString("yyyy_MM_dd")}";
        public string totalPath = string.Empty;

        public Dictionary<string, object> dutInfo = new Dictionary<string, object>
        {
            {"start_date_time",string.Empty },
            {"start_date",string.Empty },
            {"start_time",string.Empty },
            {"start_time_cnt",0 },
            {"test_id",string.Empty },
            {"cal_log_path",string.Empty },
            {"date_path",string.Empty },
            {"stage_cal_path",string.Empty },
            {"sum_csv_header",string.Empty },
            {"nest_id",0 },
            {"result","Fail" },
            {"fail_item",string.Empty },
            {"cost_time",0.0 },
            {"algo",false },
            {"mes_status",string.Empty},
            {"sn",string.Empty}
        };

        TestIdUpdater testIdUpdater;




        // 检查探针寿命
        public int ProbeCountOverRangeShow(ITestItem item)
        {
            bool result = true;
            int cntMax = 0;

            try
            {
                lock (filelock)
                {
                    SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);
                    cntMax = setting.ProbeCntMax;
                    item.AddLog($"读取出的cntmax的值为：{cntMax}");
                    //item.AddLog($"读取出的cntmax的值为：{setting.CntCount}");
                    //setting.CntCount++;
                    //using (FileStream fs = new FileStream(CaesarConfigPath, FileMode.Create))  // 确保创建或覆盖文件
                    //{
                    //    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SuperCal_Setting));
                    //    xmlSerializer.Serialize(fs, setting);  // 将更新后的setting对象序列化到文件
                    //}
                    string cntCountPath = SuperCalConfigPath + "\\ProbeCount.json";
                    // 从文件加载配置
                    Dictionary<string, object> cntConfig = ReadWriteJson.LoadJsonConfig(cntCountPath);
                    foreach (var key in cntConfig.Keys)
                    {
                        int cnt_temp = Convert.ToInt32(cntConfig[key]);
                        if (cnt_temp > cntMax)
                        {
                            item.AddLog($"{key}的探针使用次数超过设定的{cntMax},请进行更换！");
                            result = false;
                            var config = new UIMessageBoxConfig()
                            {
                                Title = "探针使用次数超限",
                                Text = $"{key}的探针使用次数 {cnt_temp}, 超过设定的{cntMax},请进行更换！",
                                TextFontSize = 20,
                                TextColor = Colors.Green,
                                Button = UIMessageBoxButton.OK,
                                WaitForExit = false, //不阻塞
                            };
                            UIMessageBox.Show(Project, config);
                        }
                    }

                    if (!result)
                    {
                        goto ReturnAndExit;
                    }
                }

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog("ProbeCountOverRangeShow error: " + ex.ToString());
            }


            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        // 检查校准文件
        public int CheckStageCalJson(ITestItem item, int dayOutTime)
        {
            bool result = true;
            string fileDateTime = "19700101000000";
            DateTime dateTimeLocal = DateTime.MinValue;
            try
            {
                lock (filelock)
                {
                    string StageCalPath = SuperCalConfigPath + "\\stagecal.json";
                    // 从文件加载配置
                    Dictionary<string, object> StageCalFile = ReadWriteJson.LoadJsonConfig(StageCalPath);

                    if (StageCalFile.TryGetValue("FileFormat", out var fileTimeDict) &&
                        fileTimeDict is JObject fileFormatDict &&
                        fileFormatDict.TryGetValue("Timestamp", out var timestampobj))

                    {
                        // 输出 timestampobj 的原始类型和内容
                        item.AddLog($"timestampobj 类型: {timestampobj}, 内容: {timestampobj}");
                        DateTime timestamp = (DateTime)timestampobj;
                        fileDateTime = timestamp.ToString("yyyyMMddHHmmss");
                    }
                }

                if (DateTime.TryParseExact(fileDateTime, "yyyyMMddHHmmss", null,
                        System.Globalization.DateTimeStyles.None, out var parsedDateTime))

                {
                    dateTimeLocal = parsedDateTime.AddHours(8);
                    item.AddLog($"读取到dateTimeLocal的时间是{dateTimeLocal}");
                }

                if ((DateTime.Now - dateTimeLocal).TotalDays > dayOutTime)
                {
                    item.AddLog($"从文件中读取到的时间是{dateTimeLocal},已超过设定的时间，请更新文件stage_cal.json");
                    result = false;
                    goto ReturnAndExit;
                }
                else
                {
                    item.AddLog($"从文件中读取到的时间是{dateTimeLocal},未超过设定的时间");
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"CheckStageCalJson_error:{ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title,  result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int SuperCalReadSn(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            string SN = string.Empty;
            string command = string.Empty;
            string read = string.Empty;

            try
            {
                string superCalCmdPath = SuperCalConfigPath + "\\SuperCal_cmd.json";
                string lastTestIdPath = SuperCalConfigPath + "\\last_test_id.json";

                Dictionary<string, object> cmd_dir = ReadWriteJson.LoadJsonConfig(superCalCmdPath);
                CMD_Command = cmd_dir;

                    if (cmd_dir.TryGetValue("read_sn", out var read_sn_cmd))
                {
                    item.AddLog($"read_sn_cmd_type {read_sn_cmd.GetType()},{read_sn_cmd}");
                    command = Convert.ToString(read_sn_cmd);
                    //$"COM{Project.ProjectIndex + 1}"
                    command = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    item.AddLog($"read_sn_cmd_type {command.GetType()},{command}");
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, timeout, ref read);
                    item.AddLog($"READ:{read}");
                    if (!isOK)
                    {
                        item.AddLog($"执行读取SN的指令，但返回的bool值为false");
                        goto ReturnAndExit;
                    }

                    SN = read.StringParse("assembly_sn");
                    if (!(SN.IsAbcNumber() || SN.Length == 14))
                    {
                        item.AddLog($"执行读取SN的指令，读取出来的SN不符合规格:{SN}");
                        goto ReturnAndExit;
                    }

                    item.AddLog($"[DUT] [Key-Info] [SN from DUT: [{SN}]]");

                    Project.SerialNumber = SN;
                    Project.PathDictionary["SN"] = Project.SerialNumber;
                    Project.SideBar.TopBar.Add("SN", Project.SerialNumber);

                    testIdUpdater = new TestIdUpdater(lastTestIdPath);
                    //test_id = testIdUpdater.UpdateTestId(Project.IsOnLine);
                    test_id = testIdUpdater.UpdateTestId(false);
                    item.AddLog($"创建test_id文件：{lastTestIdPath}");
                    test_id = test_id.Substring(Math.Max(test_id.Length - 4, 0));

                    SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);
                    var dataproc_test_id =
                        $"{setting.Line}{setting.Station}{DateTime.Now.ToString("yyyyMMddHHmmss")}{Project.ProjectIndex + 1}{test_id}";

                    dutInfo["dataproc_test_id"] = dataproc_test_id;
                    if (savaDataDict.ContainsKey("dataproc_test_id")) savaDataDict["dataproc_test_id"] = dataproc_test_id;
                    else savaDataDict.Add("dataproc_test_id", dataproc_test_id);

                    if (savaDataDict.ContainsKey("deviceSerial")) savaDataDict["deviceSerial"] = SN;
                    else savaDataDict.Add("deviceSerial", SN);

                    string cntCountPath = SuperCalConfigPath + "\\ProbeCount.json";
                    // 从文件加载配置
                    Dictionary<string, object> cntConfig = ReadWriteJson.LoadJsonConfig(cntCountPath);

                    string cnatDictKey = "probe0" + (Project.ProjectIndex + 1).ToString();
                    if (cntConfig.ContainsKey(cnatDictKey))
                    {
                        //item.AddLog($"cntConfig>{cntConfig.Values}");
                        //foreach (var v in cntConfig.Values)
                        //{
                        //    item.AddLog($"{v}");

                        //}
                        var cntTemp = Convert.ToInt32(cntConfig[cnatDictKey])+1;
                        cntConfig[cnatDictKey] = cntTemp;
                    }
                    item.AddLog($"cntConfig>{cntConfig.Values}");
                    lock (filelock)
                    {
                        ReadWriteJson.SaveJsonConfig(cntCountPath, cntConfig);

                    }
                    dutInfo["sn"] = SN;
                    result = CreateDataPath(item);

                    //result = true;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"读取SN的时候出错 error: {ex}");
                result =  false;
            }

            ReturnAndExit:
            ResultData resultData = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, SN);
            AddResult(item, resultData);
            return result ? 0 : 1;
        }

        public bool CreateDataPath(ITestItem item)
        {
            bool result;
            try
            {
                lock (filelock)
                {
                    InitStartTime();
                    GenerateDataPath(item);
                    GenerateSummaryCsv(item);

                    //string tempTestId = testIdUpdater.UpdateTestId(false);
                    string tempTestId = test_id;
                    dutInfo["test_id"] = tempTestId;
                    dutInfo["nest_id"] = Project.ProjectIndex + 1;
                    GenerateTotalPath(item);

                    result = true;
                    
                }
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
                
            }

        }

        public void InitStartTime()
        {
            DateTime timeNow = DateTime.Now;
            dutInfo["start_date_time"] = timeNow.ToString("yyyy_MM_dd HH:mm:ss");
            dutInfo["start_date"] = timeNow.ToString("yyyy_MM_dd");
            dutInfo["start_time"] = timeNow.ToString("HH:mm:ss");
            dutInfo["start_time_cnt"] = DateTime.Now;
            if (Project.IsOnLine) dutInfo["mes_status"] = "on line";
            else dutInfo["mes_status"] = "off line";

        }

        public void GenerateDataPath(ITestItem item)
        {
            SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);

            string recordPath = setting.Record;
            string dataPath = Path.Combine(recordPath, dutInfo["start_date"].ToString(),
                dutInfo["mes_status"].ToString());
            FindFile(item,dataPath);
            dutInfo["data_path"] = dataPath;
        }

        public void GenerateSummaryCsv(ITestItem item)
        {
            try
            {
                var csvHeader = new List<string> { "nest_id", "sn", "start_date_time", "test_id", "result", "fail_item", "cost_time"};
                var sum_file_path = Path.Combine(dutInfo["data_path"].ToString(), "summary.csv");
                dutInfo["sum_file_path"] = sum_file_path;
                dutInfo["sum_csv_header"] = string.Join(",", csvHeader);
                
            }
            catch (Exception e)
            {
                item.AddLog(e.ToString());
                throw;
            }

        }

        public void GenerateTotalPath(ITestItem item)
        {
            SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);

            string totalPath_temp = Path.Combine(setting.Record, dutInfo["start_date"].ToString(),
                dutInfo["mes_status"].ToString(),
                dutInfo["sn"].ToString() + "_" + dutInfo["test_id"].ToString() + "_" + dutInfo["nest_id"].ToString());

            FindFile(item, totalPath_temp);


            string dst_total_path = Path.Combine(setting.Convert,
                totalPath_temp.Substring(Path.GetPathRoot(totalPath_temp).Length));
            FindFile(item,dst_total_path);
            dutInfo["total_path"] = totalPath_temp;
            dutInfo["dst_total_path"] = dst_total_path;

            // 生成log地址，todo：要和之前生成的log做一下对比

            string total_path_log = Path.Combine(setting.Record, dutInfo["start_date"].ToString(),
                dutInfo["mes_status"].ToString(),"log",
                dutInfo["sn"].ToString()+ "_"+ dutInfo["test_id"].ToString() +"_"+ dutInfo["nest_id"].ToString());

            FindFile(item, total_path_log);
            dutInfo["total_path_log"] = total_path_log;
            //SnSend();  // todo:暂时屏蔽
        }

        public void SnSend()
        {
            try
            {
                SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);

                string sn = (string)dutInfo["sn"];
                string total_path = dutInfo["total_path"].ToString();
                string total_path_log = dutInfo["total_path_log"].ToString();
                string zip_name = setting.zip_file_name;

                Dictionary<string, object> sendDictionary = new Dictionary<string, object>();
                sendDictionary["sn"] = sn;
                sendDictionary["zip_name"] = zip_name;
                sendDictionary["sn_path"] = total_path;
                sendDictionary["sn_log_path"] =total_path_log;
                sendDictionary["nest_id"] = dutInfo["nest_id"].ToString();
                var (result, message) = SocketDataClient.ClientCommunicate($"{sendDictionary}");
                if (!result)
                    throw new Exception($" server sn log error:{message}");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static string FindFile(ITestItem item, string path)
        {
            // 检查路径是否存在  
            if (File.Exists(path) || Directory.Exists(path))
            {
                // 路径存在，什么都不做  
                return path;
            }
            else
            {
                // 获取文件名及路径  
                string directoryPath = Path.GetDirectoryName(path);

                // 检查当前路径中是否包含文件扩展名  
                if (Path.GetFileName(path).Contains("."))
                {
                    // 创建文件所在的目录  
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
                else
                {
                    // 创建文件夹  
                    Directory.CreateDirectory(path);
                    item.AddLog($"Create folder is successful! {path}");
                }
            }

            return path;
        }

        public int SuperCalReadFW(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            string FW = string.Empty;
            string read = string.Empty;
            string main_app_ver = string.Empty;

            try
            {
                SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);
                main_app_ver = setting.main_app_ver;
                item.AddLog($"Trim后的main_app_ver为：'{main_app_ver}'");

                if (CMD_Command.TryGetValue("read_fw", out var readFWCmd) && readFWCmd is string)
                {
                    item.AddLog($"readCmd:{readFWCmd}; type:{readFWCmd.GetType()}");
                    string command = Convert.ToString(readFWCmd);
                    string temp_cmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", temp_cmd, timeout, ref read);
                    item.AddLog($"指令后的返回值为{read}");

                    if (!isOK)
                    {
                        item.AddLog($"执行读取SN的指令，但返回的bool值为false");
                        goto ReturnAndExit;
                    }

                    // 暂时这样获取，等知道具体的返回指令在修改
                    FW = read.StringParse("Main App Version").Trim();
                    item.AddLog($"FW:   '{FW}'");

                    if (FW.IndexOf(main_app_ver, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result = true;
                        item.AddLog($"读取产品的FW为：{FW},全部信息为：{read}");
                    }
                }
                else
                {
                    item.AddLog($"else error:readCmd:{readFWCmd}; type:{readFWCmd.GetType()}");
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"获取产品FW时出错：{ex}");
            }

            ReturnAndExit:
            ResultData resultData = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, FW);
            AddResult(item, resultData);
            return result ? 0 : 1;
        }


        public int SuperCalReadUuid(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            string uuid = string.Empty;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("read_uuid", out var readUuidCmd) && readUuidCmd is string)
                {
                    item.AddLog($"UUIDCmd:{readUuidCmd}");
                    string command = Convert.ToString(readUuidCmd);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);

                    if (!isOk)
                    {
                        item.AddLog($"读取的信息为：{readText},执行结果返回为{isOk}");
                        goto ReturnAndExit;
                    }

                    if (readText.Contains("UUID"))
                    {
                        readText = StringExtension_GTK.GetSubString(readText, "{", "}");
                        JObject readJson = JObject.Parse(readText);
                        uuid = readJson["UUID"].ToString();
                        item.AddLog($"读取到的uuid为：{uuid}");

                        //if (!(uuid.IsAbcNumber() || uuid.Length == 16))  // 这些||都是暂时修改
                        //{
                        //    result = false;

                        //}
                        //else
                        result = true;
                    }

                }
            }
            catch (Exception e)
            {
                item.AddLog($"获取产品uuid时出错：{e}");
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, uuid);
            AddResult(item, resultData);
            return result ? 0 : 1;

        }

        public int SuperCalReadHandedness(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            string Handedness = string.Empty;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("read_handedness", out var readHandedness) && readHandedness is string)
                {
                    item.AddLog($"readHandedness:{readHandedness}");
                    string command = Convert.ToString(readHandedness);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);

                    if (!isOk)
                    {
                        item.AddLog($"读取的信息为：{readText},执行结果返回为{isOk}");
                        goto ReturnAndExit;
                    }

                    if (readText.Contains("handedness"))
                    {
                        readText = StringExtension_GTK.GetSubString(readText, "{", "}");
                        JObject readJson = JObject.Parse(readText);
                        Handedness = readJson["handedness"].ToString();

                        item.AddLog($"读取到的uuid为：{Handedness}");

                        List<string> vaildList = new List<string>{ "left", "right" };
                        if (vaildList.Contains(Handedness))
                        {
                            result = true;
                        }
                      
                    }

                }
            }
            catch (Exception e)
            {
                item.AddLog($"获取产品uuid时出错：{e}");
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, Handedness);
            AddResult(item, resultData);
            return result ? 0 : 1;

        }

        public int SuperCalReadImuInfo(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            string imuInfo = string.Empty;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("read_imu_info", out var readImuInfo) && readImuInfo is string)
                {
                    item.AddLog($"read imuInfo:{readImuInfo}");
                    string command = Convert.ToString(readImuInfo);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);

                    if (!isOk)
                    {
                        item.AddLog($"读取的信息为：{readText},执行结果返回为{isOk}");
                        goto ReturnAndExit;
                    }

                    
                    readText = StringExtension_GTK.GetSubString(readText, "{", "}");
                    JObject readJson = JObject.Parse(readText);
                    result = readJson.ContainsKey("lsb_per_dps") && readJson.ContainsKey("lsb_per_c") &&
                              readJson.ContainsKey("offset_c");

                }
            }
            catch (Exception e)
            {
                item.AddLog($"获取产品imu_info 时出错：{e}");
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, readText);
            AddResult(item, resultData);
            return result ? 0 : 1;

        }

        public int OffLed(ITestItem item, bool irLedOff, bool displayLedOff)
        {
            bool result = true;
            try
            {
                if (irLedOff)
                {
                    result = IrLedOff(item);
                    if (!result) goto ReturnAndExit;

                }

                if (displayLedOff)
                {
                    result = DisplayLedOff(item);
                    if (!result) goto ReturnAndExit;
                }


            }
            catch (Exception ex)
            {
                item.AddLog($"{item.Title} error :{ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title,result?"": CreateErrorCode(item.Title).Name, result ? "PASS" : "FAIL");
            AddResult(item, resultData);
            return result ? 0 : 1;
        }

        public bool StopCom(ITestItem item,int timeout)
        {
            bool result = false;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("stop_com", out var StopComCmd) && StopComCmd is string)
                {
                    item.AddLog($"StopComCmd:{StopComCmd}");
                    string command = Convert.ToString(StopComCmd);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    for (int i = 0; i < 3; i++)
                    {
                        bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);
                        item.AddLog($"对产品下发StopComCmd指令，得到的结果为：{isOk},返回信息为：{readText}");

                        if (string.IsNullOrEmpty(readText))
                        {
                            result = true;
                            break;
                        }

                        item.Sleep(1000);
                        //Thread.Sleep(1000);
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                item.AddLog($"对产品进行StopComCmd指令 时出错：{e}");
            }

            return false;
        }


        public bool reboot_uart(ITestItem item, int timeout = 2000)
        {
            bool result = false;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("uart_reboot", out var uartRebootCmd) && uartRebootCmd is string)
                {
                    item.AddLog($"uart_reboot:{uartRebootCmd}");
                    string command = Convert.ToString(uartRebootCmd);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    for (int i = 0; i < 3; i++)
                    {
                        bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);
                        item.AddLog($"对产品下发uartRebootCmd指令，得到的结果为：{isOk},返回信息为：{readText}");

                        if (string.IsNullOrEmpty(readText))
                        {
                            result = true;
                            break;
                        }

                        item.Sleep(1000);
                        //Thread.Sleep(1000);
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                item.AddLog($"对产品进行uart RebootCmd指令 时出错：{e}");
            }

            return false;
        }


        public bool IrLedOff(ITestItem item, int timeout = 2000)
        {
            bool result = false;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("ir_led_off", out var ledOffCmd) && ledOffCmd is string)
                {
                    item.AddLog($"ledOffCmd:{ledOffCmd}");
                    string command = Convert.ToString(ledOffCmd);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    for (int i = 0; i < 3; i++)
                    {
                        bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);
                        item.AddLog($"对产品下发ir_led_off指令，得到的结果为：{isOk},返回信息为：{readText}");

                        if (readText.Trim() == "{}")
                        {
                            result = true;
                            break;
                        }

                        item.Sleep(1000);
                        //Thread.Sleep(1000);
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                item.AddLog($"对产品进行ir_led_off指令 时出错：{e}");
            }

            return false;
        }

        public bool DisplayLedOff(ITestItem item, int timeout = 2000)
        {
            bool result = false;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue("display_led_off", out var displayOffCmd) && displayOffCmd is string)
                {
                    item.AddLog($"displayOffCmd:{displayOffCmd}");
                    string command = Convert.ToString(displayOffCmd);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    for (int i = 0; i < 3; i++)
                    {
                        bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);
                        item.AddLog($"对产品下发displayOffCmd指令，得到的结果为：{isOk},返回信息为：{readText}");

                        if (readText.Trim() == "{}")
                        {
                            result = true;
                            break;
                        }

                        item.Sleep(1000);
                        //Thread.Sleep(1000);
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                item.AddLog($"对产品进行displayOffCmd指令 时出错：{e}");
            }

            return false;
        }

        /// <summary>
        ///  把两个函数整合为一个，参数commandName为指令的名称,"led_irled_cfg","led_irled_start"
        /// </summary>
        /// <param name="item"></param>
        /// <param name="commandName">指令的名称，根据指令的名称寻找具体的指令</param>
        /// <param name="timeout">超时</param>
        /// <returns></returns>
        public int LedIrLedCfg(ITestItem item, string commandName, int timeout = 10)
        {

            bool result = false;
            string readText = string.Empty;
            try
            {
                if (CMD_Command.TryGetValue(commandName, out var irled_cfg) && irled_cfg is string)
                {
                    item.AddLog($"irled_cfg:{irled_cfg}");
                    string command = Convert.ToString(irled_cfg);
                    string tempCmd = string.Format(command, pexFilePath, COMNUMBERTEMP);
                    bool isOk = ShellHelper.RunHideRead(item.AddLog, "python.exe", tempCmd, timeout, ref readText);

                    if (!isOk)
                    {
                        item.AddLog($"读取的信息为：{readText},执行结果返回为{isOk}");
                        goto ReturnAndExit;
                    }


                    readText = StringExtension_GTK.GetSubString(readText, "{", "}");
                    JObject readJson = JObject.Parse(readText);
                    if (!readJson.HasValues)
                        result = true;


                }
            }
            catch (Exception e)
            {
                item.AddLog($"{item.Title} error：{e}");
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? "PASS" : "Fail");
            AddResult(item, resultData);
            return result ? 0 : 1;

        }

        //public int StartLedData(ITestItem item)
        //{
        //    bool result;
        //    try
        //    {

        //         result = ShellContinue(item);

        //    }
        //    catch (Exception e)
        //    {
        //        item.AddLog($"{item.Title} error:{e}");
        //        result = false;
        //    }
        //    ReturnAndExit:
        //    ResultData resultData =
        //        new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? "PASS" : "Fail");
        //    AddResult(item, resultData);
        //    return result ? 0 : 1;

        //}

        //public bool ShellContinue(ITestItem item)
        //{
        //    try
        //    {
        //        item.AddLog($"线程开始执行");
        //        if (CMD_Command.TryGetValue("led_cal", out var ledCal) && ledCal is string)
        //        {
        //            string command = string.Format(ledCal.ToString(), pexFilePath, $"COM{Project.ProjectIndex + 1}");
        //            _workThread = new Thread(() => Sh(item, command))
        //            {
        //                IsBackground = true
        //            };
        //            _workThread.Start();
        //            return true;
        //        }

        //        item.AddLog($"{item.Title}--请检查指令是否正确");
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        item.AddLog($"准备执行线程时出错：{e}");
        //        return false;
        //    }
        //}

        //public void Sh(ITestItem item, string command)
        //{
        //    item.AddLog($"在函数Sh中准备执行的指令为：{command}");
        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = "cmd.exe",
        //        Arguments = $"/C {command}",
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };
        //    Process _process = new Process() { StartInfo = startInfo };
        //    _process.Start();

        //    while (!_stopRequested)
        //    {
        //        string line = _process.StandardOutput.ReadLine();
        //        if (line == null)
        //        {
        //            if (_process.HasExited)
        //            {
        //                item.AddLog($"{command} dut NG");
        //                break;
        //            }

        //            Thread.Sleep(100);
        //            continue;
        //        }

        //        // 处理数据
        //        if (line.Contains("{"))
        //        {
        //            try
        //            {
        //                var jsonData = JObject.Parse(line);
        //                allShData.Add(jsonData);

        //            }
        //            catch (Exception e)
        //            {
        //                item.AddLog($"json解析失败：{e}");
        //            }
        //        }
        //        else if (line.Contains("configuration done"))
        //        {
        //            item.AddLog("LED configuration done");
        //            led_config_done = true;

        //        }
        //    }

        //    // 清理资源
        //    if (!_process.HasExited)
        //    {
        //        _process.Kill(); // 终止进程
        //    }

        //    _process.Dispose();
        //    _process = null;


        //}

        //public void Stop()
        //{
        //    _stopRequested = true;
        //    if (_workThread != null && _workThread.IsAlive)
        //    {
        //        _workThread.Join(2000); // 等待线程结束，最多2秒
        //    }
        //}

        public int StartLedDate(ITestItem item)
        {
            bool result = false;
            try
            {
                recData.Clear();
                // TODO:在这里清除？
                do_rec_data = true;
                stringRecData = string.Empty;
                if (CMD_Command.TryGetValue("led_cal", out var ledCal) && ledCal is string)
                {
                    string command = string.Format(ledCal.ToString(), pexFilePath, COMNUMBERTEMP);
                    item.AddLog($"command------?   {command}");
                    result = true;
                    RunningTask(item, command);
                }

            }
            catch (Exception ex)
            {
                item.AddLog($"{item.Title} error:{ex}");

            }
            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? "PASS" : "Fail");
            AddResult(item, resultData);
            return result ? 0 : 1;

        }

        public async Task RunningTask(ITestItem item, string command)
        {
            // 启动异步任务，连续运行命令并读取命令响应
            Task commandTask = RunCommandAsync(item, command, cts.Token);
            item.AddLog($"Command: {command} is running in the background...");
            try
            {
                await commandTask;
            }
            catch (Exception e)
            {
                //
            }

        }

        public Task RunCommandAsync(ITestItem item, string command, CancellationToken token)
        {
            item.AddLog($"run command {command}");

            return Task.Run(() =>
            {
                //create new process
                cmdProcess = new Process();
                cmdProcess.StartInfo.FileName = "cmd.exe"; // 在cmd中运行指令
                cmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入
                cmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向输出
                cmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出
                cmdProcess.StartInfo.CreateNoWindow = true;         // 不创建窗口
                cmdProcess.StartInfo.UseShellExecute = false;       // 不使用系统外壳
                
                // 注册输出数据接受事件处理
                cmdProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        if (do_rec_data)
                        {
                            try
                            {
                                string _dataLine = args.Data;
                                if (_dataLine.Contains("{"))
                                {
                                    Dictionary<string, object> dataLineData =
                                        JsonConvert.DeserializeObject<Dictionary<string, Object>>(_dataLine);
                                    recData.Add(dataLineData);

                                }
                                else if (_dataLine.Contains("configuration done"))
                                {
                                    led_config_done = true;
                                }

                            }
                            catch (Exception e)
                            {
                                //
                            }
                            //item.AddLog("Output: " + args.Data); // 实时处理标准输出内容
                            using (StreamWriter sw = new StreamWriter("C:/a.txt", append: true))
                            {
                                sw.WriteLine(args.Data);
                            }
                        }
                    }
                    else if(args.Data.ToString() == "")
                    {
                        //if (recErrorDataCount == 0) item.AddLog($"have error data！");
                        recErrorDataCount += 1;
                    }

                };

                // 注册错误消息接受事件 //TODO: 注册错误数据接收事件处理时软件会异常退出，不知道到为什么
                cmdProcess.ErrorDataReceived += (sender, args) =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(args.Data.ToString()))
                        {
                            //item.AddLog("Error: " + args.Data);
                            //Console.WriteLine("Error: " + args.Data); // 实时处理错误输出内容
                            recErrorDataCount += 1;
                        }

                    }
                    catch (Exception e)
                    {
                        item.AddLog($"接受处理错误输出的事件时出错：{e}");
                        //
                    }

                };

                cmdProcess.Start(); //启动进程

                // 异步读取标准输出和错误输出
                cmdProcess.BeginErrorReadLine();
                cmdProcess.BeginOutputReadLine();

                // 向命令行输入持续性命令
                cmdProcess.StandardInput.WriteLine(command); 
                // 等待进程结束
                cmdProcess.WaitForExit();

            },token);


        }

        public int TerminateChildProcesses(ITestItem item)
        {
            bool result = false;
            item.AddLog($"Terminate Process");
            try
            {
                int parentId = cmdProcess.Id;
                string query = $"Select * From Win32_Process Where ParentProcessId={parentId}";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection moc = searcher.Get())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        int childProcessId = Convert.ToInt32(mo["ProcessId"]);
                        try
                        {
                            Process childProcess = Process.GetProcessById(childProcessId);
                            childProcess.Kill();
                            Console.WriteLine($"Terminated child process {childProcessId}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to terminate child process {childProcessId}: {ex.Message}");
                        }
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while terminating child processes: {ex.Message}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        
        public int SendImagePathToCam(ITestItem item)
        {
            bool result = false;
            try
            {
                Directory.CreateDirectory(totalPath);
                
                var (result_,message) = SocketCameraClient.ClientCommunicate($"path,{totalPath}");
                result = result_;   
                item.AddLog($"发送图片路径,获取到的返回信息为{message}");

            }
            catch (Exception e)
            {
                item.AddLog($"发送图片路径时出错：{e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        // 清空地址
        public int SendImageNullPathToCam(ITestItem item)
        {
            bool result = false;
            try
            {
                var (result_, message) = SocketCameraClient.ClientCommunicate($"empty,{111111}");
                result = result_;
                item.AddLog($"image empty path: res:{result} message:{message}");

            }
            catch (Exception e)
            {
                item.AddLog($"image empty path error：{e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }





        // 获取LED数据
        public int rec_turbo_raw_data(ITestItem item)
        {
            bool result = true;
            try
            {
                var tempRecLEDData = recData;
                // to str
                string tempData = string.Join("", tempRecLEDData.Select(i => JsonConvert.SerializeObject(i) + "\r"));
                stringRecData = tempData;
                item.AddLog($"stringRecData-->{stringRecData}");
            }
            catch (Exception e)
            {
                item.AddLog($"{item.Title} error:{e}");
                result = false;
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public void SuperCalCopyFile(ITestItem item, string filePath, string folder, string fileName)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string destinationPath = Path.Combine(folder, Path.GetFileName(filePath));
                
                // 复制文件
                File.Copy(filePath,destinationPath,true);
                
                while (true)
                {
                    item.Sleep(100);
                    string[] files = Directory.GetFiles(folder).Select(Path.GetFileName).ToArray();
                    item.AddLog($"sn_path_file_lst:{string.Join(",",files)}");
                    if (files.Contains(fileName))
                    {
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                item.AddLog($"copy file generate error {e.Message}");
                throw;
            }

        }

        public void saveRawData(ITestItem item, string rwaDataPath, string rawData)
        {
            try
            {
                item.AddLog($"save_path:{rwaDataPath}  data: {rawData}");
                using (StreamWriter writer = new StreamWriter(rwaDataPath))
                {
                    writer.Write(rawData);
                }

            }
            catch (Exception e)
            {
                    item.AddLog($"写入文件时发生错误: { e.Message}");
                    throw;
            }
        }

        public bool CKFileSize(ITestItem item, string FolderPath, string fileName, int fileSize)
        {
            var fileList = Directory.GetFiles(FolderPath).Select(Path.GetFileName).ToList();
            var fileCount = fileList.Where(i => i == fileName).ToList();
            // 检查文件是否存在且仅存在一个 
            bool ckFileResult = (fileCount.Count == 1);

            var filePath = Path.Combine(FolderPath, fileCount[0]);
            // 检查文件大小
            
            long fileSizeGet = new FileInfo(filePath).Length;
            item.AddLog($"{filePath} size {fileSizeGet} byte");
            ckFileResult &= (fileSizeGet >= fileSize);
            return ckFileResult;

        }



        public int saveDataLed(ITestItem item)
        {
            bool result = false;
            bool SnPathFileCheck = true;

            string sn = Project.SerialNumber;
            //totalPath = @"D:\cobalt";  //todo
            var sn_data_path = totalPath;
            item.AddLog($"{sn} save led data to {sn_data_path}");

            try
            {
                if (! string.IsNullOrEmpty(sn))
                {
                    string StageCalPath = SuperCalConfigPath + "\\stagecal.json";
                    SuperCalCopyFile(item,StageCalPath, sn_data_path, "stagecal.json");
                    string turbocal_raw_data_path = Path.Combine(sn_data_path, "turbocal_raw_data.txt");
                    saveRawData(item, turbocal_raw_data_path, stringRecData);
                    for (int i = 0; i < 50; i++)
                    {
                        item.Sleep(100);
                        SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);
                        int fileSize = setting.rawDataFile_minSize;
                        int camNumber = setting.cam_num;

                        Func<string, int, bool> checkFileNO = (sn_data_path_, camNumber_) =>
                        {
                            string[] fileNumber = Directory.GetFiles(sn_data_path).Select(Path.GetFileName).ToArray();
                            if (fileNumber.Length == camNumber + 1)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        };

                        // todo 暂时屏蔽
                        SnPathFileCheck &= checkFileNO(sn_data_path, camNumber);
                        SnPathFileCheck &= CKFileSize(item, sn_data_path, "turbocal_raw_data.txt", fileSize);
                        var fileList = Directory.GetFiles(sn_data_path).Select(Path.GetFileName).ToList();
                        //var camFileCount = fileList.Where(n => n.Contains("cam_")).ToList();
                        //SnPathFileCheck &= (camFileCount.Count == camNumber);
                        //这个时候还没有图片文件 还没发给相机服务器
                        SnPathFileCheck &= (fileList.Where(n => n == "stagecal.json").ToList().Count == 1);
                        result = SnPathFileCheck;
                        item.AddLog($"recErrorDataCount  ->{recErrorDataCount}");
                        if (result)
                        {
                            break;
                        }

                    }

                }

            }
            catch (Exception e)
            {
               item.AddLog($"save led data generate error:{e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public List<int> GetAllCamImgNumber(ITestItem item, string imagePath,string imageEndName)
        {
            List<int> imageNumberList = new List<int>();
            

            // 使用 Directory.GetDirectories 来选择所有匹配的文件夹 
            
            // 使用 LINQ 筛选出满足条件的图像文件  
            foreach (var directory in Directory.GetDirectories(imagePath,"cam_*"))
            {
                // 获取文件夹中的所有文件
                string[] allDir = Directory.GetFiles(directory);
                // 使用 LINQ 筛选出满足条件的图像文件 
                var fileList = allDir.Where(file => file.EndsWith($".{imageEndName}",StringComparison.OrdinalIgnoreCase)).ToList();
                imageNumberList.Add(fileList.Count);

            }

            return imageNumberList;

        }
        public int TestSleep(ITestItem item, int sleepTime)
        {
            item.AddLog($"sleep:{sleepTime / 1000}s");
            item.Sleep(sleepTime);
            return 0;
        }

        public int CheckLedImage(ITestItem item, ushort plcTriggerAddress)
        {
            bool result = false;
            try
            {
                ModbusTcpClient targetPLC = _Context.PLCClient;
                var triggerCount = targetPLC.ReadMW(1,plcTriggerAddress);

                item.AddLog($"get plc trigger count {triggerCount}");

                var countList = GetAllCamImgNumber(item, totalPath, "bmp");
                SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);
                int snapImageCount = countList[0];

                if (countList.Count == setting.cam_num && countList.Distinct().Count() == 1 && 
                    Math.Abs(triggerCount - snapImageCount) < 4 && setting.img_no_min < countList[0] && countList[0] < setting.img_no_max)
                {
                    result = true;
                }

            }
            catch (Exception e)
            {
                item.AddLog($"check led image generate error: {e}");
                
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }


        public int ProcessSend(ITestItem item)
        {
            bool result = false;
            try
            {
                Dictionary<string, Dictionary<string, object>> sendDictionary =
                    new Dictionary<string, Dictionary<string, object>>();

                string algorithmKey = "processing data";
                Dictionary<string, object> internalDictionary = new Dictionary<string, object>()
                {
                    { "sn_path", totalPath },
                    { "nest_id",  Project.ProjectIndex + 1},
                };

                sendDictionary[algorithmKey] = internalDictionary;
                //JObject jsonDate = JObject.Parse(sendDictionary);
                var jsonstring = JsonConvert.SerializeObject(sendDictionary);

                item.AddLog($"sendDictionary:{jsonstring}");
                var (_ret,message) = SocketDataClient.ClientCommunicate($"{jsonstring}");
                result = _ret;
                item.AddLog($" send  processing data: res:{result} message:{message}");
            }
            catch (Exception e)
            {
                item.AddLog($"image empty path error：{e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int initial_com(ITestItem item)
        {
            bool result = false;
            try
            {  // todo:读取SN板的信息，并跟随sn获取对应的com
                SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);

                var uartSnDict = setting.uart_sn_nest_dict;
                item.AddLog($"UART_dict:{uartSnDict}");
                for (int i = 1; i < 9; i++)
                {
                    item.AddLog($"{i.ToString()}_sn:{uartSnDict[i - 1]}");
                    var dict = uartSnDict[i-1].Value;
                    item.AddLog($"======={dict}========");
                }

                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"initial_com error：{e}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }



        public int SaveDataprocConfig(ITestItem item)
        {
            bool result = false;
            try
            {
                totalPath = Path.Combine(camImagePath, Project.IsOnLine ? "Online" : "Offline",
                    Project.SerialNumber + "_" + test_id + "_" + Project.ProjectIndex+1);

                Directory.CreateDirectory(totalPath);

                string savePath = Path.Combine(totalPath, "config.json");
                // todo:未更新por_dual
                
                string jsonData = JsonConvert.SerializeObject(savaDataDict,Formatting.Indented);
                File.WriteAllText(savePath,jsonData);
                item.AddLog($"SaveDataprocConfig_path:{savePath},data:{jsonData}");
                result = true;

            }
            catch (Exception e)
            {
                item.AddLog($"SaveDataprocConfig_path error:{e}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public int SerSuperCalAlgoStatus(ITestItem item,bool status)
        {
            SuperCalALgoOn = status;
            return 0;
        }


        public int DataUploadAlgo(ITestItem item)
        {
            bool result = false;
            try
            {
                if (SuperCalALgoOn)
                {
                    AlgoOnSend(item);
                }

                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"DataUploadAlgo error:{e}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        // 给数据处理服务器发送数据，mes online 为 1 ，否则为0
        public void AlgoOnSend(ITestItem item)
        {
            string sn = dutInfo["sn"].ToString();
            string totalPath_dst = dutInfo["dst_total_path"].ToString();
            string testID_temp = dutInfo["test_id"].ToString();
            string mesStatus = Project.IsOnLine ? "1" : "0";
            string nestIdTemp = dutInfo["nest_id"].ToString();
            string imuSaveTemp = "0"; //这个需要发送吗
            Dictionary<string, Dictionary<string, object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();
            string algorithmKey = "algo on";
            Dictionary<string, object> internalDictionary = new Dictionary<string, object>() {
                { "sn_path", totalPath_dst },
                { "nest_id", nestIdTemp },
                {"sn",sn},
                {"test_id",testID_temp},
                {"mes_status_algo",mesStatus},
                {"imu_saved",imuSaveTemp}
            };
            sendDictionary[algorithmKey] = internalDictionary;

            var jsonstring = JsonConvert.SerializeObject(sendDictionary);
            item.AddLog(jsonstring);
            var (result, message) = SocketDataClient.ClientCommunicate($"{jsonstring}");
            if (!result)
                throw new Exception($" server sn log error:{message}");
        }

        public int DataMesCheck(ITestItem item)
        {
            bool result = false;
            try
            {
                if (Project.IsOnLine)
                {
                    string sn = dutInfo["sn"].ToString();
                    Dictionary<string, Dictionary<string, object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();
                    string algorithmKey = "mes check";
                    Dictionary<string, object> internalDictionary = new Dictionary<string, object>() {
                        { "sn", sn }
                    };
                    sendDictionary[algorithmKey] = internalDictionary;

                    var (result_, message) = SocketDataClient.ClientCommunicate($"{sendDictionary}");
                    if (result_)
                        result = true;
                }

            }
            catch (Exception e)
            {
                item.AddLog($"DataMesCheck error:{e}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int MesPostSend(ITestItem item)
        {
            bool result = false;
            try
            {
                if (Project.IsOnLine)
                {
                    string sn = dutInfo["sn"].ToString();
                    dutInfo["payload"] = ""; //payLoad未定义，需要确认是否需要定义
                    string payload = dutInfo["payload"].ToString();
                    Dictionary<string, Dictionary<string, object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();
                    string algorithmKey = "mes post";
                    Dictionary<string, object> internalDictionary = new Dictionary<string, object>() {
                        { "sn", sn },
                        {"sn_payload",payload},
                        {"nest_id",dutInfo["nest_id"].ToString()}
                    };
                    sendDictionary[algorithmKey] = internalDictionary;

                    var (result_, message) = SocketDataClient.ClientCommunicate($"{sendDictionary}");
                    if (result_)
                        result = true;
                }

            }
            catch (Exception e)
            {
                item.AddLog($"MesPostSend error:{e}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int SumCsvSend(ITestItem item)
        {
            bool result = false;
            try
            {
                sumCsvSend(item);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public void sumCsvSend(ITestItem item) 
        {

            SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath);
            string productName = setting.Product;
            string line = setting.Line;
            string section = setting.section;
            string testResult;
            if (string.IsNullOrEmpty(Project.GetErrorCodes()))
            {
                 testResult = "PASS";
            }
            else
            {
                 testResult = "Fail";
            }
            
            DateTime timeD = (DateTime)dutInfo["start_time_cnt"];
            int costTime = (int)(DateTime.Now - timeD).TotalSeconds;

            dutInfo["costTime"] = costTime;

            string fileName =
                $"[{dutInfo["sn"]}][{dutInfo["start_date_time"]}][{productName}][{line}_{section}][{testResult}]";

            Dictionary<string, Dictionary<string, object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();

            string algorithmKey = "sum csv";
            Dictionary<string, object> internalDictionary = new Dictionary<string, object>
            {
                { "dut_info", "dictTemp" },
                {"path",$"{dutInfo["sum_file_path"]}"},
                {"folder_path",Path.Combine($"{dutInfo["total_path_log"]}",fileName)}
            };// todo:发送的数据不全 待确认

            sendDictionary[algorithmKey] = internalDictionary;
            string jsonString = JsonConvert.SerializeObject(sendDictionary);
            item.AddLog(jsonString);
            var (result, message) = SocketDataClient.ClientCommunicate($"{jsonString}");
            
            if (!result)
                throw new Exception($" sumCsvSend error:{message}");
        }

        // todo:    def save_payload_send(self):
        // todo:clean_up_send

        public int CleanUpNonPresentDevices(ITestItem item)
        {
            bool result = false;
            try
            {
                string readread = string.Empty;
                int timeout = 15000;
                //string tool_path = $"{Application.StartupPath}\\cleanup_tool";
                string tool_path = $"D:\\Python\\cleanup_tool";
                string command = "remove_non_present_devices";
                string cmd = $"D: && cd {tool_path} && {command}";

                item.AddLog(cmd);

                //var startInfo = new ProcessStartInfo 
                //{
                //    FileName = "cmd.exe",  // 在cmd中运行指令
                //    Arguments = $"{cmd}",
                //    RedirectStandardInput = true,// 在cmd中运行指令
                //    RedirectStandardOutput = true,
                //    UseShellExecute = false,
                //    CreateNoWindow = true,
                //    RedirectStandardError = true// 重定向错误输出

                //};
                //Process _process = new Process() { StartInfo = startInfo };
                //_process.Start();
                //_process.WaitForExit(timeout * 1000);
                //for (int i = 0; i < 50; i++)
                //{ 
                // readread = _process.StandardOutput.ReadLine();
                // item.AddLog($"READ:{readread}");
                //}
                //readread = _process.StandardOutput.ReadToEnd();
                //string error = _process.StandardError.ReadLine();


                bool isOK = ShellHelper.RunHideRead(item.AddLog, "cmd.exe", cmd, timeout, ref readread);

                item.AddLog($"result: {isOK}, READ:{readread}");
                result = true;
            }
            catch (Exception e)
            {

                item.AddLog(item.Title.ToString());
                item.AddLog(e.ToString());

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int WriteConfigDut(ITestItem item,int timeout = 10000)
        {
            bool result = false;
            string readwrite = string.Empty;
            try
            {
                string irled_config_path = Path.Combine(totalPath, "config.json");
                if (CMD_Command.TryGetValue("write_config_dut", out var readFWCmd) && readFWCmd is string)
                {
                    item.AddLog($"readCmd:{readFWCmd}; type:{readFWCmd.GetType()}");
                    string command = Convert.ToString(readFWCmd);
                    string temp_cmd = string.Format(command, pexFilePath, irled_config_path, COMNUMBERTEMP);
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "cmd.exe", temp_cmd, timeout, ref readwrite);
                    item.AddLog($"cmd ：{temp_cmd}   指令后的返回值为{readwrite}");
                    string readText = StringExtension_GTK.GetSubString(readwrite, "{", "}");
                    item.AddLog($"READ:{readwrite}");
                    //JObject readJson = JObject.Parse(readText);
                    if (!isOK)
                    {
                        item.AddLog($"执行WriteConfigDut的指令，但返回的bool值为false");
                        goto ReturnAndExit;
                    }
                   

                    //temp_cmd = $"python.exe {temp_cmd}";
                    //item.AddLog($"cmd ：{temp_cmd}");
                    //var startInfo = new ProcessStartInfo {
                    //    FileName = "cmd.exe",  // 在cmd中运行指令
                    //    Arguments = $"{temp_cmd}",
                    //    RedirectStandardInput = true,// 在cmd中运行指令
                    //    RedirectStandardOutput = true,
                    //    UseShellExecute = false,
                    //    CreateNoWindow = true,
                    //    RedirectStandardError = true// 重定向错误输出

                    //};
                    //Process _process = new Process() { StartInfo = startInfo };
                    //_process.Start();
                    //_process.WaitForExit(timeout);

                    //readwrite = _process.StandardOutput.ReadToEnd().ToString();

                    //if (!_process.HasExited)
                    //{
                    //    _process.Kill(); // 终止进程
                    //}

                    //_process.Dispose();
                    //_process = null;

                    
                
                    result = true;
                }
            }
            catch (Exception e)
            {

                item.AddLog($"WriteConfigDut Error: {e.Message}");
                

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int SetSoltTwoTestStatus(ITestItem ite ,bool status)
        {
            bool result = false;
            soltTwoTestMake = status;
            result = true;
            return result ? 0 : 1;
        }

        public int GetSoltTwoTestStatus(ITestItem item,int timeout)
        {
            
            bool result = false;
            while (true)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                if (soltTwoTestMake)
                {
                    soltTwoTestMake = false;
                    result = true;
                    break;
                }

                if (stopwatch.ElapsedMilliseconds > timeout)
                {
                    goto ReturnAndExit;
                }
                item.Sleep(100);
                
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }










        public int SuperCal_StringCheck(ITestItem item, string limitName, int preWaiting = 0, int afterWaiting = 0,
            int retryWaiting = 0)
        {
            bool result = false;
            string resultString = string.Empty;
            ItemLimit limit = null;
            bool isDataOK = false;

            try
            {
                limit = _Limits.GetLimit(limitName);

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                resultString = "os_version:123.456";
                result = CheckStringLimit(item, item.Title, resultString, limit);
                isDataOK = true;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString, limit);

            return result ? 0 : 1;
        }


        public int SuperCal_ValueCheck(ITestItem item, string limitName, int preWaiting = 0, int afterWaiting = 0,
            int retryWaiting = 0)
        {
            bool result = false;
            string resultString = string.Empty;
            ItemLimit limit = null;
            bool isDataOK = false;

            try
            {
                limit = _Limits.GetLimit(limitName);

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                resultString = "123.456";
                double dTemp = double.Parse(resultString);
                result = CheckLimit(item, item.Title, CreateErrorCode(item.Title).Name, dTemp, limit);
                isDataOK = true;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


            if (!isDataOK)
                AddFailedResult(item, item.Title, result ? "" : CreateErrorCode(item.Title).Name, resultString, limit);

            return result ? 0 : 1;
        }


    }
}
