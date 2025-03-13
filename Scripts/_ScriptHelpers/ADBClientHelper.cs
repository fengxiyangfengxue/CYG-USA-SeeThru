using MetaHelpers.ScriptHelpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Test._ScriptExtensions;

namespace Test._ScriptHelpers
{
    public class ADBClientHelper //:ADBCallerHelper
    {
        public CRunExe client = null;
        public string ADBSerialNumber { get; set; }
        public string ADBExeName { get; set; }

        public ADBClientHelper()
        {
            ADBExeName = "adb.exe";
            ADBSerialNumber = string.Empty;
        }
        public ADBClientHelper(string adbSerialNumber) : this()
        {
            ADBSerialNumber = adbSerialNumber;
        }

        public ADBClientHelper(string exeName, string adbSerialNumber)
        {
            ADBExeName = exeName;
            ADBSerialNumber = adbSerialNumber;
        }

        private string ADBSN(string args)
        {
            if (!string.IsNullOrEmpty(ADBSerialNumber))
                return $"-s {ADBSerialNumber} {args}";
            return args;
        }


        #region check adb recive
        public bool CheckPullRcv(string rcv)
        {
            return rcv.Contains("B/s") && rcv.Contains("pulled") && !rcv.Contains("error") && !rcv.Contains("failed") && !rcv.Contains("no devices") && !rcv.Contains("not found") && !rcv.Contains("Permission denied");
        }

        public bool CheckRunCmdRcv(string rcv)
        {
            return !rcv.Contains("OutTime") && !rcv.Contains("not found") && !rcv.Contains("no devices") && !rcv.ToLower().Contains("error") && !rcv.ToLower().Contains("fail") && !rcv.Contains("Permission denied") && !rcv.Contains("No such file or directory");
        }

        public bool CheckPushRcv(string rcv)
        {
            return rcv.Contains("B/s") && rcv.Contains("pushed") && !rcv.Contains("error") && !rcv.Contains("failed") && !rcv.Contains("no devices") && !rcv.Contains("not found") && !rcv.Contains("Permission denied");
        }
        #endregion

        #region 重写父类方法，增加Log输出
        public bool RunHide(Action<string> logger, string args)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, int.MaxValue, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, string workingDirectory)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, int.MaxValue, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHide(Action<string> logger, string args, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, int timeOutMilliSeconds, int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunHideRead(Action<string> logger, string args, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunShow(Action<string> logger, string args)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, int.MaxValue, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, string workingDirectory)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, int.MaxValue, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShow(Action<string> logger, string args, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;

        }

        public bool RunShowRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;

        }

        public bool RunShowRead(Action<string> logger, string args, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;

        }

        public bool RunShowRead(Action<string> logger, string args, int timeOutMilliSeconds, int exitCode)
        {
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readString = string.Empty;
            bool result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, int timeOutMilliSeconds, int exitCode, ref string readData)
        {
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode: true, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, string workingDirectory, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunShowRead(Action<string> logger, string args, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, timeOutMilliSeconds, compareExitCode: false, ref exitCode, readData: true, ref readData);
            logger.AddLog(readData);
            return result;
        }

        public bool RunHideOnly(Action<string> logger, string args, string workingDirectory)
        {
            string readString = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: false, 0, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunHideOnly(Action<string> logger, string args)
        {
            string readString = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: false, 0, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShowOnly(Action<string> logger, string args, string workingDirectory)
        {
            string readString = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), workingDirectory, showWindow: true, 0, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunShowOnly(Action<string> logger, string args)
        {
            string readString = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.Run(logger, ADBExeName, ADBSN(args), string.Empty, showWindow: true, 0, compareExitCode: false, ref exitCode, readData: false, ref readString);
            logger.AddLog(readString);
            return result;
        }

        public bool RunWaitAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds)
        {
            string readData = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds)
        {
            string readData = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, ref string readData, bool compareExitCode, ref int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, ref string readData, int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, ref string readData, bool compareExitCode, ref int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, ref string readData, int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadAlive(Action<string> logger, string args, string waitString, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: false, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds)
        {
            string readData = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds)
        {
            string readData = string.Empty;
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, ref string readData, bool compareExitCode, ref int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, ref string readData, int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, ref string readData, bool compareExitCode, ref int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, ref string readData, int exitCode)
        {
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, ref string readData)
        {
            int exitCode = 0;
            var result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, bool compareExitCode, ref int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds, int exitCode)
        {
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: true, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string workingDirectory, string waitString, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), workingDirectory, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }

        public bool RunWaitReadKill(Action<string> logger, string args, string waitString, int timeOutMilliSeconds)
        {
            int exitCode = 0;
            string readData = string.Empty;
            bool result = ShellHelper.RunWait(logger, ADBExeName, ADBSN(args), string.Empty, waitString, timeOutMilliSeconds, ref readData, isKill: true, compareExitCode: false, ref exitCode);
            logger.AddLog(readData);
            return result;
        }
        #endregion

        #region 增加ADB 后台命令的执行
        public string SingleBackgroundCmdStart(Action<string> logger, string sendCmd, int timeOut = 30000, bool manulStop = false)
        {
            client = new CRunExe();
            logger("SendCmd:" + sendCmd);
            client.BeginInvokeRunExe("adb.exe", ADBSN(sendCmd), manulStop, timeOut);
            string standardOutput = client.StandardOutput;
            logger("[BackgroundCmdStart=>Receive]=" + standardOutput);
            return standardOutput;
        }

        public string SingleBackgroundCmdEnd(Action<string> logger, string exitCmd = "", bool manulStop = false)
        {
            try
            {
                if (!string.IsNullOrEmpty(exitCmd))
                {
                    logger("[Send Cmd]" + exitCmd);
                    CRunExe cRunExe = new CRunExe();
                    cRunExe.RunExe("adb.exe", ADBSN(exitCmd));
                    logger("[Receive]=" + cRunExe.StandardOutput);
                }

                client.EndInvokeRunExe(manulStop);
                string standardOutput = client.StandardOutput;
                logger("[BackgroundCmdEnd=>Receive]=" + standardOutput);
                return standardOutput;
            }
            catch (Exception ex)
            {
                logger("[BackgroundCmdEnd=>Error]=" + ex.ToString());
                return "";
            }
        }
        #endregion

        #region fastboot
        public bool FastbootDevices(Action<string> logger, int timeOutMilliSeconds, ref string readData)
        {
            bool r = ShellHelper.RunHideRead(logger, "fastboot.exe", "devices", timeOutMilliSeconds, ref readData);
            logger(readData);
            return r;
        }
        public bool FastBootRunCmd(Action<string> logger, string args, int timeOutMilliSeconds, ref string readData)
        {
            bool r = ShellHelper.RunHideRead(logger, "fastboot.exe", ADBSN(args), timeOutMilliSeconds, ref readData);
            logger(readData);
            return r;
        }
        #endregion
    }




