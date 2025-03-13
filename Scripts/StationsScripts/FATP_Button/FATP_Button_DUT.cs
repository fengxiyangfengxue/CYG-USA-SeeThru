
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Test._App;
using UserHelpers.Helpers; 
using System.Threading.Tasks;
using Test._ScriptHelpers;
using Test._Definitions;
using Test._ScriptExtensions;
using MetaHelpers.ScriptHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using MES.DLL.Test.Interface.Web_Wip_Hold;
using System.Net;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Documents;
using System.Collections;
using System.Globalization;
using System.Management;
using static System.Windows.Forms.AxHost;


namespace Test
{
    public partial class MainClass
    {
        // 因为和supercal命名冲突 修改button命名
        private Process cmdProcess_button;
        public CancellationTokenSource cts_button = new CancellationTokenSource();
        string _mainAppVersionLogs = string.Empty;
        string _snLogs = string.Empty;
        string _handednessLogs = string.Empty;
        string _InputFWLogs = string.Empty;
        public Dictionary<string, object> itemData = new Dictionary<string, object>();
        public Dictionary<string, object> mesData = new Dictionary<string, object>();
        public Dictionary<string, object> mesDataFFT = new Dictionary<string, object>();
        public List<Dictionary<string, object>> rec_data = new List<Dictionary<string, object>>();
        public List<object> deadbandXAdc = new List<object>();
        public List<object> deadbandYAdc = new List<object>();
        public List<object> deadbandXAdcAvg = new List<object>();
        public List<object> deadbandYAdcAvg = new List<object>();
        public List<object> foreAdc = new List<object>();
        public List<object> gripAdc = new List<object>();
        public bool do_rec_data_button = false;
        string fftResultFile = string.Empty;
        string _fftLogs = string.Empty;
        string _syncBossLogs = string.Empty;

        ConsoleHelper _cameraConsole = null;
        ConsoleHelper _console_pex = null;


        public int StateButton(ITestItem item, string dataType, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog($"StateButton Begin");
                var _dut_data = GetLastNList(rec_data, 10);
                item.AddLog($"recv data============={_dut_data}----{state} (==Should Be Same as Captouch Before==)");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                var ret_ax = ButtonFilterByName(_dut_data, dataType, "ax");
                var ret_by = ButtonFilterByName(_dut_data, dataType, "by");
                var ret_ts = ButtonFilterByName(_dut_data, dataType, "ts");
                var ret_sys = ButtonFilterByName(_dut_data, dataType, "sys");

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                tmp_data[$"Buttons_Ax_{state}"] = ret_ax.Last();
                tmp_data[$"Buttons_By_{state}"] = ret_by.Last();
                tmp_data[$"Buttons_Ts_{state}"] = ret_ts.Last();
                tmp_data[$"Buttons_Home_{state}"] = ret_sys.Last();

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
                }

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"StateButton End");

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

        public int ButtonCapTouch(ITestItem item, string dataType, string buttonName, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                item.AddLog($"{state}_{dataType}||{buttonName} Begain");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                var _dut_data = GetLastNList(rec_data, 10);
                item.AddLog($"{state}_{dataType}||{buttonName} recv data============={_dut_data}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                var _ret = TidyCaptouchData(item, _dut_data, dataType, buttonName);
                item.AddLog($"{state}_{dataType}||{buttonName} End");

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                string _item_title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(buttonName.Replace("_", ""));
                
                item.AddLog($"item_title: {_item_title}, state: {state}");
                tmp_data[$"{_item_title}_Capsense_Value_Max_{state}"] = _ret[0];
                tmp_data[$"{_item_title}_Capsense_Value_Min_{state}"] = _ret[1];
                tmp_data[$"{_item_title}_Capsense_Value_Avg_{state}"] = _ret[2];
                tmp_data[$"{_item_title}_Capsense_Value_First_{state}"] = _ret[3];
                tmp_data[$"{_item_title}_Capsense_Value_Last_{state}"] = _ret[4];

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
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

                return result ? 0 : 1;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                ResultData ex_data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
                AddResult(item, ex_data);
                return 1;
            }
        }

