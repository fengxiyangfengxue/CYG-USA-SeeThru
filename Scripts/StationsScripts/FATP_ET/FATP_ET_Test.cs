using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.StationsScripts.FATP_ET;
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
using System.Runtime.Remoting.Contexts;
using System.Globalization;
using Test.StationsScripts.Shared;
using System.IO.Compression;
using System.Security.RightsManagement;
using CsvHelper;
using CsvHelper.Configuration;
using System.Net.Http;
using Emgu.CV.Ocl;
using NLog;


namespace Test
{
    public partial class MainClass
    {
        private readonly object ET_lock = new object();

        public AdbCommandRunner ET_cmd_runner = null;


        public string ETTestDir = string.Empty;
        public string ETTimestamp = string.Empty;
        public string ETCalTestId = string.Empty;    //PYTHON 代码中定义了这个TestId的变量，但是并没有给它赋值，我记不清为什么没赋值也有多出地方调用它。可能就是为了使用空值？
        private string ETIOTCalTestID;           // 为了不使用空值，定义一个IOT的TestID.待确认具体使用方式，目前只能暂时和python中代码一致。

        public string ETDutVrsName = string.Empty;
        public string ETExtCamVrsName = string.Empty;
        public Process ETExtCamProce = null;
        public string ETZipFilePath = string.Empty;
        public string ETPullJsonPathName = string.Empty;
        public bool? ETRecordMark = null;
        public Dictionary<string, object> iotcalTestIdDict = new Dictionary<string, object>();

        public string ETPullCsvPath = string.Empty;
        public string ETPushJsonPathName = string.Empty;


        public Dictionary<string, string> vrsNameDict = new Dictionary<string, string>
        {
            { "Carpo_EtCalStation", "" },
            { "Carpo_EtCalBlackbox", "" },
            { "Carpo_EtCalIlluminator", "" }
        };

        public Dictionary<string, string> uploadBuildConfigNameDict = new Dictionary<string, string>
        {
            { "Carpo_EtCalStation", "station" },
            { "Carpo_EtCalBlackbox", "blackbox_optimized" },
            { "Carpo_EtCalIlluminator", "illuminator_data_storage_only" }
        };

        // 给线程使用
        private volatile bool ET_stopRequested;