    public class CRunExe
    {
        //返回的所有数据 (包含错误数据)
        private StringBuilder outputReceiveData = new StringBuilder();
        //记录adb 返回的实时数据
        public string StandardOutput
        {
            get
            {
                return GetOutputDataReceived();
            }
        }
        private Process CmdProcess = null;
        private delegate void RunExeEventhandle(string StartFileName, string StartFileArg, bool ManulStop, int MilSecond);
        private RunExeEventhandle rexe;
        private IAsyncResult ir;
        public Action<string> logger = null;

        private object _lock = new object();

        public void BeginInvokeRunExe(string StartFileName, string StartFileArg, bool ManulStop = true, int MilSecond = 0)
        {
            try
            {
                rexe = RunExe;
                ir = rexe.BeginInvoke(StartFileName, StartFileArg, ManulStop, MilSecond, null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message);
            }
        }
        public void EndInvokeRunExe(bool isStop = true)
        {
            try
            {
                if (isStop)
                {
                    CloseProcess();
                    Thread.Sleep(100);
                }

                while (!ir.IsCompleted)
                {
                    Application.DoEvents();
                }
                rexe.EndInvoke(ir);
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="StartFileName"></param>
        /// <param name="StartFileArg"></param>
        /// <param name="ManulStop">默认是false,当是后台运行(BeginInvokeRunExe)时ManulStop=true(配合BManulEnd,bRcvEnd一起使用)</param>
        /// <param name="timeout"></param>
        public void RunExe(string StartFileName, string StartFileArg, bool ManulStop = false, int timeout = 10000)
        {
            outputReceiveData.Clear();
            try
            {
                CmdProcess = new Process();
                //CmdProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(StartFileName);
                CmdProcess.StartInfo.FileName = StartFileName;      // 命令  
                CmdProcess.StartInfo.Arguments = StartFileArg;      // 参数  
                CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口  
                //CmdProcess.StartInfo.CreateNoWindow = false;         // 不创建新窗口  
                CmdProcess.StartInfo.UseShellExecute = false;
                CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入  
                CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出  
                CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出  
                var standardOutputResults = new TaskCompletionSource<bool>();
                CmdProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    try
                    {
                        if (e.Data != null)
                        {
                            lock (outputReceiveData)
                            {
                                outputReceiveData.AppendLine(e.Data);
                                if (logger != null) { logger(e.Data); }
                            }
                        }
                        else
                        {
                            standardOutputResults.SetResult(true);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                };
                var standardErrorResults = new TaskCompletionSource<bool>();
                CmdProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    try
                    {
                        if (e.Data != null)
                        {
                            lock (outputReceiveData)
                            {
                                outputReceiveData.AppendLine(e.Data);
                                if (logger != null) { logger(e.Data); }
                            }
                        }
                        else
                        {
                            standardErrorResults.SetResult(true);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                };
                CmdProcess.Start();
                CmdProcess.BeginOutputReadLine();
                CmdProcess.BeginErrorReadLine();
                if (!CmdProcess.WaitForExit(timeout))
                {
                    lock (outputReceiveData)
                    {
                        outputReceiveData.AppendLine("ERROR!Exe Run OutTime.");
                        if (logger != null) { logger("ERROR!Exe Run OutTime."); }
                    }
                }
                else
                {
                    //等待没有处理完毕的返回值信息
                    var begin = Environment.TickCount;
                    while (begin + 2000 > Environment.TickCount)
                    {
                        if (standardErrorResults.Task.IsCompleted && standardOutputResults.Task.IsCompleted)
                            break;
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (outputReceiveData)
                {
                    outputReceiveData.AppendLine("ERROR!" + ex.ToString());
                    if (logger != null) { logger("ERROR!" + ex.ToString()); }
                }
            }
            finally
            {
                CloseProcess();
            }
        }

        public bool SendAdbCmdInProcess(Action<string> action, string processCmd, string cmd, string adbSn)
        {
            string rcv = string.Empty;
            bool bRet = false;
            try
            {
                if (!string.IsNullOrEmpty(processCmd) && (CmdProcess == null || CmdProcess.HasExited))
                {
                    action("Process is null or hasExited,StartProcess again");
                    string sendCmd = !string.IsNullOrEmpty(adbSn) ? $"-s {adbSn} {processCmd}" : $"{processCmd}";
                    action("Send Cmd:" + sendCmd);
                    var bStartProcess = StartProcess("adb.exe", sendCmd, "", 20000);
                    if (!bStartProcess)
                    {
                        action("Error, Start Process fail!");
                    }
                }

                lock (_lock)
                {
                    outputReceiveData.Clear();
                }
                if (!string.IsNullOrEmpty(cmd))
                {
                    action("Send Cmd-InProcess:" + cmd);
                    CmdProcess.StandardInput.WriteLine(cmd);
                }
                bRet = true;
            }
            catch (Exception ex)
            {
                action("StartProcess Error: " + ex.ToString());
                bRet = false;
            }
            return bRet;

        }
        public bool StartProcess(string StartFileName, string StartFileArg, string checkContainStr, int timeout)
        {
            bool res = false;

            try
            {
                if (InitProcess(StartFileName, StartFileArg))
                {
                    string rcv = string.Empty;
                    int begin = Environment.TickCount;
                    while (Environment.TickCount < begin + timeout)
                    {
                        lock (_lock)
                        {
                            rcv = outputReceiveData.ToString();
                        }
                        if (rcv.Contains(checkContainStr))
                        {
                            res = true;
                            break;
                        }
                        Thread.Sleep(500);
                    }
                    return res;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        public bool InitProcess(string StartFileName, string StartFileArg)
        {

            outputReceiveData.Clear();
            CmdProcess = new Process();
            CmdProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(StartFileName);
            CmdProcess.StartInfo.FileName = StartFileName;
            CmdProcess.StartInfo.Arguments = StartFileArg;
            CmdProcess.StartInfo.CreateNoWindow = true;
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardError = true;
            CmdProcess.StartInfo.RedirectStandardInput = true;
            CmdProcess.StartInfo.RedirectStandardOutput = true;
            CmdProcess.OutputDataReceived += RunExe_OutputDataReceived;
            CmdProcess.ErrorDataReceived += RunExe_ErrorDataReceived;
            CmdProcess.Start();
            CmdProcess.StandardInput.AutoFlush = true;
            CmdProcess.BeginOutputReadLine();
            CmdProcess.BeginErrorReadLine();
            return true;
        }
        private void RunExe_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputReceiveData.AppendLine(e.Data);
            }
        }
        private void RunExe_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputReceiveData.AppendLine(e.Data);
            }

        }

        public void CloseProcess()
        {
            try
            {
                if (CmdProcess != null && !CmdProcess.HasExited)
                {
                    CmdProcess.Kill();
                }
            }
            catch (Exception ex)
            {
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        public void ConsoleCtrlC(Action<string> action)
        {
            if (action != null)
                action($"RunExe.Id = {CmdProcess.Id}");
            AttachConsole(CmdProcess.Id); // 附加到目标进程的console
            SetConsoleCtrlHandler(IntPtr.Zero, true); // 设置自己的ctrl+c处理，防止自己被终止
            bool resC = GenerateConsoleCtrlEvent(0, 0); // 发送ctrl+c（注意：这是向所有共享该console的进程发送）
            Thread.Sleep(500);
            SetConsoleCtrlHandler(IntPtr.Zero, false);
            //FreeConsole(); // 脱离目标console
            Thread.Sleep(500);
            if (CmdProcess != null)
            {
                if (!CmdProcess.HasExited)
                {
                    Thread.Sleep(500);
                    CmdProcess.Close();
                    //Thread.Sleep(3000);
                    CmdProcess.Dispose();
                    //Thread.Sleep(3000);
                    CmdProcess = null;
                    //Console.WriteLine("DevicesClean Exe Exit");
                    action("---Exe Exit---");
                    GC.Collect();
                }
            }
        }
        public string GetOutputDataReceived()
        {
            lock (outputReceiveData)
            {
                return outputReceiveData.ToString().TrimEnd();
            }
        }

    }
}
