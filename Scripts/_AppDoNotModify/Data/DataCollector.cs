using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks; 
using Test._ScriptExtensions;
using UserHelpers.Helpers;

namespace Test._App
{

    class DataCollector : IDataCollector
    {
        ITestProject _Project { get; set; }

        int _PaddingLen { get; set; }

        string _dataLogTitle = "ITEM,STATUS,VALUE,LCL,UCL,UNIT,RESULT,ERROR MESSAGE,ItemTitle,RetryIndex,TotalIndex,IsLatest,TestStartTime,TestEndTime,TestSeconds,EC Name,ERROR CODE,CUSTOMER CODE,ERROR DESCRIPTION" + Environment.NewLine;

        public DataCollector(ITestProject project)
        {
            _Project = project;
            _PaddingLen = 30;
            if (_Project == null)
                throw new Exception(MethodBase.GetCurrentMethod().DeclaringType.FullName + " : parameter error!");
        }

        List<IResultData> GetLatestResults(ITestItem item)
        {
            List<IResultData> data = new List<IResultData>();
            var gResults = item.GetGroupResultModels();
            if (gResults != null && gResults.Count > 0)
            {
                IRetryResultModel g = gResults[gResults.Count - 1];
                var results = g.GetResults();
                if (results.Count > 0)
                {
                    IResultModel r = results[results.Count - 1];
                    if (r.ResultData.Count > 0)
                    {
                        data.AddRange(r.ResultData.Select((a, i) =>
                        {
                            if (i > 0)
                                a.TestStartTime = a.TestEndTime;
                            return a;
                        }));
                    }
                }
            }
            return data;
        }

        List<IResultData> GetLatestAllResults(ITestItem item)
        {
            List<IResultData> data = new List<IResultData>();
            var gResults = item.GetGroupResultModels();
            if (gResults != null && gResults.Count > 0)
            {
                IRetryResultModel g = gResults[gResults.Count - 1];
                var results = g.GetResults();
                if (results.Count > 0)
                {
                    results.ForEach(m =>
                    {
                        if (m.ResultData.Count > 0)
                            data.AddRange(m.ResultData);
                    });

                }
            }
            return data;
        }

        List<IResultData> GetAllResults(ITestItem item)
        {
            List<IResultData> data = new List<IResultData>();
            var gResults = item.GetGroupResultModels();
            if (gResults != null && gResults.Count > 0)
            {
                gResults.ForEach(g =>
                {
                    var results = g.GetResults();
                    results.ForEach(r => data.AddRange(r.ResultData));
                });
            }
            return data;
        }

        public void UpdateLatestFlag()
        {
            List<ITestItem> allList = _Project.GetRunningItems();

            //Parallel.For(0, allList.Count, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount + 2 }, i =>
            //{
            //    GetLatestResults(allList[i]).ForEach(r => r.IsLatest = "1");
            //});

            allList.ForEach(m =>
            {
                GetLatestResults(m).ForEach(r => r.IsLatest = "1");
            });
        }

