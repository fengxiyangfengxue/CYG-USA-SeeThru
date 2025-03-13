using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Test._App;
using UserHelpers.Helpers;
using System.Windows.Media;
using System.Data;
using System.Reflection;
using System.Xml.Linq;
using Test._ScriptExtensions;
using Test._Definitions;
using Test._ScriptHelpers;
using System.Text.RegularExpressions;
using MetaHelpers.ScriptHelpers;
using Test.StationsScripts.Shared;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;


namespace Test
{
    public partial class MainClass
    {

        public static AutoResetEvent _ScanOPIDEvent = new AutoResetEvent(true);
        public static AutoResetEvent _SelectModeEvent = new AutoResetEvent(true);

        void RefreshTopBar()
        {
            Project.SideBar.TopBar.Add("PassFailCount", "P : " + _Context.PassCount + "  /", "  F : " + _Context.FailCount, 14, 14, Colors.Green, Colors.Red);
            Project.SideBar.TopBar.Add("Ver", _buildSetting.Version);
        }


        string ADBSN(string args)
        {
            if (!string.IsNullOrEmpty(_Context.ADBSN))
                return " -s " + _Context.ADBSN + " " + args;
            return args;
        }

        void Select_TestMode()
        {
            try
            {
                _SelectModeEvent.WaitOne();
                if (TestContext.SelectedTestMode == Definition.Test_Mode.IDLE)
                { 
                    TestModeForm form = new TestModeForm(Project);
                    form.ShowDialog();
                    TestContext.SelectedTestMode = form.SelectedMode; 
                }
                _Config.TestMode = TestContext.SelectedTestMode;

            }
            finally
            {
                _SelectModeEvent.Set();
            }
        }


        void Input_OPID()
        {
            try
            {
                _ScanOPIDEvent.WaitOne();
                if (_Context.OperatorID.Length != _commonSetting.OPIDLength)
                {
                    BarCodeConfig opConfig = new BarCodeConfig()
                    {
                        Title = "Input " + _commonSetting.OPIDLength + " chars OPID：",
                        MakeUpper = false
                    };
                    opConfig.ValidationHandler += delegate (string s) { return s.Length == _commonSetting.OPIDLength; };

                    _Context.OperatorID = BarCodeHelper.Get(Project.AppWindow, opConfig);

                    Project.RunningProjects.ForEach(p =>
                    {
                        if (p != Project)
                            p.GetInstance<MainClass>()._Context.OperatorID = _Context.OperatorID;
                    });
                }
            }
            finally
            {
                _ScanOPIDEvent.Set();
            }
        }

        bool IsLooping()
        {
            return File.Exists("loop.txt");
        }

        string GetLoopingSN(int snLine)
        {
            string sn = string.Empty;

            if (File.Exists("loop.txt"))
            {
                var lines = File.ReadAllLines("loop.txt").ToList();

                if (lines.Count() > snLine)
                    sn = lines[snLine].Trim();
            }

            return sn;
        }

        ECData CreateErrorCode(string title)
        {
            var ec = new ECData() { Name = title, ErrorCode = CRC32Helper.GetCRC32(title).ToUpper(), ErrorDescription = title };
            Project.AddErrorCode(ec);
            return ec;
        }

        string CreateErrorCode(bool result, string title)
        {
            var ec = new ECData() { Name = title, ErrorCode = CRC32Helper.GetCRC32(title).ToUpper(), ErrorDescription = title };
            Project.AddErrorCode(ec);
            return result ? "" : ec.Name;
        }

        void AddResult(ITestItem item, ResultData data)
        {
            item.AddResultData(data);
        }

        void AddFailedResult(ITestItem item, string title, string ecName, string value, ItemLimit limit, string errMsg = "")
        {
            if (limit == null)
                limit = new ItemLimit();

            ResultData data = new ResultData() { TestName = title, ECName = ecName, Unit = limit.Unit, Message = errMsg };
            try
            {
                data.Value = value;
                if (limit.UCL == null && limit.LCL == null)
                {

                }
                else if (limit.UCL == null)
                {
                    data.LowerLimit = limit.LCL.ToString();
                }
                else if (limit.LCL == null)
                {
                    data.UpperLimit = limit.UCL.ToString();
                }
                else
                {
                    data.UpperLimit = limit.UCL.ToString();
                    data.LowerLimit = limit.LCL.ToString();
                }
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }
            data.Message = errMsg;
            AddResult(item, data);
        }

