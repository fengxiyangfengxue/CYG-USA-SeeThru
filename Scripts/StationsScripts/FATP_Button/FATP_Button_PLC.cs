using MES.DLL.Test.Interface.TestResult;
using MetaHelpers.ScriptHelpers;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Test._App;
using Test._Definitions;
using Test._ScriptExtensions;
using UserHelpers.Helpers;
using Test.ModbusTCP;



namespace Test
{
    public partial class MainClass
    {

        public int PLCSetUpperBusyState(ITestItem item, bool plcUpperBusy)
        {
            try
            {
                item.AddLog($"PLC Set Upper Busy State: {plcUpperBusy}, {plcUpperBusy.GetType()}");
                Project.AppDictionary["PLCUpperBusy"] = plcUpperBusy;
                return 0;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return 1;
            }
        }

        public int PLCWaitUppperBusyState(ITestItem item, bool targetPlcUpperBusyState)
        {
            try
            {
                while (true)
                {
                    bool realTimePlcBusyState = Convert.ToBoolean(Project.AppDictionary["PLCUpperBusy"]);
                    item.AddLog($"{realTimePlcBusyState}  --> { targetPlcUpperBusyState}");
                    if (realTimePlcBusyState == targetPlcUpperBusyState)
                    {
                        item.AddLog($"Successfully Wait Upper PLC State: {targetPlcUpperBusyState}");
                        break;
                    }
                    item.Sleep(1000);
                }          
                return 0;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return 1;
            }
        }

        public int IfPLCUpperBusy(ITestItem item)
        {
            bool result = false;
            try
            {
                item.IParent.ItemDictionary["ParentPLCUpperBusy"] = Project.AppDictionary["PLCUpperBusy"];
                result = Convert.ToBoolean(item.IParent.ItemDictionary["ParentPLCUpperBusy"]);
                // return Convert.ToBoolean(item.IParent.ItemDictionary["ParentPLCUpperBusy"]) ? 0 : 1;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }
        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public double? PLCDataReadDoubleOnce(ITestItem item, byte slave_id, ushort address)
        {

            try
            {
                double? _MWReadValue = _Context.PLCClientData.ReadDouble(slave_id, address);
                item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue}");
                return _MWReadValue;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return null;
            }
        }

        public int PLCWriteMWOnce(ITestItem item, byte slave_id, ushort address, ushort value)
        {
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

            try
            {
                targetPLC.WriteMW(slave_id, address, value);
                item.AddLog($"PLC Write ({slave_id}) {address}: {value}");
                return 0;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return 1;
            }
        }

        public int? PLCReadMWOnce(ITestItem item, byte slave_id, ushort address)
        {
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

            try
            {
                int? _MWReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, address);
                item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue}");
                return _MWReadValue;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return null;
            }  
        }
                
        //TODO: BUTTON的PlcWrite和superCal中的冲突了
        public int PLCWrite_BUTTON(ITestItem item, byte slave_id, ushort address, ushort value, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {

            bool result = false;
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

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
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int PLCReadContinously(ITestItem item, byte slave_id, ushort address, ushort targetValue, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

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
                    item.Sleep(100);

                    int _MWReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, address);
                    item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue})");