        public string GetTestData()
        {
            StringBuilder logs = new StringBuilder();
            logs.Append(_dataLogTitle);
            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.TestDataCollectionMode == TestDataCollectionType.LatestOne)
                    GetLatestResults(m).ForEach(r => { logs.Append(r.ToLog() + Environment.NewLine); });
                else if (m.TestDataCollectionMode == TestDataCollectionType.LatestAll)
                    GetLatestAllResults(m).ForEach(r => { logs.Append(r.ToLog() + Environment.NewLine); });
                else if (m.TestDataCollectionMode == TestDataCollectionType.All)
                    GetAllResults(m).ForEach(r => { logs.Append(r.ToLog() + Environment.NewLine); });

            });
            return logs.ToString();
        }

        public string GetSFISData()
        {
            StringBuilder logs = new StringBuilder();
            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.SFISDataCollectionMode == SFISDataCollectionType.LatestOne)
                    GetLatestResults(m).ForEach(r => { logs.Append(r.ToSFISData() + "\r\n"); });
                else if (m.SFISDataCollectionMode == SFISDataCollectionType.LatestAll)
                    GetLatestAllResults(m).ForEach(r => { logs.Append(r.ToSFISData() + "\r\n"); });
                else if (m.SFISDataCollectionMode == SFISDataCollectionType.All)
                    GetAllResults(m).ForEach(r => { logs.Append(r.ToSFISData() + "\r\n"); });
            });
            return logs.ToString();
        }
        public List<string> GetDataNames()
        {
            List<string> data = new List<string>();

            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.TestDataCollectionMode == TestDataCollectionType.All || m.SFISDataCollectionMode == SFISDataCollectionType.All)
                    GetAllResults(m).ForEach(r => { data.Add(r.TestName); });
                else if (m.TestDataCollectionMode == TestDataCollectionType.LatestAll || m.SFISDataCollectionMode == SFISDataCollectionType.LatestAll)
                    GetLatestAllResults(m).ForEach(r => { data.Add(r.TestName); });
                else if (m.TestDataCollectionMode == TestDataCollectionType.LatestOne || m.SFISDataCollectionMode == SFISDataCollectionType.LatestOne)
                    GetLatestResults(m).ForEach(r => { data.Add(r.TestName); });

            });

            return data;
        }

        public string GetErrorCodes(bool isMultiple)
        {
            string error = string.Empty;

            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.ItemType != ItemNodeType.If)
                {
                    GetLatestResults(m).ForEach(r =>
                    {
                        if (r.Error != null)
                        {
                            if (isMultiple == true)
                                error = error + (error.Length > 0 ? "," : "") + r.Error.ErrorCode;
                            else
                                error = (error.Length > 0 ? error : r.Error.ErrorCode);
                        }
                    });
                }
            });
            return error;
        }

        public string GetCustomerCodes(bool isMultiple)
        {
            string error = string.Empty;

            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.ItemType != ItemNodeType.If)
                {
                    GetLatestResults(m).ForEach(r =>
                    {
                        if (r.Error != null)
                        {
                            if (isMultiple == true)
                                error = error + (error.Length > 0 ? "," : "") + r.Error.CustomerCode;
                            else
                                error = (error.Length > 0 ? error : r.Error.CustomerCode);
                        }
                    });
                }
            });
            return error;
        }

        public ObservableCollection<IResultData> GetShowResultData(bool IsFailOnly = true)
        {
            ObservableCollection<IResultData> data = new ObservableCollection<IResultData>();
            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.ItemType != ItemNodeType.If)
                {
                    if (m.TestDataCollectionMode == TestDataCollectionType.LatestOne || m.TestDataCollectionMode == TestDataCollectionType.LatestAll)
                    {
                        GetLatestResults(m).ForEach(r => data.Add(r));
                    }
                    else if (m.TestDataCollectionMode == TestDataCollectionType.All)
                    {
                        GetAllResults(m).ForEach(r => data.Add(r));
                    }
                }
            });

            if (IsFailOnly)
            {
                if (data.Any(d => !string.IsNullOrEmpty(d.ECName)))
                    data = new ObservableCollection<IResultData>(data.Where(d => !string.IsNullOrEmpty(d.ECName)));
            }

            return data;
        }
        //==================logs
        string FormatGroupLogs(IRetryResultModel groupResult)
        {
            return "Time".PadRight(_PaddingLen, ' ') + " : " + groupResult.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + ", " + groupResult.EndTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + ", " + groupResult.Ticks.ToString("g") + Environment.NewLine;
        }

        string FormatLogs(ITestItem item, IResultModel result, int groupIndex)
        {
            string log = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    StringBuilder logs = new StringBuilder();
                    logs.Append(Environment.NewLine + "=".PadRight(_PaddingLen, '=') + " : " + item.Title + Environment.NewLine +
                        "Index".PadRight(_PaddingLen, ' ') + " : Current Retry = " + result.RetryIndex.ToString() + ", Total Retry = " + result.TotalRetryIndex.ToString() + ", Group Retry = " + groupIndex + Environment.NewLine +
                        "ItemTime".PadRight(_PaddingLen, ' ') + " : " + result.TestStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + ", " + result.TestEndTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + ", " + result.TotalTicks.ToString("g") + Environment.NewLine +
                        "FunctionName".PadRight(_PaddingLen, ' ') + " : " + item.FunctionName + "(" + result.ArgsType + "), Value = (" + result.ArgsValue + ")" + Environment.NewLine +
                        "FunctionTime".PadRight(_PaddingLen, ' ') + " : " + result.FunctionStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + ", " + result.FunctionEndTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + ", " + result.FunctionTicks.ToString("g") + Environment.NewLine +
                        "Result".PadRight(_PaddingLen, ' ') + " : " + result.Result.ToString() + ", ReturnCode = " + result.ReturnCode.ToString() + Environment.NewLine +
                        "ResultData".PadRight(_PaddingLen, ' ') + " : " + result.ResultData.Count.ToString() + Environment.NewLine);

                    result.ResultData.ForEach(r => { logs.Append(r.ToLog() + Environment.NewLine); });

                    logs.Append("Logs".PadRight(_PaddingLen, ' ') + " : " + result.TestLogger.Count().ToString() + Environment.NewLine + result.TestLogger.ToLog() + Environment.NewLine);

                    log = logs.ToString();

                    break;
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }
            return log;
        }

        //public string GetLogs()
        //{
        //    StringBuilder logs = new StringBuilder();
        //    List<ITestItem> allList = _Project.GetRunningItems();
        //    allList.ForEach(m =>
        //    {
        //        string title = "============= " + m.Title + " =============";
        //        var gResults = m.GetGroupResultModels();
        //        gResults.ForEach(g =>
        //        {
        //            logs.Append("=".PadRight(title.Length, '=') + Environment.NewLine +
        //                            title + Environment.NewLine +
        //                            "=".PadRight(title.Length, '=') + Environment.NewLine);
        //            logs.Append(FormatGroupLogs(g));
        //            var retryResults = g.GetResults();
        //            if (retryResults.Count > 0)
        //                retryResults.ForEach(result => logs.Append(FormatLogs(m, result, g.GroupIndex)));
        //        });
        //    });

        //    return logs.ToString();
        //}
        public void Update_ResultValue(string key, string value)
        {
            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                GetAllResults(m).ForEach(r =>
                {
                    if (r.TestName.Equals(key))
                    {
                        r.Value = value; 
                    }
                });
            });
        }

        public void Format_ResultData(List<string> existTitles)
        { 
            //处理名称合规
            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                GetAllResults(m).ForEach(r =>
                {
                    r.TestName = r.TestName.FormatResultData();

                    if (r.Value.HasChinese())
                        r.Value = string.Empty; 
                    r.Value = r.Value.FormatResultData();

                    r.Message = r.Message.FormatResultData();
                    r.ItemTitle = r.ItemTitle.FormatResultData(); 
                });
            });

            //处理重名
            Dictionary<string, int> weights = new Dictionary<string, int>();
            existTitles.ForEach(t => weights[t] = 1);

            var data = GetTestResultData();
            data.ForEach(d =>
            {
                if (weights.ContainsKey(d.TestName))
                {
                    weights[d.TestName]++;
                    d.TestName = $"{d.TestName}_{weights[d.TestName]}";
                }
                else
                {
                    weights.Add(d.TestName, 1); 
                }
            });

        }
 
        public List<IResultData> GetTestResultData()
        {
            List<IResultData> data = new List<IResultData>();
            List<ITestItem> allList = _Project.GetRunningItems();
            allList.ForEach(m =>
            {
                if (m.SFISDataCollectionMode == SFISDataCollectionType.LatestOne)
                    data.AddRange(GetLatestResults(m));
                else if (m.SFISDataCollectionMode == SFISDataCollectionType.LatestAll)
                    data.AddRange(GetLatestAllResults(m));
                else if (m.SFISDataCollectionMode == SFISDataCollectionType.All)
                    data.AddRange(GetAllResults(m));
            });
            return data;
        }

        public string GetLogs()
        {
            List<ITestItem> allList = _Project.GetRunningItems();

            Parallel.For(0, allList.Count, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount + 2 }, i =>
            {
                var m = allList[i];
                m.TempLogs = string.Empty;
                StringBuilder tmp = new StringBuilder();
                string title = "============= " + m.Title + " =============";
                var gResults = m.GetGroupResultModels();
                gResults.ForEach(g =>
                {
                    tmp.Append("=".PadRight(title.Length, '=') + Environment.NewLine +
                                    title + Environment.NewLine +
                                    "=".PadRight(title.Length, '=') + Environment.NewLine);
                    tmp.Append(FormatGroupLogs(g));
                    var retryResults = g.GetResults();
                    if (retryResults.Count > 0)
                        retryResults.ForEach(result => tmp.Append(FormatLogs(m, result, g.GroupIndex)));
                });
                m.TempLogs = tmp.ToString();
            });

            StringBuilder logs = new StringBuilder();
            allList.ForEach(m =>
            {
                logs.Append(m.TempLogs);
            });

            return logs.ToString();
        }

    }
}
