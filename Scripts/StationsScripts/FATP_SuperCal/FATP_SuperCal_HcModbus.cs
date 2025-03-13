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
using System.Net;


namespace Test
{
    public partial class MainClass
    {

        
        public int PLCWrite(ITestItem item, byte slave_id, ushort address, ushort value, int preWaiting = 0,
            int afterWaiting = 0, int retryWaiting = 0)
        {

            bool result = false;
            ModbusTcpClient targetPLC = _Context.PLCClient;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                targetPLC.WriteMW(slave_id, address, value);
                item.AddLog($"PLC Write ({slave_id}) {address}: {value}");
                result = true;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
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

        public int PLCReadContinouslyTillAlarm(ITestItem item, byte slave_id, ushort address, ushort targetValue,
            int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            ModbusTcpClient targetPLC = _Context.PLCClient;

            try
            {

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                while (true)
                {
                    item.Sleep(10);

                    int _MWReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, address);
                    item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue})");

                    if (timeOut > 0)
                    {
                        if (stopwatch.ElapsedMilliseconds > timeOut)
                        {
                            item.AddLog($"timeout:{timeOut}");
                            break;
                        }
                            
                    }

                    if (_MWReadValue == targetValue)
                    {
                        item.AddLog(
                            $"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue}) sucessfully");
                        result = true;
                        break;
                    }
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
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


        // 等待启动按钮
        public int WaitStartButton(ITestItem item, byte slave_id, ushort address, ushort targetValue,
            int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)

        {
            bool result = false;
            ModbusTcpClient targetPLC = _Context.PLCClient;

            try
            {

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                while (true)
                {
                    item.Sleep(10);

                    int _MWReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, address);
                    item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue})");

                    if (_MWReadValue == targetValue)
                    {
                        item.AddLog(
                            $"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue}) sucessfully");
                        result = true;
                        break;
                    }
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
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

    }
}
