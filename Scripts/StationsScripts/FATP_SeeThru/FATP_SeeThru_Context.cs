using GTKWebServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using GTKWebServices.GTKWebServices.SMT;
using Test._Definitions;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Test._ScriptExtensions;
using NLog;
using UserHelpers.Helpers;


namespace Test.StationsScripts.FATP_SeeThru
{

    public class SeeThru_Context
    {

        public SeeThru_Context()
        {
            Reset();
        }


        public void Reset()
        {

        }

        public void ClearUp()
        {

        }

        public void Dispose()
        {
            Reset();
        }

    }



    public class TestIdUpdater
    {
        private readonly object lockObj = new object();
        private string testIdJson = "test_id.json";


        public TestIdUpdater(string testIdPath)
        {
            this.testIdJson = testIdPath;

        }

        public string UpdateTestId(bool isOnMes)
        {
            lock (lockObj)
            {
                // 读取json文件
                Dictionary<string, int> lastTestId = new Dictionary<string, int>();
                if (File.Exists(testIdJson))
                {
                    var jsonData = File.ReadAllText(testIdJson);
                    lastTestId = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonData);
                }

                int testId;
                if (isOnMes)
                {
                    testId = lastTestId.ContainsKey("mes_last_test_id") ? lastTestId["mes_last_test_id"] + 1 : 1;
                    lastTestId["mes_last_test_id"] = testId;
                }
                else
                {
                    testId = lastTestId.ContainsKey("last_test_id") ? lastTestId["last_test_id"] + 1 : 1;
                    lastTestId["last_test_id"] = testId;
                }

                string dir = Path.GetDirectoryName(testIdJson);
                if (!string.IsNullOrEmpty(dir) && dir.Length > 2)
                {
                    Directory.CreateDirectory(dir);
                }

                // 写回Json文件
                var jsonDataToWrite = JsonConvert.SerializeObject(lastTestId, Formatting.Indented);
                File.WriteAllText(testIdJson, jsonDataToWrite);

                return testId.ToString();


            }
        }

    }


    public class AdbCommandRunner
    {
        private Dictionary<string, string> _adbCommand;
        public string AdbToolPath { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        public AdbCommandRunner(Dictionary<string, string> adbCommand, string adbToolPath)
        {
            _adbCommand = adbCommand;
            AdbToolPath = adbToolPath;
        }

        /// <summary>
        /// 执行ADB命令并返回结果
        /// </summary>
        /// <param name="command">命令名称</param>
        /// <param name="replacePara">参数替换字典</param>
        /// <param name="delimiter">终止分隔符</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        /// <returns>执行结果及输出</returns>
        public (bool Success, string Result) RunCommand(ITestItem item, string command, Dictionary<string, string> replacePara = null,
            string delimiter = "delimiterplacehold89", int? timeout = null)
        {
            // 1. 查找命令模板
            if (!_adbCommand.TryGetValue(command, out var cmdTemplates))
            {
                Logger.Info("Can't find command");
                item.AddLog("Can't find command");
                return (false, string.Empty);
            }

            var cmdStr = cmdTemplates;
            var replaceCmd = Regex.Matches(cmdStr, @"\[.*?\]");

            // 2. 参数替换逻辑
            if (replaceCmd.Count > 0 && replacePara?.Count > 0)
            {
                foreach (Match match in replaceCmd)
                    if (replacePara.TryGetValue(match.Value.Trim('[', ']'), out var value))
                        cmdStr = cmdStr.Replace(match.Value, value);
            }

            item.AddLog($"CMD: {cmdStr}");
            Logger.Info($"CMD: {cmdStr}");

            // 3. 执行ADB命令
            var (res, errRes) = AdbCmd(cmdStr, timeout ?? 800_00, delimiter);

            // 4. 处理结果
            var resLines = res.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var errLines = errRes.Split(new[] { "\r\n" }, StringSplitOptions.None);

            if (errLines.Length > 1)
            {
                item.AddLog($"Execute {command} FAILED. Error: {errRes}, Result: {res}");
                Logger.Warn($"Execute {command} FAILED. Error: {errRes}, Result: {res}");
                return (false, errRes);
            }

            Logger.Info($"CMD:{cmdStr} --> {res}");
            item.AddLog($"CMD:{cmdStr} --> {res}");
            return (true, res);
        }

        /// <summary>
        /// 执行ADB命令核心方法
        /// </summary>
        private (string Result, string Error) AdbCmd(string adbShell, int timeoutMs, string delimiter)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{Path.Combine(AdbToolPath, adbShell)}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var output = new StringBuilder();
                var error = new StringBuilder();
                var outputCloseEvent = new ManualResetEvent(false);
                var errorCloseEvent = new ManualResetEvent(false);
                var delimiterFound = false;

                // 处理标准输出
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputCloseEvent.Set();
                        return;
                    }

                    output.AppendLine(e.Data);
                    if (!delimiterFound && e.Data.Contains(delimiter))
                    {
                        delimiterFound = true;
                        process.Kill(); // 发现终止符后立即终止进程
                    }
                };

                // 处理标准错误
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        error.AppendLine(e.Data);
                    else
                        errorCloseEvent.Set();
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 等待进程退出或超时
                    if (!process.WaitForExit(timeoutMs))
                    {
                        process.Kill();
                        error.AppendLine("Timeout reached! Exiting the loop.");
                    }

                    // 等待流关闭
                    outputCloseEvent.WaitOne(1000);
                    errorCloseEvent.WaitOne(1000);
                }
                catch (Exception ex)
                {
                    error.AppendLine($"Process error: {ex.Message}");
                }

                return (output.ToString(), error.ToString());
            }
        }



        /// <summary>
        /// 执行上传命令的子进程，并返回标准输出和错误输出结果。
        /// </summary>
        /// <param name="uploadCmd">要执行的上传命令字符串</param>
        /// <param name="timeout">超时时间（秒），默认为50秒</param>
        /// <returns>包含标准输出和错误输出的元组</returns>
        /// <exception cref="TimeoutException">当命令执行超时时抛出</exception>
        public (string res, string errRes) UploadSubprocess(ITestItem item, string upLoadCmd, int timeoutSecond = 50)
        {
            string res = string.Empty;
            string errRes = string.Empty;
            using (var process = new Process())
            {
                // 配置进程启动信息
                process.StartInfo = new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/C {upLoadCmd}",                 // "/c" 表示执行后终止cmd
                    RedirectStandardOutput = true,                 // 重定向标准输出
                    RedirectStandardError = true,                  // 重定向错误输出
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,        // 设置编码格式为UTF-8
                    CreateNoWindow = true,                          // 不创建新窗口
                    UseShellExecute = false,                       // 禁用Shell执行以重定向流
                };
                process.Start();

                // 异步读取输出流
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 等待进程退出或超时
                bool exited = process.WaitForExit(timeoutSecond * 1000);
                if (!exited)
                {
                    process.Kill();
                    process.WaitForExit();
                    throw new TimeoutException($"Command timed out after {timeoutSecond} seconds");
                }
                // 确保异步操作完成
                Task.WaitAll(outputTask, errorTask);
                res = outputTask.GetAwaiter().GetResult();
                errRes = errorTask.GetAwaiter().GetResult();

            }


            item.AddLog($"Cmd_: {upLoadCmd}");
            item.AddLog($"res_: {res}");
            item.AddLog($"errRes_: {errRes}");
            return (res, errRes);
        }








        //public static string sequenceImu = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence.json";
        //public static string sequenceLED = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence2.json";
        //public static string sequenceCal = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence_cal.json";

    }
}