        bool CheckLimit(ITestItem item, string title, string ecName, double? value, ItemLimit limit)
        {
            bool result = false;
            if (limit == null)
                limit = new ItemLimit();
            ResultData data = new ResultData() { TestName = title, Unit = limit.Unit };

            try
            {
                if (value == null)
                    data.Value = null;
                else
                    data.Value = value.ToString();

                if (limit.UCL == null && limit.LCL == null)
                {
                    result = (value != null);
                }
                else if (limit.UCL == null)
                {
                    data.LowerLimit = limit.LCL.ToString();

                    if (value == null)
                        result = false;
                    else
                        result = limit.LCLClosedInterval ? value >= limit.LCL : value > limit.LCL;
                }
                else if (limit.LCL == null)
                {
                    data.UpperLimit = limit.UCL.ToString();
                    if (value == null)
                        result = false;
                    else
                        result = limit.UCLClosedInterval ? value <= limit.UCL : value < limit.UCL;
                }
                else
                {
                    data.UpperLimit = limit.UCL.ToString();
                    data.LowerLimit = limit.LCL.ToString();
                    if (value == null)
                        result = false;
                    else
                        result = (limit.LCLClosedInterval ? value >= limit.LCL : value > limit.LCL) && (limit.UCLClosedInterval ? value <= limit.UCL : value < limit.UCL);
                }

            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }


            data.ECName = result ? "" : ecName;
            AddResult(item, data);
            return result;
        }

