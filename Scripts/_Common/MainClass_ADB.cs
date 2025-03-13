using System;
using Test._App;
using UserHelpers.Helpers;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Threading;
using Test._ScriptHelpers;
using Test._Definitions;
using Test._ScriptExtensions;
using Test.USBDevice;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Test
{
    public partial class MainClass
    {

        public int ADB_RunCommandOnly(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                result = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog(read);
                item.AddLog("result = " + result);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckExitCode(ITestItem item, string command, int exitCode, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                result = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, exitCode, ref read);
                item.AddLog(read);
                item.AddLog("result = " + result);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }



        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int Adb_OSVersion(ITestItem item, string OSVersion = "", int timeout = 5000)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                string cmd = "shell getprop ro.build.fingerprint";
                bool isOk = _Context.ADBCaller.RunHideRead(item.AddLog, cmd, timeout, ref read);
                if (!isOk)
                    goto ReturnAndExit;

                read = read.Trim().RemoveCRLF();

                if (string.IsNullOrEmpty(OSVersion) || OSVersion.Split(',').Any(a => a == read))
                {
                    result = !string.IsNullOrEmpty(read);
                    _Context.Variables[ConstKeys.OS_Version] = read;
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), read);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckString(ITestItem item, string command, string checkString, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string resultString = string.Empty;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog("checkString = " + checkString);
                string read = string.Empty;
                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog(read);
                resultString = read.RemoveCRLF();

                if (!isOK)
                    goto ReturnAndExit;

                result = string.IsNullOrEmpty(checkString) || resultString.Equals(checkString, StringComparison.OrdinalIgnoreCase);


                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), resultString, checkString, checkString);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckLimitString(ITestItem item, string command, string limitName, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string resultString = string.Empty;
            ItemLimit limit = null;
            try
            {
                limit = _Limits.GetLimit(limitName);
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog(read);
                resultString = read.RemoveCRLF();

                if (!isOK)
                    goto ReturnAndExit;

                result = string.IsNullOrEmpty(limit.CheckString) || resultString.Equals(limit.CheckString);


                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), resultString);
            if (limit != null)
            {
                data.LowerLimit = limit.CheckString;
                data.UpperLimit = limit.CheckString;
            }

            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_ConstainsString(ITestItem item, string command, string checkString, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog("checkString = " + checkString);
                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog(read);
                if (!isOK)
                    goto ReturnAndExit;

                result = string.IsNullOrEmpty(checkString) || read.IndexOf(checkString, StringComparison.OrdinalIgnoreCase) >= 0;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckAnyString(ITestItem item, string command, string checkString, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog("read = " + read);
                if (!isOK)
                    goto ReturnAndExit;

                resultString = read.RemoveCRLF();

                var arr = checkString.SplitToList(";");

                result = (arr.Count == 0 || arr.Any(s => resultString.Equals(s)));

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), resultString);
            data.Message = "checkString = " + checkString;
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckAnyLimitString(ITestItem item, string command, string limitName, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            ItemLimit limit = null;
            string resultString = string.Empty;
            try
            {
                limit = _Limits.GetLimit(limitName);
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog("read = " + read);
                if (!isOK)
                    goto ReturnAndExit;

                resultString = read.RemoveCRLF();

                var arr = limit.CheckString.SplitToList(";");

                result = (arr.Count == 0 || arr.Any(s => resultString.Equals(s)));

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), resultString);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckValue(ITestItem item, string command, string limitName, double devide = 1, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string resultString = string.Empty;
            bool isDataOK = false;
            ItemLimit limit = null;

            try
            {
                limit = _Limits.GetLimit(limitName);

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, timeOut, ref read);
                item.AddLog(read);
                resultString = read.RemoveCRLF();

                if (!isOK)
                    goto ReturnAndExit;

                double dTemp = double.Parse(resultString);
                item.AddLog("data = " + dTemp);
                if (dTemp != 1)
                    dTemp = dTemp / devide;
                item.AddLog("data = " + dTemp);

                result = CheckLimit(item, item.Title, CreateErrorCode(item.Title).Name, dTemp, limit);
                isDataOK = true;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            if (!isDataOK)
                AddFailedResult(item, item.Title, CreateErrorCode(result, item.Title), resultString, limit);

            return result ? 0 : 1;
        }

        public int ADB_PCBA_ReadSerialNumber(ITestItem item, string command, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string sn = string.Empty;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, 10000, ref read);
                item.AddLog("read = " + read);

                sn = read.Replace("\r", "\n").Split('\n')[0];


                if (!isOK)
                    goto ReturnAndExit;

                result = !string.IsNullOrEmpty(read) && read.Length == _commonSetting.SNLength;

                if (result)
                {
                    Project.SerialNumber = sn;
                    Project.PathDictionary["SN"] = Project.SerialNumber;
                    Project.SideBar.TopBar.Add("SN", Project.SerialNumber);
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), sn);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_FATP_ReadSerialNumber(ITestItem item, string command, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string sn = string.Empty;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                bool isOK = _Context.ADBCaller.RunHideRead(item.AddLog, command, 10000, ref read);
                item.AddLog("read = " + read);

                sn = read.Replace("\r", "\n").Split('\n')[0].Trim();


                if (!isOK)
                    goto ReturnAndExit;

                result = !string.IsNullOrEmpty(sn) && sn.Length == _commonSetting.SNLength;

                if (result)
                {
                    Project.SerialNumber = sn;
                    Project.PathDictionary["SN"] = Project.SerialNumber;
                    Project.SideBar.TopBar.Add("SN", Project.SerialNumber);
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), sn);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        //调用ADB后让其后台运行, 不再管它的后续
        public int ADB_CheckCallAlive(ITestItem item, string command, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string read = string.Empty;
                result = _Context.ADBCaller.RunHideOnly(item.AddLog, command);
                item.AddLog(read);
                item.AddLog("result = " + result);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_PushFile(ITestItem item, string localFilePath, string remoteFilePath, int timeout = 5000)
        {
            bool result = false;
            try
            {
                string cmd = $"push \"{localFilePath}\" \"{remoteFilePath}\"";
                result = _Context.ADBCaller.RunHideRead(item.AddLog, cmd, timeout, 0);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }
        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);

            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_PullFile(ITestItem item, string localFilePath, string remoteFilePath, int timeout = 5000, bool isDeleteOld = true)
        {
            bool result = false;
            try
            {
                var fiLocal = new FileInfo(localFilePath);
                if (isDeleteOld)
                {
                    if (fiLocal.Exists)
                        fiLocal.Delete();
                }

                if (!fiLocal.Directory.Exists)
                    fiLocal.Directory.Create();

                string cmd = $"pull \"{remoteFilePath}\" \"{localFilePath}\"";
                result = _Context.ADBCaller.RunHideRead(item.AddLog, cmd, timeout, 0);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);

            AddResult(item, data);
            return result ? 0 : 1;
        }


        //调用logcat功能在DUT内部后台运行
        public int ADB_LogcatStart(ITestItem item, int timeout = 30000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!_Context.ADBCaller.RunHide(item.AddLog, "shell \"kill `pidof logcat`\"", timeout))
                    goto ReturnAndExit;
                if (!_Context.ADBCaller.RunHideRead(item.AddLog, "rm /data/logcat.txt", timeout))
                    goto ReturnAndExit;
					
                if (!_Context.ADBCaller.RunHideRead(item.AddLog, "shell setprop persist.debug.adbd.logging all", timeout))
                    goto ReturnAndExit;
					
                if (!_Context.ADBCaller.RunHide(item.AddLog, "shell \"nohup logcat > /data/logcat.txt 2>&1 &\""))
                    goto ReturnAndExit;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }
            finally
            {
                result = true; //不FAIL
            }


        ReturnAndExit:
            return result ? 0 : 1;
        }

        //结束DUT内部logcat, pull出log
        public int ADB_LogcatEnd(ITestItem item, int timeout = 30000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;

            string destFile = Path.Combine(_Context.TmpFolder, Project.SerialNumber + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + "_logcat_[" + (Project.ProjectIndex + 1) + "].txt");
            item.AddLog($"destFile: {destFile}");

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string dir = new FileInfo(destFile).DirectoryName;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!_Context.ADBCaller.RunHide(item.AddLog, "shell \"kill `pidof logcat`\"", timeout))
                    goto ReturnAndExit;

                if (!_Context.ADBCaller.RunHide(item.AddLog, "rm /data/logcat.txt", timeout))
                    goto ReturnAndExit;

                item.Sleep(100);

                if (!_Context.ADBCaller.RunHideRead(item.AddLog, $"pull /data/logcat.txt \"{destFile}\"", timeout, 0))
                    goto ReturnAndExit;

                Project.BackUp.BackupFile(destFile);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }
            finally
            {
                result = true; //不FAIL
            }

        ReturnAndExit:
            return result ? 0 : 1;
        }


        public int Adb_TdbLog_Start(ITestItem item, int timeout = 30000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);
                if (!_Context.ADBCaller.RunHideRead(item.AddLog, "shell \"kill `pidof tdb`\"", timeout))
                    goto ReturnAndExit;

                item.Sleep(100);

                if (!_Context.ADBCaller.RunHideRead(item.AddLog, "rm /data/tdblog.txt", timeout))
                    goto ReturnAndExit;

                if (!_Context.ADBCaller.RunHideOnly(item.AddLog, "shell tdb log"))
                    goto ReturnAndExit;

                if (!_Context.ADBCaller.RunHide(item.AddLog, "shell \"nohup tdb log > /data/tdblog.txt 2>&1 &\""))
                    goto ReturnAndExit;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }
            finally
            {
                result = true; //不FAIL
            }

        ReturnAndExit:
            return result ? 0 : 1;
        }
        public int Adb_TdbLog_End(ITestItem item, int timeout = 30000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;

            int id = Project.ProjectIndex;
            string SN = Project.SerialNumber;


            string destFile = Path.Combine(_Context.TmpFolder, Project.SerialNumber + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + "_tdblog_[" + (Project.ProjectIndex + 1) + "].txt");
            item.AddLog($"destFile: {destFile}");
            try
            {

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                string dir = new FileInfo(destFile).DirectoryName;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!_Context.ADBCaller.RunHide(item.AddLog, "shell \"kill `pidof tdb`\"", timeout))
                    goto ReturnAndExit;

                Thread.Sleep(200);

                if (!_Context.ADBCaller.RunHideRead(item.AddLog, $"pull /data/tdblog.txt \"{destFile}\"", timeout))
                    goto ReturnAndExit;

                Project.BackUp.BackupFile(destFile);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }
            finally
            {
                result = true; //不FAIL
            }

        ReturnAndExit:
            return result ? 0 : 1;
        }
        public int ADB_CheckUSBSpeed(ITestItem item, int timeout = 5000, bool isUSB2 = false)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                string cmd = "shell \"cat /sys/devices/platform/soc/a600000.ssusb/a600000.dwc3/udc/a600000.dwc3/current_speed\"";
                var isOK = _Context.ADBCaller.RunHideRead(item.AddLog, cmd, timeout, ref read);
                item.AddLog(read);
                if (!isOK)
                    goto ReturnAndExit;

                result = isUSB2 ? read.Contains("high-speed") : read.Contains("super-speed");
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            AddResult(item, new ResultData(item.Title, CreateErrorCode(result, item.Title), read.TrimEnd()));
            return result ? 0 : 1;
        }


        //USB ADB
        public int USB_ADB_WaitForDevice(ITestItem item, int timeOut)
        {
            bool result = false;
            try
            {
                DateTime dtEnd = DateTime.Now.AddMilliseconds(timeOut);
                while (DateTime.Now < dtEnd)
                {
                    item.Sleep(1000);
                    if (USBDeviceHandler.ADB_DetectDevice(item, USBDeviceHandler.ADB_ComPort_DeviceName, _adbLocationSetting.ADB_Locations[Project.ProjectIndex].Location))
                    {
                        result = true;
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int USB_ADB_Get_SerialNumber(ITestItem item)
        {
            bool result = false;
            _Context.ADBSN = string.Empty;
            try
            {
                _Context.ADBSN = USBDeviceHandler.ADB_Get_SerialNumber(item, USBDeviceHandler.ADB_ComPort_DeviceName, _adbLocationSetting.ADB_Locations[Project.ProjectIndex].Location).Trim().Replace("\x0", "").Trim();
                _Context.ADBSN = _Context.ADBSN.ToUpper();
                item.AddLog("adb sn = " + _Context.ADBSN);
                result = !string.IsNullOrEmpty(_Context.ADBSN);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), _Context.ADBSN);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int USB_ADB_Get_SerialPort(ITestItem item)
        {
            bool result = false;
            _Context.ADBComPort = string.Empty;
            try
            {
                _Context.ADBComPort = USBDeviceHandler.ADB_Get_ComPort(item, USBDeviceHandler.ADB_ComPort_DeviceName, _adbLocationSetting.ADB_Locations[Project.ProjectIndex].Location).Trim().Replace("\x0", "");
                _Context.ADBComPort = _Context.ADBComPort.ToUpper();
                item.AddLog("adb port = " + _Context.ADBComPort);
                result = !string.IsNullOrEmpty(_Context.ADBComPort);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), _Context.ADBComPort);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int USB_WaitForBootLoader(ITestItem item, int timeOut)
        {
            bool result = false;
            try
            {
                DateTime dtEnd = DateTime.Now.AddMilliseconds(timeOut);
                while (DateTime.Now < dtEnd)
                {
                    item.Sleep(1000);
                    if (USBDeviceHandler.BootLoader_DetectDevice(item, USBDeviceHandler.BootLoader_USB_DeviceName, _adbLocationSetting.Bootloader_Locations[Project.ProjectIndex].Location))
                    {
                        result = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int BootLoader_Get_SerialNumber(ITestItem item)
        {
            bool result = false;
            _Context.BootloaderSN = string.Empty;
            try
            {
                _Context.BootloaderSN = USBDeviceHandler.BootLoader_Get_SerialNumber(item, USBDeviceHandler.BootLoader_USB_DeviceName, _adbLocationSetting.Bootloader_Locations[Project.ProjectIndex].Location).Trim().Replace("\x0", "").Trim();
                item.AddLog("bootloader sn = " + _Context.BootloaderSN);
                result = !string.IsNullOrEmpty(_Context.BootloaderSN);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), _Context.BootloaderSN);
            AddResult(item, data);
            return result ? 0 : 1;
        }


    }
}