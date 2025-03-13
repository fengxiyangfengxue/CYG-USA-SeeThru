using System.IO;
using System;
using Test._App;
using UserHelpers.Helpers;
using System.Threading;
using System.Text.RegularExpressions;

using System.Windows.Media;
using System.Windows.Forms;
using System.IO.Ports; 
using GTKWebServices;
using Test._ScriptHelpers;
using Test.ScriptSettings;
using Test._Definitions;
using Test._ScriptExtensions;
using System.Collections.Generic;

namespace Test
{
    public partial class MainClass
    {
         
        public int Get_SW_Version(ITestItem item)
        {
            string version = _buildSetting.Version;
            ResultData data = new ResultData(item.Title, "", version);
            AddResult(item, data);
            return 0;
        }

        public int Delay(ITestItem item, int Millseconds = 1000, bool isAddResult = false)
        {
            item.Sleep(Millseconds);

            if (isAddResult)
            {
                ResultData data = new ResultData(item.Title, "", Millseconds.ToString());
                AddResult(item, data);
            }

            return 0;
        }

        public int Manual_ScanSerialNumber(ITestItem item, int snLength = 0, string pattern = "")
        {
            bool result = false;

            if (snLength == 0)
                snLength = _commonSetting.SNLength; 

            BarCodeConfig config = new BarCodeConfig()
            {
                Title = $"[{Project.ProjectIndex + 1}] Scan {snLength} Chars Serial Number：",
                MakeUpper = true,
                IsClosable = true,
            };

            CheckBarCodeEventHandler snCheck = (s) =>
            {
                if (snLength < 0) //小于0表示不卡长度
                    return !string.IsNullOrEmpty(s); 

                if (s.Length != snLength)
                    return false;

                if (!string.IsNullOrEmpty(pattern))
                {
                    Regex regex = new Regex(pattern);
                    if (regex.IsMatch(s))
                    {
                        return true;
                    }
                }
                return true;
            };

            config.ValidationHandler += snCheck;

            if (IsLooping())
            {
                item.AddLog("looping");
                Project.SerialNumber = GetLoopingSN(Project.ProjectIndex); 
            }
            else
            {
                string sn = BarCodeHelper.Get(Project, config);
                Project.SerialNumber = sn;
            }

            result = snCheck(Project.SerialNumber);

            item.AddLog("SerialNumber = " + Project.SerialNumber);
            Project.PathDictionary["SN"] = Project.SerialNumber;
            Project.SideBar.TopBar.Add("SN", Project.SerialNumber);

            AddResult(item, new ResultData("SerialNumber", CreateErrorCode(result, item.Title), Project.SerialNumber));
            return result ? 0 : 1;
        }

        public int Scanner_ScanSerialNumber(ITestItem item, int snLength = 0, string pattern = "^2G0YC[0-9A-Z]{9}$", int timeout = 30000)
        {
            bool result = false;
            SerialPort scanner = null;
            try
            {
                if (snLength <= 0)
                    snLength = _commonSetting.SNLength;
                  
                scanner = new SerialPort(_scannerSetting.ComConfigs[Project.ProjectIndex].Port, _scannerSetting.ComConfigs[Project.ProjectIndex].BaudRate, Parity.None, 8, StopBits.One);

                scanner.ReadBufferSize = 4096;
                //scanner.LogConfig(item.AddLog);
                scanner.Open();
                result = true;
                string read = string.Empty;


                UIMessageBoxConfig config = new UIMessageBoxConfig()
                {
                    Title = "提示",
                    Text = $"UUT[{Project.ProjectIndex + 1}] 请扫描SN",
                    TextFontSize = 32,
                    WaitForExit = false,
                    TextColor = Colors.Green,
                    IsTopMost = true,
                    TimeOut = timeout,
                    AliveWith = item,
                };
                UIMessageBox.Show(Project, config);

                DateTime dtTimeOut = DateTime.Now.AddMilliseconds(timeout);
                while (DateTime.Now < dtTimeOut)
                {
                    Thread.Sleep(100);
                    string tmp = scanner.ReadExisting();
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        item.AddLog(tmp);
                        read = read + tmp;
                    }
                    if (read.IndexOf('\x0d') >= 0 ||
                        (read.StartsWith("\x1d") && read.Length >= snLength + 1) ||
                        (!read.StartsWith("\x1d") && read.Length >= snLength))
                    {
                        read = read.Replace("\x1d", "").RemoveCRLF().Trim();
                        item.AddLog("read = " + read);

                        if (read.Length != snLength)
                        {
                            item.AddLog("barcode length error!");
                            break;
                        }

                        if(!string.IsNullOrEmpty(pattern))
                        {
                            Regex regex = new Regex(pattern);
                            if (!regex.IsMatch(read))
                            {
                                item.AddLog("barcode format error!");
                                break;
                            }
                        }
                         
                        Project.SerialNumber = read;
                        Project.PathDictionary["SN"] = read;
                        Project.SideBar.TopBar.Add("SN", read);

                        result = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }
            finally
            {
                if (scanner != null && scanner.IsOpen)
                    scanner.Close();
            }

            ReturnAndExit:

            AddResult(item, new ResultData("SerialNumber", CreateErrorCode(result, item.Title), Project.SerialNumber));
            return result ? 0 : 1;
        }

    }


}