                    if (timeOut > 0)
                    {
                        if (stopwatch.ElapsedMilliseconds > timeOut)
                            break;
                    }
                    if (_MWReadValue == targetValue)
                    {
                        item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue}) sucessfully");
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
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int OnlyPLCReadContinously(byte slave_id, ushort address, ushort targetValue, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

            try
            {

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                while (true)
                {
                    Thread.Sleep(200);

                    int _MWReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, address);

                    if (timeOut > 0)
                    {
                        if (stopwatch.ElapsedMilliseconds > timeOut)
                            break;
                    }
                    if (_MWReadValue == targetValue)
                    {
                        result = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                
            }

        ReturnAndExit:
            return result ? 0 : 1;
        }

        public int PLCReadContinouslyTillAlarm(ITestItem item, byte slave_id, ushort address, ushort targetValue, ushort alarmAddress, ushort alarmValue, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

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
                            break;
                    }

                    int _alarmReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, alarmAddress);
                    if (_alarmReadValue == alarmValue)
                    {
                        item.AddLog($"PLC Read Alarm ({slave_id}) {alarmAddress}: {_alarmReadValue} (target: {alarmValue})!");
                        break;
                    }

                    if (_MWReadValue == targetValue)
                    {
                        item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue}) sucessfully");
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
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int PLCReadWriteContinously(ITestItem item, byte slave_id, ushort address, ushort targetValue, ushort writeAddress, ushort writeValue, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

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
                            break;
                    }
                    if (_MWReadValue == targetValue)
                    {
                        item.AddLog($"PLC Read ({slave_id}) {address}: {_MWReadValue} (target: {targetValue}) sucessfully");
                        
                        item.Sleep(500);
                        targetPLC.WriteMW(slave_id, writeAddress, writeValue);
                        item.AddLog($"PLC Write ({slave_id}) {writeAddress}: {writeValue}");
                        item.Sleep(500);

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
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int PLCMotionAbsolute(ITestItem item, byte slave_id, ushort flow_addr, double position, ushort posAddr, double vel, ushort velAddr, ushort write_done_write_value, ushort write_done_check_value, ushort flowOKValue, int timeOut, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            ModbusTcpClient targetPLC = Project.ProjectIndex == 0 ? _Context.PLCClient : _Context.PLCClientR;

            try
            {

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                targetPLC.WriteDoubleLittleEndian(slave_id, posAddr, position);
                item.AddLog($"PLC Write ({slave_id}) {posAddr}: {position}");
                targetPLC.WriteDoubleLittleEndian(slave_id, velAddr, vel);
                item.AddLog($"PLC Write ({slave_id}) {velAddr}: {vel}");
                targetPLC.WriteMW(slave_id, flow_addr, write_done_write_value);
                item.AddLog($"PLC Write ({slave_id}) {flow_addr}: {write_done_write_value}");
                item.Sleep(500);

                while (true)
                {
                    item.Sleep(10);

                    int _MWReadValue = targetPLC.ReadMultipleMWAsIntLittleEndian(slave_id, flow_addr);
                    item.AddLog($"PLC Read ({slave_id}) {flow_addr}: {_MWReadValue} (target: {write_done_check_value})");

                    if (timeOut > 0)
                    {
                        if (stopwatch.ElapsedMilliseconds > timeOut)
                            break;
                    }
                    if (_MWReadValue == write_done_check_value)
                    {
                        item.AddLog($"PLC Read ({slave_id}) {flow_addr}: {_MWReadValue} (target: {write_done_check_value}) sucessfully");
                        item.Sleep(500);
                        targetPLC.WriteMW(slave_id, flow_addr, flowOKValue);
                        item.AddLog($"PLC Write ({slave_id}) {flow_addr}: {flowOKValue}");
                        item.Sleep(500);

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
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int? PLCDataReadMWListAvg(ITestItem item, byte slave_id, ushort address, int count)
        {
            int? mwListAvg = null;
            var mwList = new List<object>();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int _MWReadValue = _Context.PLCClientData.ReadMultipleMWAsIntLittleEndian(slave_id, address);
                    item.AddLog($"PLC Read ({slave_id}) {address}: {i} - {_MWReadValue}");
                    mwList.Add(_MWReadValue);
                }
                mwListAvg = (int)mwList.OfType<IConvertible>().Select(Convert.ToDouble).Average();
                return mwListAvg;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return mwListAvg;
            }
        }

        public double? PLCDataReadDoubleLittleEndian(ITestItem item, byte slave_id, ushort address)
        {
            try
            {
                double? _DoubleReadValue = _Context.PLCClientData.ReadDoubleLittleEndian(slave_id, address);
                item.AddLog($"PLC Read ({slave_id}) {address}: {_DoubleReadValue}");
                return _DoubleReadValue;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return null;
            }
        }

        public int ReadGripForcePosition(ITestItem item, string state, byte slave_id, ushort forceAddr, int forceCount, ushort positionAddr, string is_abs, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            // Dictionary to store the results
            var tmp_data = new Dictionary<string, object>();
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                int? force = PLCDataReadMWListAvg(item, slave_id, forceAddr, forceCount);
                double? position = PLCDataReadDoubleLittleEndian(item, slave_id, positionAddr);

                item.AddLog($"force: {force}, position: {position}");

                if (is_abs == "zero")
                {
                    tmp_data[$"Grip_Trigger_{state}_Real_Force"] = force < 0 ? 0 : force;
                }
                else
                {
                    tmp_data[$"Grip_Trigger_{state}_Real_Force"] = force;
                }

                tmp_data[$"Grip_Trigger_{state}_Real_Position"] = position;

                if (state.Contains("Pressed"))
                {
                    // TODO: surface_pos is double or int ?
                    item.AddLog($"read surface_pos from itemData");
                    var surface_pos = itemData["Grip_Trigger_Surface_Real_Position"];
                    item.AddLog($"surface_pos: {surface_pos}");
                    if (surface_pos is IConvertible && position is IConvertible)
                    {
                        double surface_pos_value = Convert.ToDouble(surface_pos);
                        double position_value = Convert.ToDouble(position);
                        double displacement = Math.Round(Math.Abs(surface_pos_value - position_value), 2);
                        tmp_data[$"Grip_Trigger_Pressed_displacement"] = displacement;
                    }
                }

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                bool _item_result = true;
                foreach (var _data in tmp_data)
                {
                    item.AddLog($"{_data.Key}: {_data.Value}");
                    _item_result &= CheckNumbericLimit(item, _data.Key.ToString(), Convert.ToDouble(_data.Value), _Limits.GetLimit(_data.Key));
                    item.AddLog(_item_result.ToString());
                }
                result = _item_result;

            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ReadForeForcePosition(ITestItem item, string state, byte slave_id, ushort forceAddr, int forceCount, ushort positionAddr, string is_abs, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            // Dictionary to store the results
            var tmp_data = new Dictionary<string, object>();
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                int? force = PLCDataReadMWListAvg(item, slave_id, forceAddr, forceCount);
                double? position = PLCDataReadDoubleLittleEndian(item, slave_id, positionAddr);
                var pre_position = itemData["pre_position"];

                if (is_abs == "zero")
                {
                    tmp_data[$"Fore_Trigger_{state}_Real_Force"] = force < 0 ? 0 : force;
                }
                else
                {
                    tmp_data[$"Fore_Trigger_{state}_Real_Force"] = force;
                }

                double position_value = Convert.ToDouble(position);
                double pre_position_value = Convert.ToDouble(pre_position);
                double displacement = Math.Abs(position_value - pre_position_value);
                tmp_data[$"Fore_Trigger_{state}_Displacement"] = displacement.ToString("F2");
                tmp_data[$"Fore_Trigger_{state}_Real_Position"] = position;
                item.AddLog($"ForeTrigger {state} displacement:{displacement}-current:{position}||{pre_position}");

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                bool _item_result = true;
                foreach (var _data in tmp_data)
                {
                    item.AddLog($"{_data.Key}: {_data.Value}");
                    _item_result &= CheckNumbericLimit(item, _data.Key.ToString(), Convert.ToDouble(_data.Value), _Limits.GetLimit(_data.Key));
                    item.AddLog(_item_result.ToString());
                }
                result = _item_result;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public bool FindForcePosition(ITestItem item, byte slave_id, ushort forceAddr, int forceCount, ushort positionAddr, double posUpper, double forceUpper, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                var pre_position = itemData["pre_position"];
                double? position = PLCDataReadDoubleLittleEndian(item, slave_id, positionAddr) - Convert.ToDouble(pre_position);
                int? force = PLCDataReadMWListAvg(item, slave_id, forceAddr, forceCount);

                item.AddLog($"Switch:Position:{position}--->pre_pos:{pre_position}--->Force:{Math.Abs((int)force)}");
                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                if ((Math.Abs(Convert.ToDouble(position)) >= posUpper) || (Math.Abs(Convert.ToDouble(force)) >= forceUpper))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return false;
            }
        }

        public int ForeNG01(ITestItem item, byte slave_id, ushort foreAddr, ushort foreNGValue, string pinchActiveNGState, int PinchActiveNGValue, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            // Dictionary to store the results
            var tmp_data = new Dictionary<string, object>();
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                PLCWrite(item, slave_id, foreAddr, foreNGValue);
                PinchActive(item, pinchActiveNGState, PinchActiveNGValue);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ForeOK01(ITestItem item, byte slave_id, string pinchActiveOKState, int PinchActiveOKValue, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            // Dictionary to store the results
            var tmp_data = new Dictionary<string, object>();
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                PinchActive(item, pinchActiveOKState, PinchActiveOKValue);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ExecuteFindForcePos(ITestItem item, byte slave_id, ushort foreAddr, ushort foreExecuteValue, ushort flowAddr, ushort flowExecuteValue, ushort targetExecuteValue)
        {
            // Dictionary to store the results
            var tmp_data = new Dictionary<string, object>();
            bool result = false;

            try
            {

                PLCWrite(item, slave_id, foreAddr, foreExecuteValue);
                PLCWrite(item, slave_id, flowAddr, flowExecuteValue);
                PLCReadContinouslyTillAlarm(item, slave_id, flowAddr, targetExecuteValue, 914, 5, 20);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int FindForePinchLoop(ITestItem item, byte slave_id, ushort forceAddr, int forceCount, ushort positionAddr, double posUpper, double forceUpper, 
            ushort foreAddr, ushort foreNGValue, string pinchActiveNGState, int PinchActiveNGValue, 
            ushort foreExecuteValue, ushort flowAddr, ushort flowExecuteValue, ushort targetExecuteValue, 
            string forePinchState, string pinchActiveOKState, int PinchActiveOKValue, 
            int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {

            try
            {
                while (true)
                {
                    if (!FindForcePosition(item, slave_id, forceAddr, forceCount, positionAddr, posUpper, forceUpper))
                    {
                        ForeNG01(item, slave_id, foreAddr, foreNGValue, pinchActiveNGState, PinchActiveNGValue); // Exit if find_force_pos fails
                        break;
                    }

                    ExecuteFindForcePos(item, slave_id, foreAddr, foreExecuteValue, flowAddr, flowExecuteValue, targetExecuteValue);

                    ReadForePinch(item, forePinchState);
                    if (Convert.ToBoolean(item.IParent.ItemDictionary["ParentFindPinch"]))
                    {
                        ForeOK01(item, slave_id, pinchActiveOKState, PinchActiveOKValue);
                        break;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return 1;
            }
        }

        public int SetPrePos(ITestItem item, byte slave_id, ushort positionAddr, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            // Dictionary to store the results
            var tmp_data = new Dictionary<string, object>();
            bool result = false;

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                double? pre_position = PLCDataReadDoubleLittleEndian(item, slave_id, positionAddr);
                item.AddLog($"pre_pos------>:{pre_position}");
                tmp_data[$"pre_position"] = pre_position + 0.50;

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }
    }
}
