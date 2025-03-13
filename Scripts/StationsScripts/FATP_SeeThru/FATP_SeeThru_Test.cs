using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.StationsScripts.FATP_SeeThru;
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
using static TagAccessCS.TagAccessClass;
using System.Runtime.Remoting.Contexts;
using System.Globalization;
using Test.StationsScripts.Shared;
using System.IO.Compression;
using System.Security.RightsManagement;


namespace Test
{
    public partial class MainClass
    {
        private static readonly object SeeThrufilelock = new object();
        public string test_id_;
        private readonly object _lock = new object();

        public Dictionary<string, object> savaDataDictSeeThru = new Dictionary<string, object>();
        public AdbCommandRunner cmd_runner = null;


        public string SeeThruTestDir = string.Empty;
        public string SeeThruTimestamp = string.Empty;
        public string SeeThruCalTestId = string.Empty;
        public string SeeThruDutVrsName = string.Empty;
        public string SeeThruExtCamVrsName = string.Empty;
        public Process SeeThruExtCamProce = null;
        public string SeeThruZipFilePath = string.Empty;
        public string SeeThruPullJonsPathName = string.Empty;
        public bool? SeeThruRecordMark = null;


        // 给线程使用
        private volatile bool Seethru_stopRequested;


        // 发给cam服务器的存图地址
        //public string camImagePath = $"C:\\SuperCalRecord\\{DateTime.Now.ToString("yyyy_MM_dd")}";
        //public string totalPath = string.Empty;