        public int TriggerCapTouch(ITestItem item, string dataType, string buttonName, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                item.AddLog($"{state}_{dataType}||{buttonName} Begain");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                var _dut_data = GetLastNList(rec_data, 10);
                item.AddLog($"{state}_{dataType}||{buttonName} recv data============={_dut_data}");

                foreach (var _dut in _dut_data)
                { 
                    item.AddLog(JsonConvert.SerializeObject(_dut)); 
                }

                var _ret = TidyCaptouchData(item, _dut_data, dataType, buttonName);
                item.AddLog($"{state}_{dataType}||{buttonName} End");

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();
                string _item_title = string.Empty;
                
                if (buttonName.Contains("fore_trigger"))
                { 
                    _item_title = "Fore_Trigger"; 
                }
                else
                { 
                    _item_title = "Grip_Trigger"; 
                }

                tmp_data[$"{_item_title}_Capsense_Value_Max_{state}"] = _ret[0];
                tmp_data[$"{_item_title}_Capsense_Value_Min_{state}"] = _ret[1];
                tmp_data[$"{_item_title}_Capsense_Value_Avg_{state}"] = _ret[2];
                tmp_data[$"{_item_title}_Capsense_Value_First_{state}"] = _ret[3];
                tmp_data[$"{_item_title}_Capsense_Value_Last_{state}"] = _ret[4];

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
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

        public List<object> TidyCaptouchData(ITestItem item, List<Dictionary<string, object>> dataLines, string dataType, string buttonName)
        {
            var collectedValues = new List<object>();
            var tidyValues = new List<object>();

            try
            {
                foreach (var data in dataLines) 
                {
                    item.AddLog($"{data.ContainsKey(dataType)}");
                    if (data.ContainsKey(dataType))
                    {
                        object dataValue = data[dataType];
                        item.AddLog($"{dataValue}");
                        item.AddLog($"{dataValue.GetType()}");
                        if (dataValue is JObject valueDic && valueDic.ContainsKey(buttonName))
                        {
                            item.AddLog($"----> {valueDic[buttonName]}");
                            collectedValues.Add(valueDic[buttonName]);
                        }
                    }
                }
                tidyValues.Add(collectedValues.Max());
                tidyValues.Add(collectedValues.Min());
                tidyValues.Add((int)collectedValues.OfType<IConvertible>().Select(Convert.ToDouble).Average());
                tidyValues.Add(collectedValues[0]);
                tidyValues.Add(collectedValues.Last());

                item.AddLog($"====  tidyValues  ====");
                foreach(var tidy_value in tidyValues)
                {
                    item.AddLog($"{tidy_value}");
                }
                return tidyValues;
            }
            catch
            {
                return tidyValues;
            }
        }

        public int ADC(ITestItem item, string dataType, string buttonName, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                item.AddLog($"{dataType}_adc||{buttonName} Begain");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                var _dut_data = GetLastNList(rec_data, 10);
                item.AddLog($"{dataType} {buttonName} recv data============={_dut_data}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                var _ret = TidyADCData(item, _dut_data, dataType, buttonName);
                item.AddLog($"{state}_adc||{buttonName} End");

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();
                string _item_title = string.Empty;

                if (buttonName.Contains("jsx"))
                    { _item_title = "Thumbstick_Deadband_X_Axis"; }
                else if (buttonName.Contains("jsy"))
                    { _item_title = "Thumbstick_Deadband_Y_Axis"; }
                else if (buttonName.Contains("trig"))
                    { _item_title = "Fore_Trigger"; }
                else
                    { _item_title = "Grip_Trigger"; }

                tmp_data[$"{_item_title}_ADC_Max_{state}"] = _ret[0];
                tmp_data[$"{_item_title}_ADC_Min_{state}"] = _ret[1];
                tmp_data[$"{_item_title}_ADC_Avg_{state}"] = _ret[2];
                tmp_data[$"{_item_title}_ADC_First_{state}"] = _ret[3];
                tmp_data[$"{_item_title}_ADC_Last_{state}"] = _ret[4];

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
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

        public List<object> TidyADCData(ITestItem item, List<Dictionary<string, object>> dataLines, string dataType, string buttonName)
        {
            var collectedValues = new List<object>();
            var tidyValues = new List<object>();

            try
            {
                foreach (var data in dataLines)
                {
                    if (data.ContainsKey(dataType))
                    {
                        {
                            object dataValue = data[dataType];
                            if (dataValue is JObject valueDic && valueDic.ContainsKey(buttonName))
                            {
                                // Collect the entire value
                                collectedValues.Add(valueDic[buttonName]);
                            }
                        }
                    }
                }
                tidyValues.Add(collectedValues.Max());
                tidyValues.Add(collectedValues.Min());
                tidyValues.Add((int)collectedValues.OfType<IConvertible>().Select(Convert.ToDouble).Average());
                tidyValues.Add(collectedValues[0]);
                tidyValues.Add(collectedValues.Last());
                return tidyValues;
            }
            catch
            {
                return tidyValues;
            }
        }

        public int CircleRangeEnd(ITestItem item, string prod_type, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                var _dut_data = GetLastNList(rec_data, 10);
                item.AddLog($"circle range recv data============={_dut_data}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                // Lists to store ADC values
                List<int> x_adc = new List<int>();
                List<int> y_adc = new List<int>();

                foreach (var recv in _dut_data)
                {
                    if (recv.ContainsKey("adc"))
                    {
                        object adcDict = recv["adc"];
                        if (adcDict is JObject valueDic)
                        {
                            if (valueDic.ContainsKey("jsx"))
                            {
                                x_adc.Add(Convert.ToInt32(valueDic["jsx"]));
                            }
                            if (valueDic.ContainsKey("jsy"))
                            {
                                y_adc.Add(Convert.ToInt32(valueDic["jsy"]));
                            }
                        }
                    } 
                }

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                if (x_adc.Any())
                {
                    tmp_data["Thumbstick_Range_Cal_X_Axis_ADC_Min"] = x_adc.Min();
                    tmp_data["Thumbstick_Range_Cal_X_Axis_ADC_Max"] = x_adc.Max();
                    tmp_data["Thumbstick_X_Axis_Range_Delta"] = x_adc.Max() - x_adc.Min();
                }

                if (y_adc.Any())
                {
                    tmp_data["Thumbstick_Range_Cal_Y_Axis_ADC_Min"] = y_adc.Min();
                    tmp_data["Thumbstick_Range_Cal_Y_Axis_ADC_Max"] = y_adc.Max();
                    tmp_data["Thumbstick_Y_Axis_Range_Delta"] = y_adc.Max() - y_adc.Min();
                }

                if (prod_type == "ruby")
                {
                    tmp_data["Thumbstick_Range_Cal_Adjacent_Points_Distance"] = CalADC(x_adc, y_adc);
                }

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
                }

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"thumbstick Range End");

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

        public int GetDeadbandAdc(ITestItem item, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog($"GetDeadbandAdc Begin");
                var _dut_data = GetLastNList(rec_data, 10);
                item.AddLog($"recv data============={_dut_data}----{state}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                var _retx = ButtonFilterByName(_dut_data, "adc", "jsx");
                var _retx_avg = (int)_retx.OfType<IConvertible>().Select(Convert.ToDouble).Average();
                deadbandXAdc.AddRange(_retx);
                deadbandXAdcAvg.Add(_retx_avg);

                tmp_data[$"Thumbstick_Deadband_X_Axis_ADC_Avg_{state}"] = _retx_avg;
                tmp_data[$"Thumbstick_Deadband_X_Axis_ADC_Min_{state}"] = _retx.Min();
                tmp_data[$"Thumbstick_Deadband_X_Axis_ADC_Max_{state}"] = _retx.Max();
                tmp_data[$"Thumbstick_Deadband_X_Axis_ADC_First_{state}"] = _retx[0];
                tmp_data[$"Thumbstick_Deadband_X_Axis_ADC_Last_{state}"] = _retx.Last();

                var _rety = ButtonFilterByName(_dut_data, "adc", "jsy");
                var _rety_avg = (int)_rety.OfType<IConvertible>().Select(Convert.ToDouble).Average();
                deadbandYAdc.AddRange(_rety);
                deadbandYAdcAvg.Add(_rety_avg);

                tmp_data[$"Thumbstick_Deadband_Y_Axis_ADC_Avg_{state}"] = _rety_avg;
                tmp_data[$"Thumbstick_Deadband_Y_Axis_ADC_Min_{state}"] = _rety.Min();
                tmp_data[$"Thumbstick_Deadband_Y_Axis_ADC_Max_{state}"] = _rety.Max();
                tmp_data[$"Thumbstick_Deadband_Y_Axis_ADC_First_{state}"] = _rety[0];
                tmp_data[$"Thumbstick_Deadband_Y_Axis_ADC_Last_{state}"] = _rety.Last();

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
                }

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"StateButton End");

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

        public int StateTriggerWithPinch(ITestItem item, string dataType, string buttonName, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog($"StateTriggerWithPinch Begin");
                // read 10 record to rec_data
                ReadMultiData(item, 100, 10, 10000);
                var _dut_data = rec_data;
                item.AddLog($"recv data============={_dut_data}----{state}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                var _ret = ButtonFilterByName(_dut_data, dataType, buttonName);
                var _ret_avg = (int)_ret.OfType<IConvertible>().Select(Convert.ToDouble).Average();
                string title = string.Empty;
                if ((dataType=="adc") && (buttonName=="grip"))
                {
                    title = "Grip";
                    gripAdc.AddRange(_ret);
                }
                else if ((dataType == "adc") && (buttonName == "trig"))
                {
                    title = "Fore";
                    foreAdc.AddRange(_ret);
                }

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                tmp_data[$"{title}_Trigger_ADC_Max_{state}"] = _ret.Max();
                tmp_data[$"{title}_Trigger_ADC_Min_{state}"] = _ret.Min();
                tmp_data[$"{title}_Trigger_ADC_Avg_{state}"] = _ret_avg;
                tmp_data[$"{title}_Trigger_ADC_First_{state}"] = _ret[0];
                tmp_data[$"{title}_Trigger_ADC_Last_{state}"] = _ret.Last();

                // todo: Pinch_Button_Status_???
                //_ret = ButtonFilterByName(_dut_data, "Pinch Active");
                //tmp_data[$"Pinch_Button_Status_{title}_{state}"] = _ret[0];


                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
                }

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"StateTriggerWithPinch End");

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

        public int ForeTriggerPinchActive(ITestItem item)
        {
            //set a value in ItemDictionary
            item.ItemDictionary["ParentFindPinch"] = true;
            item.AddLog($"ForeTriggerPinchActive initialize find_pinch to false");
            return 0;
        }

        // TODO: pinch active???
        public int ReadForePinch(ITestItem item, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            return 0;
        }
        /*
        public int ReadForePinch(ITestItem item, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog($"ReadForePinch Begin");
                // read 10 record to rec_data
                ReadMultiData(item, 10, 10, 10000);
                var _dut_data = rec_data;
                item.AddLog($"recv data============={_dut_data}----{state}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                var _ret = ButtonFilterByName(_dut_data, "Pinch Active");
                foreach (var value in _ret)
                {
                    if (!value.Equals(Convert.ToInt32(state)))
                    {
                        item.IParent.ItemDictionary["ParentFindPinch"] = false;
                        break;
                    }
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"ReadForePinch End");
                result = true;
            }
            catch (Exception ex)
            {
                item.IParent.ItemDictionary["ParentFindPinch"] = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }
        */

        // TODO: Fore ADC  in new command ?
        public int ForeADC(ITestItem item, string state, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog($"ForeADC Begin");

                // read 10 record to rec_data
                ReadMultiData(item, 10, 10, 10000);
                var _dut_data = rec_data;
                item.AddLog($"recv data============={_dut_data}----{state}");

                foreach (var _dut in _dut_data)
                    item.AddLog(JsonConvert.SerializeObject(_dut));

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                var _ret = ButtonFilterByName(_dut_data, "adc", "trig");
                var _ret_avg = (int)_ret.OfType<IConvertible>().Select(Convert.ToDouble).Average();
                foreAdc.AddRange(_ret);

                tmp_data[$"Fore_Trigger_ADC_Max_{state}"] = _ret.Max();
                tmp_data[$"Fore_Trigger_ADC_Min_{state}"] = _ret.Min();
                tmp_data[$"Fore_Trigger_ADC_Avg_{state}"] = _ret_avg;
                tmp_data[$"Fore_Trigger_ADC_First_{state}"] = _ret[0];
                tmp_data[$"Fore_Trigger_ADC_Last_{state}"] = _ret.Last();

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"ForeADC End");

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

        public int PinchActive(ITestItem item, string state, int value, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                item.AddLog($"Fore Pinch Active Begin");

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();
                tmp_data[$"Pinch_Button_Status_{state}"] = value;

                // update dictionary itemData wth dictionary tmp_data
                tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                // Output the results
                foreach (var entry in tmp_data)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog($"Pinch Active End");
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

        public int IfForeFindPinch(ITestItem item)
        {
            try
            {
                item.AddLog($"{item.IParent.ItemDictionary["ParentFindPinch"]}");
                return Convert.ToBoolean(item.IParent.ItemDictionary["ParentFindPinch"]) ? 0 : 1;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return 1;
            }
        }

        public int IfNotForeFindPinch(ITestItem item)
        {
            try
            {
                item.AddLog($"{item.IParent.ItemDictionary["ParentFindPinch"]}");
                return Convert.ToBoolean(item.IParent.ItemDictionary["ParentFindPinch"]) ? 1 : 0;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return 1;
            }
        }

        public int Pex_RecordNList(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            string _record_log = string.Empty;
            List<Dictionary<string, object>> _rec_data_n = new List<Dictionary<string, object>>();
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _record_log = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _record_log = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _record_log;

                string[] lines = read.Trim().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                List<string> lineList = new List<string>(lines);
                foreach (string line in lineList)
                {
                    Dictionary<string, object> _data_line_dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);
                    _rec_data_n.Add(_data_line_dict);
                }  

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                rec_data = _rec_data_n;
                item.AddLog($"record count: {_rec_data_n.Count}");
                result = _rec_data_n.Count == 10;
            }
            catch (Exception ex)
            {
                rec_data = _rec_data_n;
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:
            rec_data = _rec_data_n;
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int StartRecord(ITestItem item)
        {
            bool result = false;
            try
            {
                rec_data.Clear();
                do_rec_data = true;
                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int StopRecord(ITestItem item, int timeout)
        {
            bool result = false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                while (true)
                {
                    if (stopwatch.ElapsedMilliseconds > timeout)
                    {
                        item.AddLog($"Dut not received record data.");
                        break;
                    }
                    item.AddLog($"count: {rec_data.Count}");
                    if (rec_data.Count >= 10)
                    {
                        result = true;
                        break;
                    }

                    item.Sleep(100);
                }
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

        // todo: what's the difference between ReadMultiData and StartRecord/StopRecord
        public int ReadMultiData(ITestItem item, int delay, int count, int timeout)
        {
            bool result = false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            item.Sleep(delay);
            rec_data.Clear();
            do_rec_data = true;

            try
            {
                while (true)
                {
                    if (stopwatch.ElapsedMilliseconds > timeout)
                    {
                        item.AddLog($"Dut not received record data.");
                        break;
                    }
                    item.AddLog($"count: {rec_data.Count}");
                    if (rec_data.Count >= count)
                    {
                        result = true;
                        break;
                    }

                    item.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:
            do_rec_data = false;
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int startCommand(ITestItem item, string command)
        {
            runningTask(item, command);

            return 0;
        }

        public async Task runningTask(ITestItem item, string command)
        {
            // start async task to run command and read command response continously
            Task commandTask = RunCommandAsync(item, command, cts.Token);

            // simulate other job
            item.AddLog($"Command: {command} is running in the background...");

            // wait task completed（在这个例子中是永远不会结束的，除非你在逻辑中手动停止它）
            try
            {
                await commandTask;
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.ToString());
                // item.AddLog($"cat exception: {ex}");
            }

        }

        public Task RunCommandAsync_button(ITestItem item, string continously_command, CancellationToken token)
        {
            item.AddLog($"run command {continously_command}");
            return Task.Run(() =>
            {
                // create new process
                cmdProcess_button = new Process();

                // set up start information of process
                cmdProcess_button.StartInfo.FileName = "cmd.exe"; // run command in cmd.exe
                cmdProcess_button.StartInfo.RedirectStandardInput = true;  // 重定向输入
                cmdProcess_button.StartInfo.RedirectStandardOutput = true; // 重定向输出
                cmdProcess_button.StartInfo.RedirectStandardError = true;  // 重定向错误输出
                cmdProcess_button.StartInfo.UseShellExecute = false;       // 不使用系统外壳
                cmdProcess_button.StartInfo.CreateNoWindow = true;         // 不创建窗口

                // 注册输出数据接收事件处理
                cmdProcess_button.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        if (do_rec_data)
                        {
                            try
                            {
                                string _data_line = args.Data;
                                Dictionary<string, object> _data_line_dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(_data_line);
                                rec_data.Add(_data_line_dict);
                            }
                            catch (Exception ex)
                            {
                                //
                            }
                            
                        }
                        //item.AddLog("Output: " + args.Data); // 实时处理标准输出内容
                        //using (StreamWriter sw = new StreamWriter("C:/a.txt", append: true))
                        //{
                        //    sw.WriteLine(args.Data);
                        //}
                    }
                };

                //TODO: 注册错误数据接收事件处理时软件会异常退出，不知道到为什么
                //// 注册错误数据接收事件处理
                cmdProcess_button.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        //item.AddLog("Error: " + args.Data);
                        //Console.WriteLine("Error: " + args.Data); // 实时处理错误输出内容
                    }
                };

                // 启动进程
                cmdProcess_button.Start();

                // 开始异步读取标准输出和错误输出
                cmdProcess_button.BeginOutputReadLine();
                cmdProcess_button.BeginErrorReadLine();

                // 向命令行输入持续性命令
                cmdProcess_button.StandardInput.WriteLine(continously_command); // 使用需要持续输出的命令, ping 命令作为示例

                // 等待进程结束（此处因为是 ping -t，除非手动结束，否则不会停止）
                cmdProcess_button.WaitForExit();
            }, token);
        }

        public int TerminateChildProcesses_button(ITestItem item)
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

        // Method to stop the running process
        public int StopProcess(ITestItem item)
        {
            bool result = false;
            item.AddLog($"{cmdProcess != null}, {!cmdProcess.HasExited}");
            if (cmdProcess != null && !cmdProcess.HasExited)
            {
                item.AddLog($"ctrl + c");
                cmdProcess.StandardInput.WriteLine("\x03");
                cmdProcess.StandardInput.Close();
                Task.Delay(1000).Wait();
                //cmdProcess.StandardInput.WriteLine("\x03");
                cmdProcess.StandardInput.Close();
                Task.Delay(1000).Wait();

                if (!cmdProcess.HasExited)
                {
                    cmdProcess.Kill();
                    item.AddLog($"cmd prcess terminated forcefully");
                }

            }
            result = true;
        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int simulateTest(ITestItem item, int test_time)
        {
            bool result = false;
            item.Sleep(test_time);
            item.AddLog($"simulated {item.Title}. {test_time}ms");
            result = true;

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int wait_time(ITestItem item, int wait_time)
        {
            item.Sleep(wait_time);
            item.AddLog($"wait {item.Title}. {wait_time}ms");
            return 0;
        }

        public int Pex_GetSN(ITestItem item, string command, int snLength, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string serial_number = string.Empty;
            string read = string.Empty;
            string resultString = string.Empty;
            bool isDataOK = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _snLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _snLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _snLogs;

                if (read.Contains("assembly_sn"))
                {
                    read = GetSubString(read, "{", "}");
                    //string _remove_string = "Controller not in factory mode, setting.\r\n";
                    //if (read.Trim().StartsWith(_remove_string))
                    //{
                    //    read = read.Substring(_remove_string.Length).Trim();
                    //}
                    JObject read_json = JObject.Parse(read);
                    serial_number = read_json["assembly_sn"].ToString();
                    item.AddLog($"Serial number: {serial_number}, length: {serial_number.Length}");
                    result = CheckStringLength(item, item.Title, serial_number, snLength);
                    item.AddLog(result.ToString());
                    isDataOK = true;
                }

                if (result)
                {
                    Project.SerialNumber = serial_number;
                    Project.PathDictionary["SN"] = Project.SerialNumber;
                    Project.SideBar.TopBar.Add("SN", Project.SerialNumber);
                    // ----
                    Project.ProjectDictionary["Serial number"] = serial_number;
                    itemData.Clear();
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

            ReturnAndExit:

            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString);

            return result ? 0 : 1;
        }

        public int Pex_CheckFwVersion(ITestItem item, string command, string limitNameApp, string limitNameSPL, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            ItemLimit limit = null;
            ItemLimit limitSplVersion = null;
            bool isDataOK = false;
            try
            {
                limit = _Limits.GetLimit(limitNameApp);
                limitSplVersion = _Limits.GetLimit(limitNameSPL);

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _mainAppVersionLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _mainAppVersionLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _mainAppVersionLogs;
                // ----
                read = GetSubString(read, "{", "}");
                // ----
                if (read.Contains("Main App Version") && read.Contains("SPL Version"))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    JObject read_json = JObject.Parse(read);
                    string main_app_version = read_json["Main App Version"].ToString().SplitToList(" ")[0];
                    string spl_version = read_json["SPL Version"].ToString().SplitToList(" ")[0];

                    tmp_data[$"FW_Main_App_Version"] = main_app_version;
                    tmp_data[$"FW_SPL_Version"] = spl_version;

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        _item_result &= CheckStringLimit(item, _data.Key.ToString(), _data.Value.ToString(), _Limits.GetLimit(_data.Key));
                    }
                    result = _item_result;

                    isDataOK = true;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString, limit);

            return result ? 0 : 1;
        }

        public int Pex_CheckInputFw(ITestItem item, string command, string limitInputFw, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            ItemLimit limitInputFW = null;
            bool isDataOK = false;
            try
            {
                limitInputFW = _Limits.GetLimit(limitInputFw);

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _InputFWLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _InputFWLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _InputFWLogs;

                if (read.Contains("Main app version") && read.Contains("SPL version"))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    read = GetSubString(read, "{", "}");
                    JObject read_json = JObject.Parse(read);
                    string inputMainVersion = read_json["Main app version"].ToString();
                    string inputSPLVersion = read_json["SPL version"].ToString();

                    tmp_data[$"Input_FW_Main_App_Version"] = inputMainVersion;
                    tmp_data[$"Input_FW_SPL_Version"] = inputSPLVersion;

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        _item_result &= CheckStringLimit(item, _data.Key.ToString(), _data.Value.ToString(), _Limits.GetLimit(_data.Key));
                    }
                    result = _item_result;

                    isDataOK = true;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString, limitInputFW);

            return result ? 0 : 1;
        }

        public int Pex_GetBattery(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            bool isDataOK = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _handednessLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _handednessLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _handednessLogs;

                if (read.Contains("percentage"))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    read = GetSubString(read, "{", "}");
                    JObject read_json = JObject.Parse(read);
                    int battery = Convert.ToInt32(read_json["percentage"]);
                    tmp_data[$"Battery_Read"] = battery;

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        _item_result &= CheckNumbericLimit(item, _data.Key.ToString(), Convert.ToDouble(_data.Value), _Limits.GetLimit(_data.Key));
                        item.AddLog(_item_result.ToString());
                    }
                    result = _item_result;

                    isDataOK = true;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString);

            return result ? 0 : 1;
        }

        public int Pex_GetHandedness(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            bool isDataOK = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _handednessLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _handednessLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _handednessLogs;

                if (read.Contains("handedness"))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    read = GetSubString(read, "{", "}");
                    JObject read_json = JObject.Parse(read);
                    string handedness = read_json["handedness"].ToString();

                    tmp_data[$"Handedness_Read"] = handedness;

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        _item_result &= CheckStringLimit(item, _data.Key.ToString(), _data.Value.ToString(), _Limits.GetLimit(_data.Key));
                    }
                    result = _item_result;

                    isDataOK = true;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString);

            return result ? 0 : 1;
        }

        public int Pex_Shutdown(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            bool isDataOK = false;
            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _handednessLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    _handednessLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _handednessLogs;

                if (read.Contains("{}") )
                {
                    result = true;

                    isDataOK = true;

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

        public int Mes_CalibrationItemCheck(ITestItem item, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            try
            {
                item.AddLog($"Mes Calibration Item Check Begain");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                // Dictionary to store the mes cal data
                var mes_cal_data = new Dictionary<string, object>();

                mes_cal_data[$"TS_RANGE_X_MIN"] = "TS_Range_Calibration_X_Min_CheckMes";
                mes_cal_data[$"TS_RANGE_X_MAX"] = "TS_Range_Calibration_X_Max_CheckMes";
                mes_cal_data[$"TS_RANGE_Y_MIN"] = "TS_Range_Calibration_Y_Min_CheckMes";
                mes_cal_data[$"TS_RANGE_Y_MAX"] = "TS_Range_Calibration_Y_Max_CheckMes";

                mes_cal_data[$"TS_DEADBAND_X_MIN"] = "TS_Deadband_Calibration_X_Min_CheckMes";
                mes_cal_data[$"TS_DEADBAND_X_MAX"] = "TS_Deadband_Calibration_X_Max_CheckMes";
                mes_cal_data[$"TS_DEADBAND_Y_MIN"] = "TS_Deadband_Calibration_Y_Min_CheckMes";
                mes_cal_data[$"TS_DEADBAND_Y_MAX"] = "TS_Deadband_Calibration_Y_Max_CheckMes";

                mes_cal_data[$"GRIP_RANGE_MIN"] = "Grip_Trigger_Calibration_Range_Min_CheckMes";
                mes_cal_data[$"GRIP_RANGE_MAX"] = "Grip_Trigger_Calibration_Range_Max_CheckMes";

                item.AddLog($"all mes data");
                foreach (var _mes_data in mesData)
                {
                    item.AddLog($"{_mes_data.Key}: {_mes_data.Value}");
                }
                item.AddLog($"all mes data end");

                if (Project.IsOnLine)
                {
                    item.AddLog($"Mes Calibration Item Check Online");
                    foreach (var _data in mesData)
                    {
                        try
                        {
                            var mes_ret_bind = BindAndUpdateSnAndNotMac(item, _data.Key.ToString(), _data.Value.ToString());
                            item.AddLog($"mes_ret_bind: {mes_ret_bind}");
                            item.Sleep(100);
                            var mes_ret_get_mac_by_sn = GetMacBySn(item, _data.Key.ToString());
                            item.AddLog($"mes_ret_get_mac_by_sn: {mes_ret_get_mac_by_sn}");
                            if (mes_ret_get_mac_by_sn == _data.Value.ToString())
                            {
                                item.AddLog($"mes_write: {_data.Key}-{_data.Value}---- OK");
                                tmp_data[$"{mes_cal_data[_data.Key.ToString()]}"] = mes_ret_get_mac_by_sn;
                            }
                            else
                            {
                                item.AddLog($"mes_write: {_data.Key}-{_data.Value}---- NG");
                                tmp_data[$"{mes_cal_data[_data.Key.ToString()]}"] = mes_ret_get_mac_by_sn;
                            }
                        }
                        catch (Exception ex)
                        {
                            tmp_data[$"{mes_cal_data[_data.Key]}"] = "Error";
                            item.AddLog($"mes_write: {_data.Key}-{_data.Value}---- Error");
                        }
                    }

                    // TODO: add check mesDataFFT
                    foreach (var _data_fft in mesDataFFT)
                    {
                        try
                        {  
                            var mes_ret_bind_fft = BindAndUpdateSnAndNotMac(item, _data_fft.Key.ToString(), _data_fft.Value.ToString().Trim());
                            item.AddLog($"mes_ret_bind_fft: {mes_ret_bind_fft}");
                            item.Sleep(100);
                            var mes_ret_get_mac_by_sn_fft = GetMacBySn(item, _data_fft.Key.ToString());
                            item.AddLog($"mes_ret_get_mac_by_sn_fft: {mes_ret_get_mac_by_sn_fft}");
                            if (mes_ret_get_mac_by_sn_fft == _data_fft.Value.ToString())
                            {
                                item.AddLog($"mes_write: {_data_fft.Key}-{_data_fft.Value}---- OK");
                                //tmp_data[$"{mes_cal_data[_data.Key.ToString()]}"] = mes_ret_get_mac_by_sn;
                            }
                            else
                            {
                                item.AddLog($"mes_write: {_data_fft.Key}-{_data_fft.Value}---- NG");
                                //tmp_data[$"{mes_cal_data[_data.Key.ToString()]}"] = mes_ret_get_mac_by_sn;
                            }
                        }
                        catch (Exception ex)
                        {
                            //tmp_data[$"{mes_cal_data[_data_fft.Key]}"] = "Error";
                            item.AddLog($"mes_write: {_data_fft.Key}-{_data_fft.Value}---- Error");
                        }
                    }

                    // Output the results
                    foreach (var entry in tmp_data)
                    {
                        item.AddLog($"{entry.Key}: {entry.Value}");
                    }

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    if (afterWaiting > 0)
                        item.Sleep(afterWaiting);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        _item_result &= CheckStringLimit(item, _data.Key.ToString(), _data.Value.ToString(), _Limits.GetLimit(_data.Key));
                        item.AddLog(_item_result.ToString());
                    }
                    result = _item_result;
                }
                else
                {
                    item.AddLog($"Mes Calibration Item Check Offline (Not Check)");
                    result = true;
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }
            item.AddLog($"Mes Calibration Item Check End");

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int Pex_WriteThumbStickRangeCal(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                item.AddLog("Write Thumbstick Range Calibration Begin");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                var xMax = itemData["Thumbstick_Range_Cal_X_Axis_ADC_Max"];
                var xMin = itemData["Thumbstick_Range_Cal_X_Axis_ADC_Min"];
                var yMax = itemData["Thumbstick_Range_Cal_Y_Axis_ADC_Max"];
                var yMin = itemData["Thumbstick_Range_Cal_Y_Axis_ADC_Min"];

                if (!string.IsNullOrEmpty(command))
                {
                    command = command.Replace("x_min", xMin.ToString()).Replace("x_max", xMax.ToString()).Replace("y_min", yMin.ToString()).Replace("y_max", yMax.ToString());
                    read = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                // ----
                read = GetSubString(read, "{", "}");
                // ----

                result = read.Trim() == "{}";


                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog("Write Thumbstick Range Calibration End");
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

        public int Pex_ReadThumbStickRangeCal(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                item.AddLog("Read Thumbstick Range Calibration Begin");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    read = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                // ----
                // read = GetSubString(read, "{", "}");
                // item.AddLog($"{read}");
                // ----
                if (!string.IsNullOrEmpty(read))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    JObject read_json = JObject.Parse(read);
                    // string readThumbStickRangeRet = read_json["Status"].ToString();

                    var xMinRead = read_json["range_x"]["min"];
                    var xMaxRead = read_json["range_x"]["max"];
                    var yMinRead = read_json["range_y"]["min"];
                    var yMaxRead = read_json["range_y"]["max"];
                    item.AddLog($"Read Thumbstick RangeCal range_x_min: {xMinRead}");
                    item.AddLog($"Read Thumbstick RangeCal range_x_max: {xMaxRead}");
                    item.AddLog($"Read Thumbstick RangeCal range_y_min: {yMinRead}");
                    item.AddLog($"Read Thumbstick RangeCal range_y_maz: {yMaxRead}");

                    tmp_data["TS_Range_Calibration_X_Min"] = xMinRead;
                    tmp_data["TS_Range_Calibration_X_Max"] = xMaxRead;
                    tmp_data["TS_Range_Calibration_Y_Min"] = yMinRead;
                    tmp_data["TS_Range_Calibration_Y_Max"] = yMaxRead;

                    var xMax = itemData["Thumbstick_Range_Cal_X_Axis_ADC_Max"];
                    var xMin = itemData["Thumbstick_Range_Cal_X_Axis_ADC_Min"];
                    var yMax = itemData["Thumbstick_Range_Cal_Y_Axis_ADC_Max"];
                    var yMin = itemData["Thumbstick_Range_Cal_Y_Axis_ADC_Min"];
                    item.AddLog($"Thumbstick_Range_Cal_X_Axis_ADC_Max: {xMax}");
                    item.AddLog($"Thumbstick_Range_Cal_X_Axis_ADC_Min: {xMin}");
                    item.AddLog($"Thumbstick_Range_Cal_Y_Axis_ADC_Max: {yMax}");
                    item.AddLog($"Thumbstick_Range_Cal_Y_Axis_ADC_Min: {yMin}");

                    if (Convert.ToInt32(xMin) == Convert.ToInt32(xMinRead))
                    {
                        item.AddLog($"TS_Range_Calibration_X_Min_CheckLocal Pass");
                        tmp_data["TS_Range_Calibration_X_Min_CheckLocal"] = "Pass";
                    }  
                    else
                    {
                        item.AddLog($"TS_Range_Calibration_X_Min_CheckLocal Fail");
                        tmp_data["TS_Range_Calibration_X_Min_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(xMax) == Convert.ToInt32(xMaxRead))
                    {
                        item.AddLog($"TS_Range_Calibration_X_Max_CheckLocal Pass");
                        tmp_data["TS_Range_Calibration_X_Max_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Range_Calibration_X_Max_CheckLocal Fail");
                        tmp_data["TS_Range_Calibration_X_Max_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(yMin) == Convert.ToInt32(yMinRead))
                    {
                        item.AddLog($"TS_Range_Calibration_Y_Min_CheckLocal Pass");
                        tmp_data["TS_Range_Calibration_Y_Min_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Range_Calibration_Y_Min_CheckLocal Fail");
                        tmp_data["TS_Range_Calibration_Y_Min_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(yMax) == Convert.ToInt32(yMaxRead))
                    {
                        item.AddLog($"TS_Range_Calibration_Y_Max_CheckLocal Pass");
                        tmp_data["TS_Range_Calibration_Y_Max_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Range_Calibration_Y_Max_CheckLocal Fail");
                        tmp_data["TS_Range_Calibration_Y_Max_CheckLocal"] = "Fail";
                    }

                    // update dictionary mesData
                    mesData["TS_RANGE_X_MIN"] = xMinRead.ToString();
                    mesData["TS_RANGE_X_MAX"] = xMaxRead.ToString();
                    mesData["TS_RANGE_Y_MIN"] = yMinRead.ToString();
                    mesData["TS_RANGE_Y_MAX"] = yMaxRead.ToString();

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        if (_data.Key.ToString().Contains("CheckLocal"))
                        {
                            _item_result &= CheckStringLimit(item, $"{_data.Key}", _data.Value.ToString(), _Limits.GetLimit($"{_data.Key}"));
                        }
                    }
                    result = _item_result;

                    return result ? 0 : 1;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog("Read Thumbstick Range Calibration End");
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            string _item_title = "Write_Thumbstick_Range_Calibration";
            ResultData data = new ResultData(_item_title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int Pex_WriteThumbStickDeadbandDelta(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                item.AddLog("Write Thumbstick Deadband Delta Calibration Begin");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                // var center02_avg_x = itemData["Thumbstick_Deadband_X_Axis_ADC_Avg_After_2"];
                // var center02_avg_y = itemData["Thumbstick_Deadband_Y_Axis_ADC_Avg_After_2"];
                item.AddLog($"deadbandXAdcAvg List:");
                foreach (var _adc_avg_x in deadbandXAdcAvg)
                {
                    item.AddLog($"{_adc_avg_x}");
                }
                item.AddLog($"deadbandYAdcAvg List:");
                foreach (var _adc_avg_y in deadbandYAdcAvg)
                {
                    item.AddLog($"{_adc_avg_y}");
                }

                var x_min = deadbandXAdcAvg.Min();
                var x_max = deadbandXAdcAvg.Max();
                var y_min = deadbandYAdcAvg.Min();
                var y_max = deadbandYAdcAvg.Max();

                // deadbandXAdcAvg.Add(center02_avg_x);
                // deadbandYAdcAvg.Add(center02_avg_y);

                // x_max = deadbandXAdcAvg.Max();
                // x_min = deadbandXAdcAvg.Min();
                // y_max = deadbandYAdcAvg.Max();
                // y_min = deadbandYAdcAvg.Min();

                if (!string.IsNullOrEmpty(command))
                {
                    command = command.Replace("x_min", x_min.ToString()).Replace("x_max", x_max.ToString()).Replace("y_min", y_min.ToString()).Replace("y_max", y_max.ToString());
                    read = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                // ----
                read = GetSubString(read, "{", "}");
                // ----

                result = read.Trim() == "{}";


                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog("Write Thumbstick Deadband Delta Calibration End");
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

        public int Pex_ReadThumbStickDeadbandCal(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                item.AddLog("Read Thumbstick Deadband Calibration Begin");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    read = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                // ----
                // read = GetSubString(read, "{", "}");
                // item.AddLog($"{read}");
                // ----
                if (!string.IsNullOrEmpty(read))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    JObject read_json = JObject.Parse(read);
                    // string readThumbStickRangeRet = read_json["Status"].ToString();

                    var xMinRead = read_json["deadband_x"]["min"];
                    var xMaxRead = read_json["deadband_x"]["max"];
                    var yMinRead = read_json["deadband_y"]["min"];
                    var yMaxRead = read_json["deadband_y"]["max"];
                    item.AddLog($"Read Thumbstick Deadband range_x_min: {xMinRead}");
                    item.AddLog($"Read Thumbstick Deadband range_x_max: {xMaxRead}");
                    item.AddLog($"Read Thumbstick Deadband range_y_min: {yMinRead}");
                    item.AddLog($"Read Thumbstick Deadband range_y_maz: {yMaxRead}");

                    var xMin = deadbandXAdcAvg.Min();
                    var xMax = deadbandXAdcAvg.Max();
                    var yMin = deadbandYAdcAvg.Min();
                    var yMax = deadbandYAdcAvg.Max();

                    item.AddLog($"Thumbstick_Deadband_X_Axis_ADC_Max: {xMax}");
                    item.AddLog($"Thumbstick_Deadband_X_Axis_ADC_Min: {xMin}");
                    item.AddLog($"Thumbstick_Deadband_Y_Axis_ADC_Max: {yMax}");
                    item.AddLog($"Thumbstick_Deadband_Y_Axis_ADC_Min: {yMin}");

                    tmp_data["TS_Deadband_Calibration_X_Min"] = Convert.ToInt32(xMinRead);
                    tmp_data["TS_Deadband_Calibration_X_Max"] = Convert.ToInt32(xMaxRead);
                    tmp_data["TS_Deadband_Calibration_Y_Min"] = Convert.ToInt32(yMinRead);
                    tmp_data["TS_Deadband_Calibration_Y_Max"] = Convert.ToInt32(yMaxRead);

                    if (Convert.ToInt32(xMin) == Convert.ToInt32(xMinRead))
                    {
                        item.AddLog($"TS_Deadband_Calibration_X_Min_CheckLocal Pass");
                        tmp_data["TS_Deadband_Calibration_X_Min_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Deadband_Calibration_X_Min_CheckLocal Fail");
                        tmp_data["TS_Deadband_Calibration_X_Min_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(xMax) == Convert.ToInt32(xMaxRead))
                    {
                        item.AddLog($"TS_Deadband_Calibration_X_Max_CheckLocal Pass");
                        tmp_data["TS_Deadband_Calibration_X_Max_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Deadband_Calibration_X_Max_CheckLocal Fail");
                        tmp_data["TS_Deadband_Calibration_X_Max_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(yMin) == Convert.ToInt32(yMinRead))
                    {
                        item.AddLog($"TS_Deadband_Calibration_Y_Min_CheckLocal Pass");
                        tmp_data["TS_Deadband_Calibration_Y_Min_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Deadband_Calibration_Y_Min_CheckLocal Fail");
                        tmp_data["TS_Deadband_Calibration_Y_Min_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(yMax) == Convert.ToInt32(yMaxRead))
                    {
                        item.AddLog($"TS_Deadband_Calibration_Y_Max_CheckLocal Pass");
                        tmp_data["TS_Deadband_Calibration_Y_Max_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"TS_Deadband_Calibration_Y_Max_CheckLocal Fail");
                        tmp_data["TS_Deadband_Calibration_Y_Max_CheckLocal"] = "Fail";
                    }

                    tmp_data["Thumbstick_Deadband_Cal_X_Delta_2"] = Convert.ToInt32(xMaxRead) - Convert.ToInt32(xMinRead);
                    tmp_data["Thumbstick_Deadband_Cal_Y_Delta_2"] = Convert.ToInt32(yMaxRead) - Convert.ToInt32(yMinRead);
                    tmp_data["Thumbstick_Deadband_Cal_X_Delta"] = Convert.ToInt32(xMax) - Convert.ToInt32(xMin);
                    tmp_data["Thumbstick_Deadband_Cal_Y_Delta"] = Convert.ToInt32(yMax) - Convert.ToInt32(yMin);

                    // update dictinary mesData
                    mesData["TS_DEADBAND_X_MIN"] = xMinRead.ToString();
                    mesData["TS_DEADBAND_X_MAX"] = xMaxRead.ToString();
                    mesData["TS_DEADBAND_Y_MIN"] = yMinRead.ToString();
                    mesData["TS_DEADBAND_Y_MAX"] = yMaxRead.ToString();

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        if (_data.Key.ToString().Contains("CheckLocal") || (_data.Key.ToString().Contains("TS_Deadband_Calibration")))
                        {
                            _item_result &= CheckStringLimit(item, $"{_data.Key}", _data.Value.ToString(), _Limits.GetLimit($"{_data.Key}"));
                        }
                    }
                    result = _item_result;

                    return result ? 0 : 1;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog("Read Thumbstick Deadband Calibration End");
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            string _item_title = "Read_Thumbstick_Deadband_Calibration";
            ResultData data = new ResultData(_item_title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int Pex_WriteGripTriggerCal(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                item.AddLog("Write Grip Trigger Calibration Begin");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                // Dictionary to store the results
                var tmp_data = new Dictionary<string, object>();

                List<int> _ret = new List<int>() { Convert.ToInt32(itemData["Grip_Trigger_ADC_Avg_Press"]), Convert.ToInt32(itemData["Grip_Trigger_ADC_Avg_Release"]) };
                var _max = itemData["Grip_Trigger_ADC_Max_Press"];
                var _min = itemData["Grip_Trigger_ADC_Min_Release"];
                tmp_data["Grip_Trigger_Max_Min_Delta"] = Math.Abs(Convert.ToInt32(_max) - Convert.ToInt32(_min));

                if (!string.IsNullOrEmpty(command))
                {
                    command = command.Replace("min_value", _ret.Min().ToString()).Replace("max_value", _ret.Max().ToString());
                    read = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                // ----
                read = GetSubString(read, "{", "}");
                // ----

                result = read.Trim() == "{}";

                bool _item_result = true;
                foreach (var _data in tmp_data)
                {
                    item.AddLog($"{_data.Key}: {_data.Value}");
                    _item_result &= CheckNumbericLimit(item, _data.Key.ToString(), Convert.ToDouble(_data.Value), _Limits.GetLimit(_data.Key));
                    item.AddLog(_item_result.ToString());
                }
                result = _item_result;

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog("Write Grip Trigger Calibration End");
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

        public int Pex_ReadGripTriggerCal(ITestItem item, string command, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            try
            {
                item.AddLog("Read Grip Trigger Calibration Begin");

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    read = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, "D:\\Python", timeOut, ref read);
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                // ----
                // read = GetSubString(read, "{", "}");
                // item.AddLog($"{read}");
                // ----
                if (!string.IsNullOrEmpty(read))
                {
                    // Dictionary to store the results
                    var tmp_data = new Dictionary<string, object>();

                    JObject read_json = JObject.Parse(read);
                    // string readThumbStickRangeRet = read_json["Status"].ToString();

                    var gripMinRead = read_json["grip"]["min"];
                    var gripMaxRead = read_json["grip"]["max"];
                    item.AddLog($"Read Grip Trigger min: {gripMinRead}");
                    item.AddLog($"Read Grip Trigger max: {gripMaxRead}");

                    tmp_data["Grip_Trigger_Calibration_Range_Min"] = Convert.ToInt32(gripMinRead);
                    tmp_data["Grip_Trigger_Calibration_Range_Max"] = Convert.ToInt32(gripMaxRead);

                    List<int> _ret = new List<int>() { Convert.ToInt32(itemData["Grip_Trigger_ADC_Avg_Press"]), Convert.ToInt32(itemData["Grip_Trigger_ADC_Avg_Release"]) };
                    var _max = itemData["Grip_Trigger_ADC_Max_Press"];
                    var _min = itemData["Grip_Trigger_ADC_Min_Release"];

                    item.AddLog($"Grip_Trigger_ADC_Max_Press: {_max}");
                    item.AddLog($"Grip_Trigger_ADC_Min_Release: {_min}");


                    if (Convert.ToInt32(gripMinRead) == Convert.ToInt32(_ret.Min()))
                    {
                        item.AddLog($"Grip_Trigger_Calibration_Range_Min_CheckLocal Pass");
                        tmp_data["Grip_Trigger_Calibration_Range_Min_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"Grip_Trigger_Calibration_Range_Min_CheckLocal Fail");
                        tmp_data["Grip_Trigger_Calibration_Range_Min_CheckLocal"] = "Fail";
                    }

                    if (Convert.ToInt32(gripMaxRead) == Convert.ToInt32(_ret.Max()))
                    {
                        item.AddLog($"Grip_Trigger_Calibration_Range_Max_CheckLocal Pass");
                        tmp_data["Grip_Trigger_Calibration_Range_Max_CheckLocal"] = "Pass";
                    }
                    else
                    {
                        item.AddLog($"Grip_Trigger_Calibration_Range_Max_CheckLocal Fail");
                        tmp_data["Grip_Trigger_Calibration_Range_Max_CheckLocal"] = "Fail";
                    }

                    // update dictinary mesData
                    mesData["GRIP_RANGE_MIN"] = gripMinRead.ToString();
                    mesData["GRIP_RANGE_MAX"] = gripMaxRead.ToString();

                    // update dictionary itemData wth dictionary tmp_data
                    tmp_data.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                    bool _item_result = true;
                    foreach (var _data in tmp_data)
                    {
                        item.AddLog($"{_data.Key}: {_data.Value}");
                        if (_data.Key.ToString().Contains("CheckLocal"))
                        {
                            _item_result &= CheckStringLimit(item, $"{_data.Key}", _data.Value.ToString(), _Limits.GetLimit($"{_data.Key}"));
                        }
                    }
                    result = _item_result;

                    return result ? 0 : 1;

                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                item.AddLog("Read Grip Trigger Calibration End");
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            string _item_title = "Read_Grip_Trigger_Calibration";
            ResultData data = new ResultData(_item_title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int Pex_FFT_Test(ITestItem item, string command, string working_dir, string fftResultFolder, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
            string resultString = string.Empty;
            bool isDataOK = false;
            try
            {

                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                if (!string.IsNullOrEmpty(command))
                {
                    _fftLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "python.exe", command, working_dir, timeOut, ref read);
                    _fftLogs = read;
                    item.AddLog("read = " + read);

                    // copy fft csv to tmpfiles, it will back up to TestLog ...
                    fftResultFile = GetAllFilesRecursively(fftResultFolder)[0];
                    item.AddLog($"fft result file path: {fftResultFile}");
                    FileInfo fi = new FileInfo(Path.Combine(_Context.BackupFolder, Path.GetFileName(fftResultFile)));
                    if (!fi.Directory.Exists)
                        fi.Directory.Create();
                    File.Copy(fftResultFile, fi.FullName, overwrite: true);

                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _fftLogs;

                result = read.Contains("SUCCESS");

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

        public int ClearFFTResult(ITestItem item, string fftResultFolder)
        {
            bool result = false;

            try
            {
                result = ClearDirectory(fftResultFolder);
                item.AddLog($"Clear FFT Result Folder {fftResultFolder}: {result}");
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

        public int AnalysisFFTResult(ITestItem item, string fftResultFolder, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {

            bool result = false;
            var  targetKeys = new [] { "Force Zero", "Trig Out", "Pos_Min_PWM", "Neg_Min_PWM", "Cam Out", "Cam In", "Trig In", "Cam Travel", "trig_cam_coex_lut", "trig_cam_coex_shift", "trig_to_cam_cal_generated", "force_cal", "motor_off_backdrive_mN", "spring_out_nmm", "spring_in_nmm", "PASS" };
            var targetKeysMes = new[] { "Cam Out", "Cam Travel", "trig_cam_coex_lut", "trig_cam_coex_shift", "trig_to_cam_cal_generated", "force_cal" };

            try
            {
                if (preWaiting > 0)
                    item.Sleep(preWaiting);

                if (item.RetriedTime > 0 && retryWaiting > 0)
                    item.Sleep(retryWaiting);

                fftResultFile = GetAllFilesRecursively(fftResultFolder)[0];
                item.AddLog($"fft result file path: {fftResultFile}");

                // update dictionary mesDataFFT
                Dictionary<string, object> fftMesCalData = ParseCsvToDictionaryWithKeySelected(fftResultFile, targetKeysMes);
                foreach (var key in  fftMesCalData.Keys.ToList())
                {
                    if (fftMesCalData[key] is List<object> fftListData)
                    {
                        fftMesCalData[key] = String.Join("_", fftListData);
                    }
                    item.AddLog($"{key}: {fftMesCalData[key]}");
                }

                mesDataFFT["FFTCAMOUT"] = fftMesCalData["Cam Out"];
                mesDataFFT["FFTCAMTRAVEL"] = fftMesCalData["Cam Travel"];
                mesDataFFT["FFTTRIGCAMCOEXLUT"] = fftMesCalData["trig_cam_coex_lut"];
                mesDataFFT["FFTTRIGCAMCOEXSHIFT"] = fftMesCalData["trig_cam_coex_shift"];
                mesDataFFT["FFTTRIGTOCAMCAL"] = fftMesCalData["trig_to_cam_cal_generated"];
                mesDataFFT["FFTFORCECAL"] = fftMesCalData["force_cal"];
                // Output fft mes data
                foreach (var _mes_fft_data in mesDataFFT)
                {
                    item.AddLog($"{_mes_fft_data.Key}: {_mes_fft_data.Value}");
                }

                Dictionary<string, object> fftItemResult = ParseCsvToDictionaryWithKeySelected(fftResultFile, targetKeys);
                // list to string format
                foreach (var key in fftItemResult.Keys.ToList())
                {
                    if (fftItemResult[key] is List<object> fftListItemResult)
                    {
                        fftItemResult[key] = String.Join("_", fftListItemResult);
                    }
                    item.AddLog($"{key}: {fftItemResult[key]}");
                }

                // Output the results
                foreach (var entry in fftItemResult)
                {
                    item.AddLog($"{entry.Key}: {entry.Value}");
                }

                // update dictionary itemData wth dictionary tmp_data
                fftItemResult.ToList().ForEach(kvp => itemData[kvp.Key] = kvp.Value);

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);

                bool _item_result = true;
                foreach (var _data in fftItemResult)
                {
                    item.AddLog($"{_data.Key}: {_data.Value}");
                    if (_data.Key.ToString().Contains("_"))
                    {
                        _item_result &= CheckStringLimit(item, _data.Key.ToString(), _data.Value.ToString(), _Limits.GetLimit(_data.Key));
                    }
                    else if (_data.Key.ToString().ToUpper() != "PASS")
                    {
                        _item_result &= CheckNumbericLimit(item, _data.Key.ToString(), Convert.ToDouble(_data.Value), _Limits.GetLimit(_data.Key));
                    }
                    else
                    {
                        item.AddLog($"{_data.Key}, {_data.Value}, {_Limits.GetLimit("fft_cal_result")}");
                        _item_result &= CheckStringLimit(item, "fft_cal_result", _data.Value.ToString().ToUpper(), _Limits.GetLimit("fft_cal_result"));
                    }
                    item.AddLog(_item_result.ToString());
                }
                result = _item_result;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                ResultData ex_data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
                AddResult(item, ex_data);
            }
        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int ADB_CheckSyncbossVersion(ITestItem item, string command, string limitName, string keyword, int timeOut = 20000, int preWaiting = 0, int afterWaiting = 0, int retryWaiting = 0)
        {
            bool result = false;
            string read = string.Empty;
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

                if (!string.IsNullOrEmpty(command))
                {
                    _syncBossLogs = string.Empty;
                    bool isOK = ShellHelper.RunHideRead(item.AddLog, "adb.exe", ADBSN(command), timeOut, ref read);
                    _syncBossLogs = read;
                    item.AddLog("read = " + read);
                    if (!isOK)
                        goto ReturnAndExit;
                }
                else
                    read = _syncBossLogs;

                var lines = read.SplitToList("\r\n");

                var line = lines.FirstOrDefault(l => l.Contains(keyword));
                if (line != null)
                {
                    line = line.Substring(line.IndexOf(keyword) + keyword.Length);
                    resultString = line.Substring(0, line.IndexOf("Built")).Trim();
                    result = CheckStringLimit(item, item.Title, resultString, limit);
                    isDataOK = true;
                }

                if (afterWaiting > 0)
                    item.Sleep(afterWaiting);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }


        ReturnAndExit:

            if (!isDataOK)
                AddFailedStringResult(item, item.Title, resultString, limit);

            return result ? 0 : 1;
        }
    }
}