        public int ETHKWebRecord(ITestItem item)
        {
            bool result = false;
            try
            {
                webRecorder.StartRecording();
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"record Web error:{e}");
            }

            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "Pass" : "Fail");
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ETHKWebEndRecord(ITestItem item)
        {
            bool result = false;
            try
            {
                webRecorder.StopRecording();
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"record Web error:{e}");
            }

            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "Pass" : "Fail");
            AddResult(item, data);
            return result ? 0 : 1;
        }


        // 获取OPID
        public int ETGetOpID(ITestItem item)
        {
            bool result = false;
            BarCodeConfig config = new BarCodeConfig() {
                Title = "Input your job ID(length >= 6)",
            };

            //check barcode length = 6
            config.ValidationHandler += (s) =>
            {
                return s.Length >= 6;
            };
            string barcode = string.Empty;
            //string barcode = BarCodeHelper.Get(Project, config);
            //item.AddLog("OPID = " + barcode);
            for (int i = 1; i < 6; i++)
            {
                config.MakeLower = true;
                config.MakeUpper = false;
                barcode = BarCodeHelper.Get(Project, config);
                item.AddLog("OPID = " + barcode);
                if (barcode.IsNumber())
                {
                    result = true;
                    Project.ProjectDictionary["OPID"] = barcode;
                    break;
                }
            }

            if (!barcode.IsNumber())
            {
                result = false;
            }

            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                barcode);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        // 初始化执行ADB的类的实例
        public int ETGenerateAdbCommandRunnerinstance(ITestItem item, string adbPath)
        {
            bool result = false;
            try
            {
                var adbCommand =
                    ETjsonCmdData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
                item.AddLog($"cmd: {adbCommand}");
                foreach (KeyValuePair<string, string> kvp in adbCommand)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;
                    item.AddLog($"key:{key},value:{value}");
                }


                if (ET_cmd_runner == null)
                {
                    ET_cmd_runner = new AdbCommandRunner(adbCommand, adbPath);
                }

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"generate AdbCommandRunner instance error: {ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        // 为ET的检查校准文件是否超时
        public int ETCheckNoDutTime(ITestItem item, int testInterval, string checkCmd)
        {
            bool result = false;
            string fileDateTime = "19700101000000";
            DateTime dateTimeLocal = DateTime.MinValue;
            string read = string.Empty;

            try
            {
                bool isOK = ShellHelper.RunHideRead(item.AddLog, "cmd.exe", checkCmd, 10, ref read);
                item.AddLog($"READ:{read}");

                if (isOK)
                {
                    // 解析 JSON
                    var jsonDict = JsonConvert.DeserializeObject<JObject>(read);
                    // 获取时间字符串
                    string notDutLastTime = jsonDict["FileFormat"]["Timestamp"].ToString();
                    string nowStart = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    // 将字符串转换为 DateTime  
                    DateTime lastTime = DateTime.ParseExact(notDutLastTime, "yyyy-MM-ddTHH:mm:ss", null);
                    DateTime nowTime = DateTime.ParseExact(nowStart, "yyyy-MM-ddTHH:mm:ss", null);
                    // 计算时间差
                    int diffDays = (int)(nowTime - lastTime).TotalDays;

                    if (diffDays > testInterval)
                    {
                        item.AddLog(
                            $"It has been more than {testInterval} days since the last camera calibration test. Please conduct the camera calibration test");
                        var config = new UIMessageBoxConfig() {
                            Title = "Calibration timeout",
                            Text = "Please conduct the camera calibration test！",
                            TextFontSize = 20,
                            TextColor = Colors.Red,
                            Button = UIMessageBoxButton.OK,
                            AliveWith = Project, //alive with Project
                            WaitForExit = false //non-block
                        };
                        UIMessageBox.Show(Project, config);
                        goto ReturnAndExit;
                    }

                    result = true;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"Check Calibration cameraFile error: {ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        /// <summary>
        /// 读取SN 和下发其他命令
        /// </summary>
        /// <param name="item"></param>
        /// <param name="timeout_"></param>
        /// <returns></returns>
        public int ETReadSnAndOtherCommand(ITestItem item, int timeout_ = 10000)
        {
            bool result = false;
            string SN = string.Empty;
            string command = string.Empty;
            string read = string.Empty;

            List<string> othreadCmd = new List<string> {
                "adb_devices","adb_reboot","adb_wait_for_device","adb_root","adb_persist_Exposure_true",
                "adb_persist_Gian_true","adb_root","echo_wait_for_device","adb_wait_for_device","adb_root",
                "adb_remount","adb_syncboss","adb_vendor_oculus","adb_trackingfidelity","adb_display_off"
            };
            try
            {
                foreach (var key in othreadCmd)
                {
                    // RunCommand
                    var (success, res) = ET_cmd_runner.RunCommand(item, key, timeout: timeout_);
                    if (key == "adb_devices")
                    {
                        // 处理序列号获取逻辑
                        var lines = res.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                        if (lines.Length >= 2)
                        {
                            var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 1)
                            {
                                Project.SerialNumber = parts[0];
                                item.AddLog($"read sn is {Project.SerialNumber}");
                            }
                        }
                    }

                    if (!success)
                    {
                        result = false;
                        goto ReturnAndExit;
                    }
                }

                if (!Project.SerialNumber.IsAbcNumber())
                {
                    item.AddLog($"读取到的SN不符合规格: {Project.SerialNumber}");
                    result = false;
                    goto ReturnAndExit;
                }

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"读取SN的时候出错 error: {ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData resultData = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, SN);
            AddResult(item, resultData);
            return result ? 0 : 1;
        }


        /// <summary>
        /// 创建文件夹结构
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isNotDUt">是否有dut</param>
        /// <param name="Stability">是否是Stability</param>
        /// <returns></returns>
        public int ETCreateFolderStructure(ITestItem item, bool isNotDUt = false, bool Stability = false)
        {
            bool result = false;

            try
            {
                ETTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                if (isNotDUt)
                {
                    Project.SerialNumber = "NODUT";
                }

                ETTestDir = Path.Combine((string)jsonConfigData["output_path"],
                    $"{Project.SerialNumber}_{ETTimestamp}");
                var _tempWorkingDir = ETTestDir;

                if (!Stability && !isNotDUt)
                {
                    // 三层嵌套目录结构创建
                    foreach (var cam in new[] { "docl", "docr" })
                    {
                        foreach (var color in new[] { "red", "green", "blue" })
                        {
                            foreach (var image in (string[])jsonConfigData["image_names"])
                            {
                                var fullPath = Path.Combine(
                                    ETTestDir,
                                    "display",
                                    cam,
                                    color,
                                    image
                                );

                                // 使用更高效的目录创建方式
                                Directory.CreateDirectory(fullPath);
                            }
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(_tempWorkingDir);
                }

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"{item.Title} error :{ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? "PASS" : "FAIL");
            AddResult(item, resultData);
            return result ? 0 : 1;
        }

        #region read IAD

        public int ETReadIAD(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            bool readIAD = false;
            bool readIAD2 = false;
            float? distance1 = null;
            float? distance2 = null;
            try
            {
                var (success1, result1Str) = ET_cmd_runner.RunCommand(item, "adb_shell_IAD", timeout: timeout);
                if (!success1)
                {
                    item.AddLog($"run adb shell_IAD ERROR: {result1Str}");
                    goto ReturnAndExit;
                }

                distance1 = ParseDistance(result1Str, lineIndex: 4); // 对应Python的ret_1[1][4]
                readIAD = IsValidDistance(distance1);

                // 执行第二个命令并解析结果
                var (success2, result2Str) = ET_cmd_runner.RunCommand(item, "adb_shell_IAD_meters", timeout: timeout);
                if (!success2)
                {
                    item.AddLog($"run adb shell_IAD fail:{result2Str}");
                    goto ReturnAndExit;
                }

                distance2 = ParseDistance(result2Str, lineIndex: 1); // 对应Python的ret_2[1][1]
                readIAD2 = IsValidDistance(distance2);
                item.AddLog($"READ IAD RESULT: 1->{distance1},2->{distance2}");

                if (readIAD || readIAD2)
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                item.AddLog($"获取产品IAD distance时出错：{e}");
                result = false;
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                    readIAD ? distance1.ToString() : distance2.ToString());
            AddResult(item, resultData);
            return result ? 0 : 1;
        }

        /// <summary>
        /// 解析距离值（带安全校验）
        /// </summary>
        private float? ETParseDistance(string commandResult, int lineIndex)
        {
            try
            {
                // 分割结果行
                var lines = commandResult.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                // 安全校验行索引
                if (lines.Length <= lineIndex)
                {
                    Console.WriteLine($"结果行数不足，期望至少{lineIndex + 1}行，实际{lines.Length}行");
                    return null;
                }

                // 解析数值部分
                var line = lines[lineIndex];
                var parts = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 1)
                {
                    Console.WriteLine($"无效的数据格式: {line}");
                    return null;
                }

                // 取最后一个冒号后的值
                var valueStr = parts.Last().Trim();

                if (float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }

                Console.WriteLine($"无法解析数值: {valueStr}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"距离解析异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 验证距离有效性（带范围容差）
        /// </summary>
        private bool ETIsValidDistance(float? distance)
        {
            const float min = 0.063f;
            const float max = 0.0651f;
            const float tolerance = 0.00001f; // 浮点数比较容差

            return distance.HasValue &&
                   (distance.Value > min - tolerance) &&
                   (distance.Value < max + tolerance);
        }

        #endregion

        public int ETPullIOTCalibrationFiles(ITestItem item, int timeout = 10000)
        {
            bool result = false;

            string read = string.Empty;

            try
            {
                // 拉取IMU校准文件
                var imuReplaceParams = new Dictionary<string, string> {
                    ["file_to_pull"] = "/persist/calibration/imu_calibration.json",
                    ["output_path"] = Path.Combine(ETTestDir, "imu_calibration.json")
                };
                var (imuSuccess, imuResult) =
                    ET_cmd_runner.RunCommand(item, "adb_pull", imuReplaceParams, timeout: timeout);
                if (!imuSuccess)
                {
                    item.AddLog($"pull Calibration Files error: {imuResult}");
                    goto ReturnAndExit;
                }

                var cameraReplaceParams = new Dictionary<string, string> {
                    ["file_to_pull"] = "/persist/calibration/camera_calibration.json",
                    ["output_path"] = Path.Combine(ETTestDir, "camera_calibration.json")
                };

                var (cameraSuccess, cameraResult) =
                    ET_cmd_runner.RunCommand(item, "adb_pull", cameraReplaceParams, timeout: timeout);
                if (!cameraSuccess)
                {
                    item.AddLog($"Camera calibration file pull error: {cameraResult}");
                    goto ReturnAndExit;
                }

                // 读取并解析校准文件
                var outputPath = cameraReplaceParams["output_path"];
                if (!File.Exists(outputPath))
                {
                    item.AddLog($"校准文件不存在: {outputPath}");
                    goto ReturnAndExit;
                }

                try
                {
                    var jsonContent = File.ReadAllText(outputPath);
                    var iotCalibration = JsonConvert.DeserializeObject<JObject>(jsonContent);

                    // 安全访问嵌套属性
                    ETCalTestId = iotCalibration?["Metadata"]?["NamedTags"]?["cal_test_id"]?.Value<string>();
                    if (string.IsNullOrWhiteSpace(ETCalTestId))
                    {
                        item.AddLog("cal_test_id值为空或不存在");
                        goto ReturnAndExit;
                    }

                    item.AddLog($"成功获取IOT cal_test_id: {ETCalTestId}");
                    result = true;
                }
                catch (JsonException ex)
                {
                    item.AddLog($"JSON解析失败: {ex.Message}");
                    result = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"未知错误: {ex.Message}");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"Check Calibration cameraFile error: {ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        #region dut statr dut recode Vrs func

        public int ETStartVRS(ITestItem item, int timeout = 50000,string vrs_name = "recording2.vrs", string duration = "14")
        {
            bool result = false;

            try
            {
                item.AddLog($"dut statr recode Vrs");
                ETDutVrsName =
                    $"{vrs_name}_{Project.SerialNumber}_{ETTimestamp}_{ETIOTCalTestID}.vrs";

                List<string> vrsAllName = new List<string> { "Carpo_EtCalStation", "Carpo_EtCalIlluminator", "Carpo_EtCalBlackbox" };
                foreach (string n in vrsAllName)
                {
                    if (vrs_name.Contains(n))
                    {
                        vrsNameDict[n] = ETDutVrsName;
                    }
                }


                var cameraReplaceParams = ETBuildRecordParameters(duration,vrs_name);
                item.AddLog($"record params ->{cameraReplaceParams}");
                // 启动异步录制任务
                var recordingTask = ETStartRecordingAsync(item, "record_vrs_data", cameraReplaceParams);
                Task.Delay(1000).Wait();
                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"dut statr recode Vrs error: {ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        private dynamic ETBuildRecordParameters(string duration,string vrs_tags_name)
        {
            string dcTypeValue = string.Empty;
            if (jsonConfigData.TryGetValue("vrs_tag_config", out var vrsTagConfig))
            {
                // 检查 vrsTagConfig 是否可以转为 Dictionary<string, object>  
                if (vrsTagConfig is JObject jsonObject)
                {
                    // 使用 JObject 直接获取 dc_type_value  
                    dcTypeValue = (string)jsonObject["dc_type_value"];
                }
                else if (vrsTagConfig is Dictionary<string, object> configDictionary)
                {
                    // 如果是 Dictionary<string, object>，可以使用这个方式  
                    if (configDictionary.TryGetValue("dc_type_value", out var dcTypeValue_))
                    {
                        dcTypeValue = (string)dcTypeValue_;
                    }
                }
            }

            return new {
                vrs_duration = duration,
                tset_id = " ",
                cal_test_id = ETTimestamp,
                operator_id = Project.ProjectDictionary["OPID"],
                iot_cal_test_id = ETIOTCalTestID,
                dc_type = dcTypeValue,
                calibration_type = vrs_tags_name.Split('_').Last().ToLower(),
            };
        }

        private async Task ETStartRecordingAsync(ITestItem item, string command, dynamic parameters)
        {
            await Task.Run(() =>
            {
                try
                {
                    // todo:模拟Python的runCommand调用 感觉这样会有错误，但是目前暂时只能按照AI给的方式
                    var result = ET_cmd_runner.RunCommand(item, command, parameters);

                    // 处理录制结果
                    lock (ET_lock)
                    {
                        ETRecordMark = result.Output.Contains("FINAL STATS");
                    }

                    if (ETRecordMark.HasValue)
                    {
                        item.AddLog((bool)ETRecordMark ? "录制完成" : "未检测到结束标记");
                    }
                }
                catch (Exception ex)
                {
                    item.AddLog($"录制线程异常,{ex}");
                }
            });
        }

        #endregion

        #region Start External camera Recording

        public int ETExternalCameraStartVRS(ITestItem item, string pythonPath, string cameraClientPath,
            bool stability = false, bool notDut = false, int vrsDuration = 45, int timeout = 50000)
        {
            bool result = false;

            try
            {
                item.AddLog($"Start External Camera Recording");
                if (stability)
                    ETExtCamVrsName =
                        $"{ETTestDir}/{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{ETTimestamp}_{ETTimestamp}_ext.vrs";
                else if (notDut)
                    ETExtCamVrsName =
                        $"{ETTestDir}/{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{ETTimestamp}.vrs";
                else
                    ETExtCamVrsName =
                        $"{ETTestDir}/{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{ETTimestamp}_{ETCalTestId}_ext.vrs";

                string command =
                    $"{pythonPath} {cameraClientPath} -m \"roll -o {ETExtCamVrsName} -d {vrsDuration}\"";
                item.AddLog($"recording command: {command}");
                ETExtCamProce = new Process();
                ETExtCamProce.StartInfo.FileName = "cmd.exe";
                ETExtCamProce.StartInfo.Arguments = $"/C {command}";
                ETExtCamProce.StartInfo.RedirectStandardError = true;
                ETExtCamProce.StartInfo.RedirectStandardOutput = true;
                ETExtCamProce.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                ETExtCamProce.StartInfo.CreateNoWindow = true;
                ETExtCamProce.StartInfo.UseShellExecute = false;
                ETExtCamProce.Start();
                item.Sleep(2000);

                result = true;

                //// 记录启动时间
                //DateTime startTime = DateTime.Now;
                //while (!ETExtCamProce.HasExited)
                //{
                //    string stderrOutput = ETExtCamProce.StandardError.ReadLine();
                //    if (stderrOutput != null && stderrOutput.Contains("Creating VRS writer"))
                //    {
                //        Thread.Sleep(2000);
                //        result= true; // 录制成功
                //    }

                //    if ((DateTime.Now - startTime).TotalSeconds > timeout)
                //    {
                //        item.AddLog("Error starting external camera recorder");
                //        result = false; // 录制失败
                //    }
                //}
            }
            catch (Exception ex)
            {
                item.AddLog($"Error occurred when starting to record with an external camera -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        #endregion

        #region Start pull vrs

        public int ETPullVRS(ITestItem item, string vrs_name = "Carpo_EtCalStation",string save_vrs_path = "D:\\vrs_test\\test", int timeoutMs = 50000)
        {
            bool result = false;

            try
            {
                item.AddLog($"Start PULL Vrs");
                int maxAttempts = timeoutMs / 1000 * 2;
                for (int attempts = 0; attempts < maxAttempts; attempts++)
                {
                    item.Sleep(500);
                    if (ETRecordMark == null)
                        continue;
                    else if (ETRecordMark.Value)
                    {
                        item.Sleep(300);
                        ETRecordMark = null;
                        break;
                    }
                    else
                    {
                        item.Sleep(300);
                        ETRecordMark = null;
                        result = false;
                        goto ReturnAndExit;
                    }
                }


                List<string> vrsAllName = new List<string> { "Carpo_EtCalStation", "Carpo_EtCalIlluminator", "Carpo_EtCalBlackbox" };

                if (vrsAllName.Contains(vrs_name))
                {
                    vrs_name = vrsNameDict[vrs_name];
                }
                else
                {
                    item.AddLog("Can't find the target VRS to pull");
                    result = false;
                    goto ReturnAndExit;
                }
                
                Dictionary<string, string> cmdPara = new Dictionary<string, string> {
                    
                    ["save_vrs_path"] = $"{save_vrs_path}/{vrs_name}"
                };

                var pullResult = ET_cmd_runner.RunCommand(item, "adb_vrs_pull", cmdPara, timeout: timeoutMs);
                if (pullResult.Success)
                {
                    item.AddLog($"PASS: vrs_pull--result:{pullResult.Result}");
                    if (pullResult.Result.Contains("No such file or directory"))
                    {
                        result = false;
                        goto ReturnAndExit;
                    }
                    result = true;

                }
                else
                {
                    
                    result = false;
                    item.AddLog($"FAIL:vrs_pull--result:{pullResult.Result}");
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"dut Pull recode Vrs error: {ex}");
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        #endregion

        #region check External camera Recording

        public int ETCheckExtCamVrs(ITestItem item, int timeout = 5000)
        {
            bool result = false;

            try
            {
                item.AddLog($"Start Check External Camera vrs");

                if (ETExtCamProce == null || ETExtCamProce.HasExited)
                {
                    item.AddLog("No active external camera process found.");
                }

                bool exited = ETExtCamProce.WaitForExit(timeout);
                if (exited)
                {
                    item.AddLog("External camera process has exited.");
                }
                else
                {
                    item.AddLog("Timeout waiting for external camera process.");
                    ETExtCamProce.Kill(); // 强制终止进程（可选）
                    ETExtCamProce.WaitForExit(); // 等待进程完全退出
                    ETExtCamProce.Dispose(); // 释放资源
                    ETExtCamProce = null; // 清空引用
                }

                // 检查VRS文件是否生成
                if (File.Exists(ETExtCamVrsName))
                {
                    item.AddLog($"External camera VRS file found: {ETExtCamVrsName}");
                    result = true;
                }
                else
                {
                    item.AddLog("External camera VRS file not found.");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"CheckExtCamVRS_error-> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        #endregion


        #region DisplayCalibration

        public int ETDisplayCalibration(ITestItem item, int timeout = 50000)
        {
            bool result = false;
            try
            {
                var replacePara = new Dictionary<string, string> {
                };

                ET_cmd_runner.RunCommand(item, "adb_display_drive_conditions", timeout: timeout);
                var overallStartTime = DateTime.Now;
                if (jsonConfigData != null && jsonConfigData.TryGetValue("image_names", out var imageNamesObj))
                {
                    var imageNames = (imageNamesObj as JArray)?.ToObject<List<string>>();
                    foreach (var image in imageNames)
                    {
                        var imageStartTime = DateTime.Now;
                        ET_cmd_runner.RunCommand(item, "adb_display_on", timeout: timeout);
                        replacePara["image_path"] = $"{jsonConfigData["dut_image_path"]}/{image}.png";
                        ET_cmd_runner.RunCommand(item, "adb_render_image", replacePara, timeout: timeout);
                        foreach (var color in new[] { "red", "green", "blue" })
                        {
                            SetBrightness(color, replacePara);
                            item.AddLog($"Start displays for image {image}");
                            item.Sleep(100);
                            ET_cmd_runner.RunCommand(item, "adb_set_brightness_left", replacePara, timeout: timeout);
                            ET_cmd_runner.RunCommand(item, "adb_set_brightness_right", replacePara, timeout: timeout);
                            item.AddLog($"begin exposures");

                            var docExposures = ((JArray)jsonConfigData["doc_exposures"]).ToObject<List<int>>();
                            foreach (var exposure in docExposures)
                            {
                                var captureStartTime = DateTime.Now;
                                replacePara["exposure_l"] = exposure.ToString();
                                replacePara["exposure_r"] = exposure.ToString();
                                ET_cmd_runner.RunCommand(item, "lensCroc_set_exposure", replacePara, timeout: timeout);
                                item.AddLog("Capture image");

                                string paddedSec =
                                    ((int)(captureStartTime - overallStartTime).TotalSeconds).ToString("D4");
                                string paddedFracSec =
                                    ((int)((captureStartTime - overallStartTime).TotalMilliseconds % 1000)).ToString(
                                        "D4");

                                replacePara["path_l"] =
                                    $"{ETTestDir}/display/docl/{color}/{image}/display.docl.{color}.{image}.{paddedSec}.{paddedFracSec}s.{exposure:D6}.png";
                                replacePara["path_r"] =
                                    $"{ETTestDir}/display/docr/{color}/{image}/display.docr.{color}.{image}.{paddedSec}.{paddedFracSec}s.{exposure:D6}.png";

                                ET_cmd_runner.RunCommand(item, "lensCroc_snap_image", replacePara, timeout: timeout);

                                if (!File.Exists(replacePara["path_l"]) || !File.Exists(replacePara["path_r"]))
                                {
                                    item.AddLog($"Missing file: {replacePara["path_l"]} or {replacePara["path_r"]}");
                                    ET_cmd_runner.RunCommand(item, "adb_display_off", timeout: timeout);
                                    result = false;
                                    goto ReturnAndExit;
                                }

                                if (!image.Contains("noise") && !image.Contains("flatfield") &&
                                    (new FileInfo(replacePara["path_l"]).Length <
                                     (int)jsonConfigData["image_size_min"] ||
                                     new FileInfo(replacePara["path_r"]).Length <
                                     (int)jsonConfigData["image_size_min"]))
                                {
                                    item.AddLog(
                                        $"File size check failure: {replacePara["path_l"]} or {replacePara["path_r"]}");
                                    ET_cmd_runner.RunCommand(item, "adb_display_off", timeout: timeout);
                                    result = false;
                                    goto ReturnAndExit;
                                }

                                item.AddLog($"Each capture takes: {(DateTime.Now - captureStartTime).TotalSeconds}s");
                            }

                            item.AddLog("Stop display between images");
                        }

                        ET_cmd_runner.RunCommand(item, "adb_display_off", timeout: timeout);
                        item.AddLog($"Each image capture takes: {(DateTime.Now - imageStartTime).TotalSeconds}s");
                    }
                }

                replacePara["exposure_l"] = "5000";
                replacePara["exposure_r"] = "5000";
                ET_cmd_runner.RunCommand(item, "lensCroc_set_exposure_all", replacePara, timeout: timeout);
                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"Error occurred when calibrating the display -> {ex}");
                throw;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        private void ETSetBrightness(string color, Dictionary<string, string> replacePara)
        {
            replacePara["red_brightness"] = color == "red" ? "70" : "0";
            replacePara["green_brightness"] = color == "green" ? "20" : "0";
            replacePara["blue_brightness"] = color == "blue" ? "62" : "0";
        }

        #endregion

        #region genera zip file

        public int ETZIPFile(ITestItem item, string vrsType, string zipName,string vrsPath, int timeout = 50000)
        {
            bool result = false;
            // string zipName = string.Empty;
            try
            {
                string vrsName = vrsNameDict[vrsType];
                string zipDir = Path.Combine("D:\\vrs_zip",Project.SerialNumber,
                    $"{ETCalTestId}_{ETStartTimestamp}");
                Directory.CreateDirectory(zipDir);

                var zipPath = Path.Combine(zipDir, zipName);
                ETZipFilePath = zipPath;
                // Create and populate ZIP files
                using (var zip = ZipFile.Open(zipPath,ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.EnumerateFiles(vrsPath))
                    {
                        var fileName = Path.GetFileName(file);

                        if (fileName.Contains(vrsName) || fileName.EndsWith(".json",StringComparison.OrdinalIgnoreCase))
                        {
                            zip.CreateEntryFromFile(file, fileName);
                        }
                    }
                }

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"zip file error -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        // 计算相对路径方法，兼容.NET Framework
        // uri  可以方便地计算路径差异，同时处理跨平台的路径分隔符问题
        static string ETGetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? basePath
                : basePath + Path.DirectorySeparatorChar);
            Uri fileUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }

        #endregion

        // todo：ET上传文件到算法服务器
        public int ETUploadFileToAlgoServer(ITestItem item, string vrsType, int timeoutSecond = 50)
        {
            bool result = false;
            string bulidConfig = string.Empty;
            string cmdUpload = string.Empty;
            try
            {
                if (uploadBuildConfigNameDict.ContainsKey(vrsType))
                {
                    bulidConfig = uploadBuildConfigNameDict[vrsType];
                }
                else
                {
                    goto ReturnAndExit;
                }

                bool genRes = GenerateUpLoadCmd(item, ETZipFilePath, Project.SerialNumber, bulidConfig,
                    ETStartTimestamp, out cmdUpload);


                var uploadRes = ET_cmd_runner.UploadSubprocess(item, cmdUpload, timeoutSecond: timeoutSecond);
                
                if (uploadRes.res.Contains("job_id") || uploadRes.errRes.Contains("job_id"))
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"UploadFileToAlgoServer -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        /// <summary>
        /// 生成上传文件的url
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="SN"></param>
        /// <param name="buildConfig"></param>
        /// <param name="startTimetamp"></param>
        /// <returns></returns>
        public bool GenerateUpLoadCmd(ITestItem item,string filePath, string sn, string buildConfig, string startTimestamp, out string uploadCmd)
        {
            uploadCmd = string.Empty;
            try
            {
                if (jsonConfigData.TryGetValue("UPLOAD_CMD_ALGO", out var cmdObj) && cmdObj is string cmdTemplate)
                {
                    uploadCmd = string.Format(cmdTemplate, filePath, sn, buildConfig, startTimestamp);
                    return true;
                }
                
                item.AddLog("generate_upload_cmd error: UPLOAD_CMD_ALGO not found or invalid.");
                return false;
                
            }
            catch (Exception e)
            {
                item.AddLog($"generate_upload_cmd error: {e.Message}");
                return false;
            }
        }


        public bool GenerateGetCmd(ITestItem item, string sn, string startTimestamp, out string getCmd)
        {
            getCmd = string.Empty;
            try
            {
                if (jsonConfigData.TryGetValue("GET_RESULT_CMD_ALGO",out var cmdValue) && cmdValue is string cmdTemplate)
                {
                    getCmd = string.Format(cmdTemplate, sn, startTimestamp);
                    return true;
                }
                
                item.AddLog("generate_get_cmd error: GET_RESULT_CMD_ALGO not found or invalid.");
                return false;
                
            }
            catch (Exception e)
            {
                item.AddLog($"generate_get_cmd error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从algoServer中拉取解析结果
        /// </summary>
        /// <param name="item"></param>
        /// <param name="vrsType"></param>
        /// <param name="timeoutSecond"></param>
        /// <returns></returns>
        public int ETGetFileToAlgoServer(ITestItem item, string vrsType, int timeoutSecond = 50)
        {
            bool result = false;
            string bulidConfig = string.Empty;
            string cmdUpload = string.Empty;
            try
            {
                if (uploadBuildConfigNameDict.ContainsKey(vrsType))
                {
                    bulidConfig = uploadBuildConfigNameDict[vrsType];
                }
                else
                {
                    goto ReturnAndExit;
                }

                bool getRes = GenerateGetCmd(item, Project.SerialNumber, ETStartTimestamp, out cmdUpload);

                bool pullSuccess = false;
                string getStringElement = string.Empty;

                for (int i = 1; i <= 5; i++)
                {
                    item.AddLog($"Number of times the loop is pulled-->:{i}");
                    var (res, errres) = ET_cmd_runner.UploadSubprocess(item, cmdUpload, timeoutSecond);
                    if (res.Contains("serial_number"))
                    {
                        pullSuccess = true;
                        getStringElement = res;
                        item.AddLog($"Getting parsing results successfully, number of for loop pulls --->:{i}");
                        break;
                    }
                    else
                    {
                        item.Sleep(2000);
                    }

                }

                if (!pullSuccess)
                {
                    item.AddLog($"Timeout failure for pulling file parsing results");
                    goto ReturnAndExit;
                }

                ETPullCsvPath = Path.Combine((string)jsonConfigData["pull_csv_save_path"],
                    $"{Project.SerialNumber}_{ETCalTestId}.csv");

                try
                {
                    File.WriteAllText(ETPullCsvPath, getStringElement);
                    List<Dictionary<string, string>> listDict = ETReadCsvValues(ETPullCsvPath, "name", "overall_status");
                    foreach (Dictionary<string, string> d in listDict)
                    {
                        if (d.TryGetValue("attribute", out string attributeValue) && attributeValue == "1")
                        {
                            result = true;
                        }
                    }


                }
                catch (Exception e)
                {
                    item.AddLog($"Error while generating CSV file: {e.Message}");
                    goto ReturnAndExit;
                }

            }
            catch (Exception ex)
            {
                item.AddLog($"UploadFileToAlgoServer -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }



        #region Check No Dut result
        public int ETCheckNoDutCalibrationResult(ITestItem item, int timeout = 80000)
        {
            bool result = false;
            string cmdUpload = string.Empty;
            try
            {
                result = ProcessTestAsync(item).GetAwaiter().GetResult();

            }
            catch (Exception ex)
            {
                item.AddLog($"UploadFileToAlgoServer -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public async Task<bool> ETProcessTestAsync(ITestItem item)
        {
            try

            {
                HttpClient _httpClient = null;
                if (_httpClient !=null)
                {
                    _httpClient = new HttpClient();
                }
                
                string pullCsvPath = string.Empty;
                var checkNodutUrl = $"http://172.18.193.172:8088/dtr_search/dstcalstation/NODUT/1/{ETTimestamp}";
               item.AddLog($"check NODUT command: {checkNodutUrl}");

                string responseContent = null;
                bool success = false;

                // 最大重试5次，每次间隔20秒
                for (int i = 1; i <= 5; i++)
                {
                    await Task.Delay(20000); // 等待20秒
                    item.AddLog($"check counts: {i}");

                    try
                    {
                        var response = await _httpClient.GetAsync(checkNodutUrl);
                        responseContent = await response.Content.ReadAsStringAsync();

                        if (responseContent.Contains("serial_number"))
                        {
                            item.AddLog($"成功获取序列号，尝试次数: {i}");
                            success = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        item.AddLog( $"第 {i} 次请求失败,{ex}" );
                    }
                }

                if (!success)
                {
                    item.AddLog("拉取数据超时失败");
                    return false;
                }

                // 生成CSV文件路径
                pullCsvPath = Path.Combine(jsonConfigData["pull_csv_save_path"].ToString(), $"{ETTimestamp}_{ETCalTestId}.csv");
                // 提取目录路径
                var directoryPath = Path.GetDirectoryName(pullCsvPath);
                Directory.CreateDirectory(directoryPath);

                try
                {
                    // 写入CSV文件

                    using (var writer = new StreamWriter(pullCsvPath))
                    {
                        await writer.WriteAsync(responseContent);
                    }


                    // 读取并解析CSV
                    var records = ETReadCsvValues(pullCsvPath, "name", "overall_status");

                    foreach (var record in records)
                    {
                        if (record.TryGetValue("attribute", out var value) && value == "1")
                        {
                            item.AddLog("校准结果: PASS");
                            return true;
                        }
                    }

                    item.AddLog("校准结果: FAIL");
                    return false;
                }
                catch (Exception ex)
                {
                    item.AddLog( $"CSV文件处理失败{ex}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"服务器处理异常{ex}");
                return false;
            }
        }
        
        private List<Dictionary<string, string>> ETReadCsvValues(string path, string header, string key)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                HasHeaderRecord = true,
                MissingFieldFound = null // 忽略缺失的字段
            };

            var records = new List<Dictionary<string, string>>();

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var record = new Dictionary<string, string>();
                    foreach (var headerName in csv.HeaderRecord)
                    {
                        record[headerName] = csv.GetField(headerName);
                    }

                    // 筛选符合条件的记录
                    if (record.TryGetValue(header, out var value) && value == key)
                    {
                        records.Add(record);
                    }
                }
            }

            return records;
        }
        #endregion



        public int ETTimeWaitSeconds(ITestItem item, int timeSleepSecond)
        {
            int timeSleepMs = timeSleepSecond * 1000;
            item.Sleep(timeSleepMs);
            return 0;
        }


        public int ETDUTPullJson(ITestItem item, int timeout = 8000)
        {
            bool result = false;
            string pullJsonPathTotal = "D:\\vrs_test\\test";
            try
            {

                Directory.CreateDirectory(pullJsonPathTotal);
                var cmdResult = ET_cmd_runner.RunCommand(item, "adb_pull_json",  timeout: timeout);
                if (!cmdResult.Success)
                {
                    result = false;
                    item.AddLog($"command run failed");
                    goto ReturnAndExit;
                }

                string FilePath = "D:\\vrs_test\\test\\camera_calibration.json";
                // 读取 JSON 文件并解析内容
                if (!File.Exists(FilePath))
                {
                    item.AddLog("pull fail;file not exist");
                   goto ReturnAndExit;
                }

                string jsonData = File.ReadAllText(FilePath);
                JObject tempJObject = JsonConvert.DeserializeObject<JObject>(jsonData);
                ETIOTCalTestID = tempJObject?["Metadata"]?["NamedTags"]?["cal_test_id"]?.ToString();
                if (string.IsNullOrEmpty(ETIOTCalTestID))
                {
                    ETIOTCalTestID = "123123123";
                }
                

                item.AddLog($"jsonData->{jsonData}");
                iotcalTestIdDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                if ((iotcalTestIdDict != null && iotcalTestIdDict.Count > 0))
                {
                    item.AddLog("iotcalTestIdDict : " + JsonConvert.SerializeObject(iotcalTestIdDict, Formatting.Indented));
                    result = true;
                }


            }
            catch (Exception ex)
            {
                item.AddLog($"DUTPullJson have error  -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        // DUT Push json
        public int ETDUTPushJson(ITestItem item,string pushJsonPath, int timeout = 8000)
        {
            bool result = false;
            
            try
            {
                Dictionary<string, string> jsonDict = new Dictionary<string, string>() {
                    ["SN"] = Project.SerialNumber,
                    ["cal_test_id"] = ETStartTimestamp,
                    ["station_number"] = "1"
                };

                Directory.CreateDirectory(pushJsonPath);
                ETPushJsonPathName = Path.Combine(pushJsonPath, "etcal_usecase.json");

                using (StreamWriter swStreamWriter = new StreamWriter(ETPushJsonPathName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(swStreamWriter,jsonDict);
                }

                Dictionary<string, string> cmdPara = new Dictionary<string, string>() {
                    ["json_name"] = ETPushJsonPathName
                };

                var cmdResult = ET_cmd_runner.RunCommand(item, "adb_push_file",cmdPara, timeout: timeout);
                if (!cmdResult.Success)
                {
                    result = false;
                    item.AddLog($"Pushing json file to product fails");
                    goto ReturnAndExit;
                }
                else
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"DUT Push Json generate error  -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        // ET SET DUT LED
        public int ETSetLeds(ITestItem item, int timeout = 5000,string ledTime = "5",string brightness = "0")
        {
            bool result = false;
            
            try
            {
                var cmdPara = new Dictionary<string, string>() {
                    ["led_time"] = ledTime,
                    ["brightness"] = brightness

                };
                var cmdResult = ET_cmd_runner.RunCommand(item, "adb_syncboss_led", cmdPara,timeout: timeout);

                item.AddLog($"TestActionETLeds--result:{cmdResult.Success}");
                if (cmdResult.Success)
                {
                    result = true;
                }

            }
            catch (Exception ex)
            {
                item.AddLog($"TestActionETLeds have error  -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int ETFinishAndPullLog(ITestItem item, int timeout = 8000)
        {
            bool result = false;

            try
            {
                var cmdResult = ET_cmd_runner.RunCommand(item, "adb_exit_station", timeout: timeout);
                if (!cmdResult.Success)
                {
                    goto ReturnAndExit;
                }


                var cmdpara_ = new Dictionary<string, string>() { ["destination"] = ETTestDir };
                var cmdResult_1 = ET_cmd_runner.RunCommand(item, "adb_pull_log_files", cmdpara_, timeout: timeout);
                if (!cmdResult_1.Success)
                {
                    goto ReturnAndExit;
                }

                var cmdResult_2 = ET_cmd_runner.RunCommand(item, "adb_remove_log_files", timeout: timeout);
                if (!cmdResult_2.Success)
                {
                    goto ReturnAndExit;
                }


                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog($"TestActionFinishAndPullLog-error -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }
    }
}