        // 获取OPID
        public int GetOpID(ITestItem item)
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
        public int GenerateAdbCommandRunnerinstance(ITestItem item, string adbPath)
        {
            bool result = false;
            try
            {
                var adbCommand =
                    jsonCmdData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
                item.AddLog($"cmd: {adbCommand}");
                foreach (KeyValuePair<string, string> kvp in adbCommand)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;
                    item.AddLog($"key:{key},value:{value}");
                }


                if (cmd_runner == null)
                {
                    cmd_runner = new AdbCommandRunner(adbCommand, adbPath);
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


        // 为SeeThru的检查校准文件是否超时
        public int SeeThruCheckNoDutTime(ITestItem item, int testInterval, string checkCmd)
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
        public int SeeThruReadSnAndOtherCommand(ITestItem item, int timeout_ = 10000)
        {
            bool result = false;
            string SN = string.Empty;
            string command = string.Empty;
            string read = string.Empty;

            List<string> othreadCmd = new List<string> {
                "adb_devices", "adb_reboot", "adb_wait_for_device", "adb_root",
                "adb_remount", "adb_remove_log_files", "adb_enter_station", "adb_get_serialno",
                "adb_get_sn", "adb_enable_manual_exposure", "adb_enable_manual_gain", "adb_et_led_off",
                "adb_restart_tracking", "adb_display_off", "adb_check_input_voltage", "adb_get_battery",
                "adb_get_temps"
            };
            try
            {
                foreach (var key in othreadCmd)
                {
                    // 调用修改后的RunCommand方法
                    var (success, res) = cmd_runner.RunCommand(item, key, timeout: timeout_);
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
        public int CreateFolderStructure(ITestItem item, bool isNotDUt = false, bool Stability = false)
        {
            bool result = false;

            try
            {
                SeeThruTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                if (isNotDUt)
                {
                    Project.SerialNumber = "NODUT";
                }

                SeeThruTestDir = Path.Combine((string)jsonConfigData["output_path"],
                    $"{Project.SerialNumber}_{SeeThruTimestamp}");
                var _tempWorkingDir = SeeThruTestDir;

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
                                    SeeThruTestDir,
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

        public int SeeThruReadIAD(ITestItem item, int timeout = 10000)
        {
            bool result = false;
            bool readIAD = false;
            bool readIAD2 = false;
            float? distance1 = null;
            float? distance2 = null;
            try
            {
                var (success1, result1Str) = cmd_runner.RunCommand(item, "adb_shell_IAD", timeout: timeout);
                if (!success1)
                {
                    item.AddLog($"run adb shell_IAD ERROR: {result1Str}");
                    goto ReturnAndExit;
                }

                distance1 = ParseDistance(result1Str, lineIndex: 4); // 对应Python的ret_1[1][4]
                readIAD = IsValidDistance(distance1);

                // 执行第二个命令并解析结果
                var (success2, result2Str) = cmd_runner.RunCommand(item, "adb_shell_IAD_meters", timeout: timeout);
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
        private float? ParseDistance(string commandResult, int lineIndex)
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
        private bool IsValidDistance(float? distance)
        {
            const float min = 0.063f;
            const float max = 0.0651f;
            const float tolerance = 0.00001f; // 浮点数比较容差

            return distance.HasValue &&
                   (distance.Value > min - tolerance) &&
                   (distance.Value < max + tolerance);
        }

        #endregion

        public int PullIOTCalibrationFiles(ITestItem item, int timeout = 10000)
        {
            bool result = false;

            string read = string.Empty;

            try
            {
                // 拉取IMU校准文件
                var imuReplaceParams = new Dictionary<string, string> {
                    ["file_to_pull"] = "/persist/calibration/imu_calibration.json",
                    ["output_path"] = Path.Combine(SeeThruTestDir, "imu_calibration.json")
                };
                var (imuSuccess, imuResult) =
                    cmd_runner.RunCommand(item, "adb_pull", imuReplaceParams, timeout: timeout);
                if (!imuSuccess)
                {
                    item.AddLog($"pull Calibration Files error: {imuResult}");
                    goto ReturnAndExit;
                }

                var cameraReplaceParams = new Dictionary<string, string> {
                    ["file_to_pull"] = "/persist/calibration/camera_calibration.json",
                    ["output_path"] = Path.Combine(SeeThruTestDir, "camera_calibration.json")
                };

                var (cameraSuccess, cameraResult) =
                    cmd_runner.RunCommand(item, "adb_pull", cameraReplaceParams, timeout: timeout);
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
                    SeeThruCalTestId = iotCalibration?["Metadata"]?["NamedTags"]?["cal_test_id"]?.Value<string>();
                    if (string.IsNullOrWhiteSpace(SeeThruCalTestId))
                    {
                        item.AddLog("cal_test_id值为空或不存在");
                        goto ReturnAndExit;
                    }

                    item.AddLog($"成功获取IOT cal_test_id: {SeeThruCalTestId}");
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

        public int StartVRS(ITestItem item, int timeout = 50000, int duration = 45)
        {
            bool result = false;

            try
            {
                item.AddLog($"dut statr recode Vrs");
                SeeThruDutVrsName =
                    $"{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{SeeThruTimestamp}_{SeeThruCalTestId}_dut.vrs";

                var cameraReplaceParams = BuildRecordParameters(duration);
                item.AddLog($"record params ->{cameraReplaceParams}");
                // 启动异步录制任务
                var recordingTask = StartRecordingAsync(item, "record_vrs_data", cameraReplaceParams);
                Task.Delay(2000).Wait();
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


        private dynamic BuildRecordParameters(int duration)
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
                vrs_name = SeeThruDutVrsName,
                vrs_duration = duration,
                station_type_name_value = "dstcal",
                dc_type_value = dcTypeValue,
                dc_id_value = SeeThruCalTestId,
                cal_test_id_value = SeeThruTimestamp,
                iot_cal_test_id_value = SeeThruCalTestId,
                operator_id_value = Project.ProjectDictionary["OPID"]
            };
        }

        private async Task StartRecordingAsync(ITestItem item, string command, dynamic parameters)
        {
            await Task.Run(() =>
            {
                try
                {
                    // todo:模拟Python的runCommand调用 感觉这样会有错误，但是目前暂时只能按照AI给的方式
                    var result = cmd_runner.RunCommand(item, command, parameters);

                    // 处理录制结果
                    lock (_lock)
                    {
                        SeeThruRecordMark = result.Output.Contains("FINAL STATS");
                    }

                    if (SeeThruRecordMark.HasValue)
                    {
                        item.AddLog((bool)SeeThruRecordMark ? "录制完成" : "未检测到结束标记");
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

        public int ExternalCameraStartVRS(ITestItem item, string pythonPath, string cameraClientPath,
            bool stability = false, bool notDut = false, int vrsDuration = 45, int timeout = 50000)
        {
            bool result = false;

            try
            {
                item.AddLog($"Start External Camera Recording");
                if (stability)
                    SeeThruExtCamVrsName =
                        $"{SeeThruTestDir}/{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{SeeThruTimestamp}_{SeeThruTimestamp}_ext.vrs";
                else if (notDut)
                    SeeThruExtCamVrsName =
                        $"{SeeThruTestDir}/{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{SeeThruTimestamp}.vrs";
                else
                    SeeThruExtCamVrsName =
                        $"{SeeThruTestDir}/{jsonConfigData["vrs_name_prefix"]}_{Project.SerialNumber}_{SeeThruTimestamp}_{SeeThruCalTestId}_ext.vrs";

                string command =
                    $"{pythonPath} {cameraClientPath} -m \"roll -o {SeeThruExtCamVrsName} -d {vrsDuration}\"";
                item.AddLog($"recording command: {command}");
                SeeThruExtCamProce = new Process();
                SeeThruExtCamProce.StartInfo.FileName = "cmd.exe";
                SeeThruExtCamProce.StartInfo.Arguments = $"/C {command}";
                SeeThruExtCamProce.StartInfo.RedirectStandardError = true;
                SeeThruExtCamProce.StartInfo.RedirectStandardOutput = true;
                SeeThruExtCamProce.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                SeeThruExtCamProce.StartInfo.CreateNoWindow = true;
                SeeThruExtCamProce.StartInfo.UseShellExecute = false;
                SeeThruExtCamProce.Start();
                item.Sleep(2000);

                result = true;

                //// 记录启动时间
                //DateTime startTime = DateTime.Now;
                //while (!SeeThruExtCamProce.HasExited)
                //{
                //    string stderrOutput = SeeThruExtCamProce.StandardError.ReadLine();
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

        public int PullVRS(ITestItem item, int timeoutMs = 50000)
        {
            bool result = false;

            try
            {
                item.AddLog($"Start PULL Vrs");
                int maxAttempts = timeoutMs / 1000 * 2;
                for (int attempts = 0; attempts < maxAttempts; attempts++)
                {
                    item.Sleep(500);
                    if (SeeThruRecordMark == null)
                        continue;
                    else if (SeeThruRecordMark.Value)
                    {
                        item.Sleep(300);
                        SeeThruRecordMark = null;
                        break;
                    }
                    else
                    {
                        item.Sleep(300);
                        SeeThruRecordMark = null;
                        result = false;
                        goto ReturnAndExit;
                    }
                }

                Dictionary<string, string> cmdPara = new Dictionary<string, string> {
                    ["vrs_name"] = SeeThruDutVrsName,
                    ["save_vrs_path"] = $"{SeeThruTestDir}/{SeeThruDutVrsName}"
                };

                var pullResult = cmd_runner.RunCommand(item, "adb_vrs_pull", cmdPara, timeout: timeoutMs);
                if (pullResult.Success)
                {
                    item.AddLog($"PASS: vrs_pull--result:{pullResult.Result}");

                    var cmdParaMove = new Dictionary<string, string>() {
                        ["file_path_to_remove"] = $"/data/{SeeThruDutVrsName}"
                    };
                    cmd_runner.RunCommand(item, "adb_remove_file", cmdParaMove, timeout: 5000);
                    result = true;
                }
                else
                {
                    var cmdParaMove = new Dictionary<string, string>() {
                        ["file_path_to_remove"] = $"/data/{SeeThruDutVrsName}"
                    };
                    cmd_runner.RunCommand(item, "adb_remove_file", cmdParaMove, timeout: 5000);
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

        public int CheckExtCamVrs(ITestItem item, int timeout = 5000)
        {
            bool result = false;

            try
            {
                item.AddLog($"Start Check External Camera vrs");

                if (SeeThruExtCamProce == null || SeeThruExtCamProce.HasExited)
                {
                    item.AddLog("No active external camera process found.");
                }

                bool exited = SeeThruExtCamProce.WaitForExit(timeout);
                if (exited)
                {
                    item.AddLog("External camera process has exited.");
                }
                else
                {
                    item.AddLog("Timeout waiting for external camera process.");
                    SeeThruExtCamProce.Kill(); // 强制终止进程（可选）
                    SeeThruExtCamProce.WaitForExit(); // 等待进程完全退出
                    SeeThruExtCamProce.Dispose(); // 释放资源
                    SeeThruExtCamProce = null; // 清空引用
                }

                // 检查VRS文件是否生成
                if (File.Exists(SeeThruExtCamVrsName))
                {
                    item.AddLog($"External camera VRS file found: {SeeThruExtCamVrsName}");
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

        public int DisplayCalibration(ITestItem item, int timeout = 50000)
        {
            bool result = false;
            try
            {
                var replacePara = new Dictionary<string, string> {
                };

                cmd_runner.RunCommand(item, "adb_display_drive_conditions", timeout: timeout);
                var overallStartTime = DateTime.Now;
                if (jsonConfigData != null && jsonConfigData.TryGetValue("image_names", out var imageNamesObj))
                {
                    var imageNames = (imageNamesObj as JArray)?.ToObject<List<string>>();
                    foreach (var image in imageNames)
                    {
                        var imageStartTime = DateTime.Now;
                        cmd_runner.RunCommand(item, "adb_display_on", timeout: timeout);
                        replacePara["image_path"] = $"{jsonConfigData["dut_image_path"]}/{image}.png";
                        cmd_runner.RunCommand(item, "adb_render_image", replacePara, timeout: timeout);
                        foreach (var color in new[] { "red", "green", "blue" })
                        {
                            SetBrightness(color, replacePara);
                            item.AddLog($"Start displays for image {image}");
                            item.Sleep(100);
                            cmd_runner.RunCommand(item, "adb_set_brightness_left", replacePara, timeout: timeout);
                            cmd_runner.RunCommand(item, "adb_set_brightness_right", replacePara, timeout: timeout);
                            item.AddLog($"begin exposures");

                            var docExposures = ((JArray)jsonConfigData["doc_exposures"]).ToObject<List<int>>();
                            foreach (var exposure in docExposures)
                            {
                                var captureStartTime = DateTime.Now;
                                replacePara["exposure_l"] = exposure.ToString();
                                replacePara["exposure_r"] = exposure.ToString();
                                cmd_runner.RunCommand(item, "lensCroc_set_exposure", replacePara, timeout: timeout);
                                item.AddLog("Capture image");

                                string paddedSec =
                                    ((int)(captureStartTime - overallStartTime).TotalSeconds).ToString("D4");
                                string paddedFracSec =
                                    ((int)((captureStartTime - overallStartTime).TotalMilliseconds % 1000)).ToString(
                                        "D4");

                                replacePara["path_l"] =
                                    $"{SeeThruTestDir}/display/docl/{color}/{image}/display.docl.{color}.{image}.{paddedSec}.{paddedFracSec}s.{exposure:D6}.png";
                                replacePara["path_r"] =
                                    $"{SeeThruTestDir}/display/docr/{color}/{image}/display.docr.{color}.{image}.{paddedSec}.{paddedFracSec}s.{exposure:D6}.png";

                                cmd_runner.RunCommand(item, "lensCroc_snap_image", replacePara, timeout: timeout);

                                if (!File.Exists(replacePara["path_l"]) || !File.Exists(replacePara["path_r"]))
                                {
                                    item.AddLog($"Missing file: {replacePara["path_l"]} or {replacePara["path_r"]}");
                                    cmd_runner.RunCommand(item, "adb_display_off", timeout: timeout);
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
                                    cmd_runner.RunCommand(item, "adb_display_off", timeout: timeout);
                                    result = false;
                                    goto ReturnAndExit;
                                }

                                item.AddLog($"Each capture takes: {(DateTime.Now - captureStartTime).TotalSeconds}s");
                            }

                            item.AddLog("Stop display between images");
                        }

                        cmd_runner.RunCommand(item, "adb_display_off", timeout: timeout);
                        item.AddLog($"Each image capture takes: {(DateTime.Now - imageStartTime).TotalSeconds}s");
                    }
                }

                replacePara["exposure_l"] = "5000";
                replacePara["exposure_r"] = "5000";
                cmd_runner.RunCommand(item, "lensCroc_set_exposure_all", replacePara, timeout: timeout);
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

        private void SetBrightness(string color, Dictionary<string, string> replacePara)
        {
            replacePara["red_brightness"] = color == "red" ? "70" : "0";
            replacePara["green_brightness"] = color == "green" ? "20" : "0";
            replacePara["blue_brightness"] = color == "blue" ? "62" : "0";
        }

        #endregion

        #region genera zip file

        public int ZIPFile(ITestItem item, bool notDut = false, int timeout = 50000)
        {
            bool result = false;
            string zipName = string.Empty;
            try
            {
                if (notDut)
                    zipName = "display_nodut.zip";
                else
                    zipName = "diaplay.zip";
                string zipPath = (string)jsonConfigData["output_path"];
                SeeThruZipFilePath = Path.Combine(zipPath, zipName);
                if (File.Exists(SeeThruZipFilePath))
                    File.Delete(SeeThruZipFilePath);

                // 创建文件流
                using (FileStream zipToOpen = new FileStream(SeeThruZipFilePath, FileMode.Create))
                {
                    // 创建ZIP压缩对象 -ZipArchive 是压缩逻辑的核心类。
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        // 遍历目录下的所有文件 第一个参数是目标目录， 第二个参数’*‘ 匹配所有文件
                        foreach (string file in Directory.GetFiles(SeeThruTestDir, "*", SearchOption.AllDirectories))
                        {
                            string entryName = GetRelativePath(SeeThruTestDir, file);
                            // 创建ZIP中的条目（文件）
                            ZipArchiveEntry entry = archive.CreateEntry(entryName);

                            // 将源文件内容写入ZIP条目
                            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                            using (Stream entryStream = entry.Open())
                            {
                                fileStream.CopyTo(entryStream);
                            }
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
        static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? basePath
                : basePath + Path.DirectorySeparatorChar);
            Uri fileUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }

        #endregion


        public int UploadFileToAlgoServer(ITestItem item, bool notDut = false, bool stability = false,
            int timeout = 80000)
        {
            bool result = false;
            string binaryType = string.Empty;
            string cmdUpload = string.Empty;
            try
            {
                if (stability)
                    binaryType = "carpo_p1_dstcal";
                else if (notDut)
                    binaryType = "carpo_p1_dstcalstation";
                else
                    binaryType = "carpo_p1_dstcal";

                cmdUpload =
                    $"curl -T {SeeThruZipFilePath} -X POST 172.18.193.172:8080/gendstcal/{Project.SerialNumber}/{binaryType}/1/{SeeThruTimestamp}/0/1";
                item.AddLog($"upload cmd: {cmdUpload}");
                var uploadRes = cmd_runner.UploadSubprocess(item, cmdUpload, timeoutSecond: timeout);
                if (uploadRes.res.Contains("job_id"))
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


        public int DUTPushJson(ITestItem item, string pullJsonPath, int timeout = 80000)
        {
            bool result = false;
            string pullJsonPathTotal = string.Empty;
            try
            {
                Dictionary<string, string> jsonDict = new Dictionary<string, string>() {
                    ["SN"] = Project.SerialNumber,
                    ["cal_test_id"] = SeeThruTimestamp,
                    ["station_number"] = "1"
                };
                pullJsonPathTotal = Path.Combine(pullJsonPath, $"{Project.SerialNumber}_{SeeThruCalTestId}");
                Directory.CreateDirectory(pullJsonPathTotal);

                SeeThruPullJonsPathName = Path.Combine(pullJsonPathTotal, "dstcal_usecase.json");

                item.AddLog($"SeeThruPullJonsPathName->{SeeThruPullJonsPathName}");

                File.WriteAllText(SeeThruPullJonsPathName,
                    JsonConvert.SerializeObject(jsonDict, Formatting.Indented));

                Dictionary<string, string> cmdPara = new Dictionary<string, string>() {
                    ["file_to_push"] = SeeThruPullJonsPathName,
                    ["destination"] = "/data/dstcal_usecase.json"
                };
                item.AddLog($"cmdpara->{cmdPara}");

                var cmdResult = cmd_runner.RunCommand(item, "adb_push", cmdPara, timeout: timeout);
                if (cmdResult.Success)
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                item.AddLog($"DUTPushJson have error  -> {ex}");
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int FinishAndPullLog(ITestItem item, int timeout = 8000)
        {
            bool result = false;

            try
            {
                var cmdResult = cmd_runner.RunCommand(item, "adb_exit_station", timeout: timeout);
                if (!cmdResult.Success)
                {
                    goto ReturnAndExit;
                }


                var cmdpara_ = new Dictionary<string, string>() { ["destination"] = SeeThruTestDir };
                var cmdResult_1 = cmd_runner.RunCommand(item, "adb_pull_log_files", cmdpara_, timeout: timeout);
                if (!cmdResult_1.Success)
                {
                    goto ReturnAndExit;
                }

                var cmdResult_2 = cmd_runner.RunCommand(item, "adb_remove_log_files", timeout: timeout);
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