        bool CheckStringLength(ITestItem item, string title, string value, int length)
        {
            bool result = false;
            ResultData data = new ResultData() { TestName = title, Value = value };
            try
            {
                result = value.Length == length;

            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

            data.ECName = CreateErrorCode(result, title);
            AddResult(item, data);
            return result;
        }

        string GetSubString(string mainString, string startString, string endString)
        {
            string result = string.Empty;
            try
            {
                int startIndex = mainString.IndexOf(startString);
                int endIndex = mainString.IndexOf(endString, startIndex + startString.Length);
                if (startIndex != -1 && endIndex != -1)
                {
                    endIndex += endString.Length;
                    result = mainString.Substring(startIndex, endIndex - startIndex);
                }
            }
            catch
            {
                result = string.Empty;
            }
            return result;
        }

        public static List<T> GetLastNList<T>(List<T> list, int n)
        {
            try
            {
                int count = list.Count;
                return list.GetRange(Math.Max(0, count - n), Math.Min(n, count));
            }
            catch
            {
                return new List<T>();
            }
        }

        public static List<T> ButtonFilterByName<T>(List<Dictionary<string, T>> list, string dataType, string buttonName)
        {
            var filteredValues = new List<T>();
            try
            {
                foreach(var line in list)
                {
                    if (line.ContainsKey(dataType))
                    {
                        object dataValue = line[dataType];
                        if (dataValue is JObject valueDic && valueDic.ContainsKey(buttonName))
                        {

                            object nameValue = valueDic[buttonName];
                            if (nameValue is T nameValueInt)
                            {
                                filteredValues.Add(nameValueInt);
                             }
                        }
                    }
                }
                return filteredValues;
            }
            catch
            {
                return filteredValues;
            }
        }

        public static double CalADC(List<int> x_adc, List<int> y_adc)
        {
            if (x_adc.Count <= 1 || y_adc.Count <= 1)
                return 0;

            List<double> distances = new List<double>();
            double lastX = x_adc[0];
            double lastY = y_adc[0];

            for (int i = 1; i < x_adc.Count && i < y_adc.Count; i++)
            {
                double x = x_adc[i];
                double y = y_adc[i];
                double distance = Math.Sqrt(Math.Pow(x - lastX, 2) + Math.Pow(y - lastY, 2));
                distances.Add(distance);
                lastX = x;
                lastY = y;
            }

            return distances.Sum() / distances.Count;
        }

        bool CheckStringList(ITestItem item, string title, string value, List<string> string_list)
        {
            bool result = false;
            ResultData data = new ResultData() { TestName = title, Value = value };
            try
            {
                result = string_list.Contains(value);

            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

            data.ECName = CreateErrorCode(result, title);
            AddResult(item, data);
            return result;
        }

        bool CheckStringLimit(ITestItem item, string title, string value, ItemLimit limit = null)
        {
            bool result = false;
            if (limit == null)
                limit = new ItemLimit();
            ResultData data = new ResultData() { TestName = title, Value = value, Unit = limit.Unit };
            try
            {
                //没有配置limit或者CheckString不比较
                if (string.IsNullOrEmpty(limit.CheckString))
                {
                    result = true;
                }
                else
                {
                    result = value.Equals(limit.CheckString);
                    data.UpperLimit = limit.CheckString;
                    data.LowerLimit = limit.CheckString;
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

            data.ECName = CreateErrorCode(result, title);
            AddResult(item, data);
            return result;
        }

        bool AddFailedStringResult(ITestItem item, string title, string value, ItemLimit limit = null)
        {
            bool result = false;
            if (limit == null)
                limit = new ItemLimit();
            ResultData data = new ResultData() { TestName = title, Value = value, Unit = limit.Unit };
            try
            {
                data.UpperLimit = limit.CheckString;
                data.LowerLimit = limit.CheckString;
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }
            data.ECName = CreateErrorCode(result, title);
            AddResult(item, data);
            return false;
        }

        bool CheckNumbericLimit(ITestItem item, string title, double value, ItemLimit limit = null)
        {
            bool result = false;

            ResultData data = new ResultData() { TestName = title, Value = value.ToString(), Unit = limit.Unit };
            try
            {
                result = CheckLimitOnly(value, limit);
                data.UpperLimit = limit.UCL.ToString();
                data.LowerLimit = limit.LCL.ToString();
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

            data.ECName = CreateErrorCode(result, title);
            AddResult(item, data);
            return result;
        }


        bool CheckLimitOnly(double? value, ItemLimit limit)
        {
            bool result = false;
            if (limit == null)
                limit = new ItemLimit();

            if (value != null)
            {
                if (limit.UCL == null && limit.LCL == null)
                {
                    result = true;
                }
                else if (limit.UCL == null)
                {
                    result = limit.LCLClosedInterval ? value >= limit.LCL : value > limit.LCL;
                }
                else if (limit.LCL == null)
                {
                    result = limit.UCLClosedInterval ? value <= limit.UCL : value < limit.UCL;
                }
                else
                {
                    result = (limit.LCLClosedInterval ? value >= limit.LCL : value > limit.LCL) && (limit.UCLClosedInterval ? value <= limit.UCL : value < limit.UCL);
                }
            }

            return result;
        }

        string ChineseCheck(string str)
        {
            if (Regex.IsMatch(str, @"[\u4e00-\u9fa5]"))
                return string.Empty;
            return str;
        }

        string GetFirstFailItem(string sfisData)
        {
            List<string> lines = new List<string>();
            lines = sfisData.SplitToList(Environment.NewLine);

            string firstFailedItem = "";
            for (int i = 0; i < lines.Count; i++)
            {
                string[] arr = lines[i].Split(',');
                if (string.IsNullOrEmpty(firstFailedItem) && arr[1].Equals("0"))
                {
                    firstFailedItem = arr[0];
                    break;
                }
            }

            return firstFailedItem;
        }

        string GetAllFailItems(string sfisData)
        {
            List<string> failItems = new List<string>();
            var lines = sfisData.SplitToList(Environment.NewLine);

            for (int i = 0; i < lines.Count; i++)
            {
                string[] arr = lines[i].Split(',');
                if (arr[1].Equals("0"))
                {
                    failItems.Add(arr[0]);
                }
            }

            return failItems.CombineToString(";");
        }

        public bool ClearDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)
                    || !Directory.Exists(path))
                {
                    return true;  // 如果参数为空，则视为已成功清空
                }
                // 删除当前文件夹下所有文件
                foreach (string strFile in Directory.GetFiles(path))
                {
                    try
                    {
                        File.Delete(strFile);
                    }
                    catch { }
                }
                // 删除当前文件夹下所有子文件夹(递归)
                foreach (string strDir in Directory.GetDirectories(path))
                {
                    try
                    {
                        Directory.Delete(strDir, true);
                    }
                    catch { }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public void CopyDirectory(string sourcePath, string destPath)
        {
            try
            {
                //如果指定的存储路径不存在，则创建该存储路径
                if (!Directory.Exists(destPath))
                {
                    //创建
                    Directory.CreateDirectory(destPath);
                }
                //获取源路径文件的名称
                string[] files = Directory.GetFiles(sourcePath);
                //遍历子文件夹的所有文件
                foreach (string file in files)
                {
                    string pFilePath = destPath + "\\" + Path.GetFileName(file);
                    if (File.Exists(pFilePath))
                        continue;
                    File.Copy(file, pFilePath, true);
                }
                string[] dirs = Directory.GetDirectories(sourcePath);
                //递归，遍历文件夹
                foreach (string dir in dirs)
                {
                    CopyDirectory(dir, destPath + "\\" + Path.GetFileName(dir));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 获取文件夹下的所有文件（包括子文件夹中的文件）
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>文件路径数组</returns>
        public string[] GetAllFilesRecursively(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");
            }

            // 使用 SearchOption.AllDirectories 递归搜索所有文件
            return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        }

        static Dictionary<string, object> ParseCsvToDictionaryWithKeySelected(string filePath, string[] keysToSelect = null)
        {
            var result = new Dictionary<string, object>();

            using (var reader = new StreamReader(filePath))
            {
                // Read the header line (first row)
                var headerLine = reader.ReadLine();
                var headers = headerLine.Split(',');

                // Read the second data line (second row)
                var dataLine = reader.ReadLine();
                var dataValues = SplitCsvValues(dataLine);

                // Loop through columns, select specific ones, and parse the data
                for (int i = 0; i < headers.Length; i++)
                {
                    var key = headers[i].Trim();
                    var value = ParseValueWithKey(dataValues[i].Trim());

                    // If keysToSelect is provided, only select the specified columns
                    if (keysToSelect == null || Array.Exists(keysToSelect, k => k == key))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }

        static object ParseValueWithKey(string value)
        {
            try
            {
                // Handle list format: [3, 5, 8, 10, 12, 13, 14, 14]
                if (value.StartsWith("[") && value.EndsWith("]"))
                {
                    return ParseList(value);
                }

                // Handle tuple format: (36553, 10523), (39873, 10537), ...
                if (value.StartsWith("(") && value.Contains(",") && value.EndsWith(")"))
                {
                    return ParseTupleList(value);
                }

                // Handle JSON-like dictionary list: [{'count': 15871, ...}]
                if (value.StartsWith("[{") && value.EndsWith("}]"))
                {
                    return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(value);
                }

                // Handle a single dictionary format: {'key': value, ...}
                if (value.StartsWith("{") && value.EndsWith("}"))
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
                }

                // Try to parse the value as a numeric (int or double)
                if (double.TryParse(value, out double numericValue))
                {
                    return numericValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing value '{value}': {ex.Message}");
            }

            // Return the value as a string if no parsing applies
            return value;
        }

        static List<object> ParseList(string value)
        {
            // Remove the square brackets and split the string by commas
            value = value.TrimStart('[').TrimEnd(']');
            var values = value.Split(',');

            var list = new List<object>();
            foreach (var item in values)
            {
                // Parse each item (could be a number, tuple, or further list)
                list.Add(ParseValueWithKey(item.Trim()));
            }

            return list;
        }

        static List<Tuple<int, int>> ParseTupleList(string value)
        {
            var tupleList = new List<Tuple<int, int>>();
            // Remove square brackets and split by tuples
            value = value.TrimStart('[').TrimEnd(']');

            var tupleStrings = value.Split(new string[] { "), (" }, StringSplitOptions.None);

            foreach (var tupleString in tupleStrings)
            {
                var parts = tupleString.Trim('(', ')').Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int first) && int.TryParse(parts[1], out int second))
                {
                    tupleList.Add(new Tuple<int, int>(first, second));
                }
            }

            return tupleList;
        }

        // Helper method to handle CSV data properly, splitting by commas but handling embedded quotes and brackets
        static string[] SplitCsvValues(string line)
        {
            var values = new List<string>();
            var currentValue = string.Empty;
            var insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];

                if (currentChar == '"' && (i == 0 || line[i - 1] != '\\')) // Handling quoted values
                {
                    insideQuotes = !insideQuotes;
                }
                else if (currentChar == ',' && !insideQuotes) // Split at commas only outside quotes
                {
                    values.Add(currentValue.Trim());
                    currentValue = string.Empty;
                }
                else
                {
                    currentValue += currentChar;
                }
            }

            if (!string.IsNullOrEmpty(currentValue))
            {
                values.Add(currentValue.Trim()); // Add the last value
            }

            return values.ToArray();
        }

        public void SaveExtraLogs(string log)
        {
            try
            {
                log = Environment.NewLine + "=============== " + _Station.ToString() + " ===== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ===============" + Environment.NewLine + log;
                var fi = new FileInfo("AppLogs\\ExtraLog\\Slot_" + (Project.ProjectIndex + 1) + "_ExtraLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
                if (!fi.Directory.Exists)
                    fi.Directory.Create();

                File.AppendAllText(fi.FullName, log + Environment.NewLine);
            }
            catch (Exception ex)
            {
                UIMessageBox.Show(Project, ex.ToString());
            }
        }

    }
}

