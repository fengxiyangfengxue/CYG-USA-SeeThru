using System;
using System.Collections.Generic;
using System.IO;
using UserHelpers.Helpers;
using Test._App;
using LitJson;
using System.Text.RegularExpressions;
using GTKWebServices;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using Test.Definition;
using Test._ScriptExtensions;
using Test._Definitions;
using System.Windows.Media;
using System.Threading;
using Test._ScriptHelpers;
using System.Data;
using MetaHelpers.ScriptHelpers;

namespace Test
{
    public partial class MainClass
    {

        [MainClassConstructor(TEST_STATION.ANY_STATION, level: 0)]
        public int MainClassConstructor_MES()
        {
            //改成后台登录,在开始测试之前检查登录是否成功
             
            var task = Task.Run(() =>
            {
                var client = new GTKTestInterface(_mesSetting.App_WebService, _mesSetting.Wip_WebService);
                _Context.MESClient = client;

                while (true)
                {
                    if (_isDisplsed) //mainclass如果释放了，说明会重新运行一个Task，这里要停止退出
                        return;

                    try
                    {
                        if (!_Context.IsMesLoggedIn)
                        {
                            _Context.IsMesLoggedIn = client.mes.Login(_mesSetting.UserName, _mesSetting.PWD);
                            if (!_Context.IsMesLoggedIn)
                                throw new Exception("MES Login Failed!");
                        }

                        if (!_Context.IsMESCheckedLineAndStation)
                        {
                            _Context.IsMESCheckedLineAndStation = client.mes.CheckLineAndStation(_mesSetting.LineName, _mesSetting.StationDescs[Project.ProjectIndex].Value);
                            if (!_Context.IsMESCheckedLineAndStation)
                                throw new Exception("MES CheckLineAndStation Failed!");
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        //UIMessageBox.Show(Project, ex.ToString(), "MainClassConstructor_MES", UIMessageBoxButton.OK, 14, Colors.Red);
                        Thread.Sleep(1000);
                    }
                }
            });


            return 0;
        }


        [ScriptInitialize(TEST_STATION.ANY_STATION, level: 5)]
        public int Script_Initialize_Mes(ITestItem item)
        {
            //item.AddLog("Mes Login Result{0} = {1}", Project.ProjectIndex + 1, _context.IsMesLoggedIn);
            //if (!_context.IsMesLoggedIn)
            //    throw new Exception("mes 登录异常,请联系相关人员检查!");

            return 0;
        }


        [AfterScript(station: TEST_STATION.ANY_STATION, level: 0)]
        public int AfterScript_ResultData(ITimeLogger logger)
        {
            bool result = false;

            try
            {
                string ec = Project.GetErrorCodes();
                if (!string.IsNullOrEmpty(ec))
                    ec = "_[" + ec + "]";
                Project.PathDictionary["ErrorCode"] = ec;

                _Context.TestCount++;
                if (Project.HasFailed)
                {
                    _Context.FailCount++;
                }
                else
                {
                    _Context.PassCount++;
                    if (Project.IsOnLine)
                        _appraiser.Next(Project.SerialNumber);
                }


                var collector = (DataCollector)App_GetDataCollector();

                //更改两个测试项数值
                _Context.TestEndTime = DateTime.Now;
                string endtime = _Context.TestEndTime.ToString("yyyy-MM-dd HH:mm:ss:fff");
                string total = (_Context.TestEndTime - _Context.TestStartTime).TotalSeconds.ToString();
                collector.Update_ResultValue(ConstKeys.Test_EndTime, endtime);
                collector.Update_ResultValue(ConstKeys.TestTotalSeconds, total);

                //全局统一处理特殊字符并检查重名项
                //这里的调用GenerateMESData只是为了获取Header
                List<string> headers = GenerateMESData(Project.SerialNumber, logger).Keys.ToList();
                collector.Format_ResultData(headers);

                //find fails
                Project.PathDictionary["Result"] = Project.HasFailed ? "FAIL" : "PASS";
                if (Project.HasFailed)
                {
                    var allFailData = collector.GetTestResultData().Where(d => !string.IsNullOrEmpty(d.ECName)).ToList();
                    logger.AddLog("allFailData count = " + allFailData.Count);
                    if (allFailData.Count > 0)
                    {
                        _Context.FirstFailData = (ResultData)allFailData.First();
                        logger.AddLog("firstFailedItem = " + _Context.FirstFailData.TestName);
                        _Context.AllFailData = allFailData.ConvertAll(d => (ResultData)d).ToList();
                    }
                }

                lock (SummaryLocker)
                {
                    SummaryHelper summary = new SummaryHelper(Project);
                    summary.FailItems = _Context.FirstFailData.TestName;
                    summary.LineID = _mesSetting.LineID;
                    summary.TesterName = _mesSetting.TesterNames[Project.ProjectIndex].Value;
                    //TODO:保存CSV时MES相关信息 appraiser
                    summary.Appraiser = _appraiser.Get();
                    summary.TestStartTime = _Context.TestStartTime;
                    summary.TestEndTime = _Context.TestEndTime;
                    summary.SummaryPath = _commonSetting.SummaryPath;
                    //summary.Station = _Config.StartupConfig.Station.ToString();
                    summary.Station = _mesSetting.StationName;
                    summary.FileMaxSize = 5L * 1024 * 1024; //5M
                    summary.IsUnitEnabled = false;
                    summary.IsAuditMode = _Context.IsAudit;
                    summary.CONFIG_VER = _buildSetting.Version;
                    if (!summary.SaveSummary(logger))
                        throw new Exception("save summary failed!");
                }

                result = true;
            }
            catch (Exception ex)
            {
                logger.AddLog(ex.ToString());
                UIMessageBox.Show(Project, ex.ToString());
            }
            finally
            {
                if (!result)
                {
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterScript_ResultData failed!", "AfterScript fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }

            return 0;
        }

        JsonData GenerateMESData(string serialNumber, ITimeLogger logger)
        {

            JsonData jd = new JsonData();
            jd["Product"] = _Config.Product;   //G20, 产口名称，人工配制
            jd["Station"] = _mesSetting.StationName;    //测试站名称  //在调用完CheckLineAndStation之后 通过_mes.CurrentSectionDesc获得, 我们自己配制
            jd["SN"] = serialNumber;
            jd["DeviceID"] = "";   //为空，用不到
            jd["Lot"] = "";  //空，用不到
            jd["MachineMac"] = "";  //空，用不到

            jd["User"] = _Config.OperatorID;  //是登录Rubik的帐号，我们没有
            jd["TestBeginTime"] = _Context.TestStartTime.ToString("yyyy-MM-dd HH:mm:ss");
            jd["TestEndTime"] = _Context.TestEndTime.ToString("yyyy-MM-dd HH:mm:ss");
            jd["TotalTime"] = (_Context.TestEndTime - _Context.TestStartTime).TotalSeconds.ToString("F2") + "s"; //测试总时间，单位秒
            jd["TotalStatus"] = Project.HasFailed ? "FAIL" : "PASS";  //PASS/FAIL
            jd["CfgVer"] = Project.Signature.Version;     //测试Sequence版本，或软件版本号
            jd["CfgFile"] = "";   //空，用不到

            jd["TestFailItem"] = _Context.FirstFailData.TestName;  //FAIL项目名称，如果多个只传第一个
            jd["Build_phase"] = _buildSetting.BuildPhase;  //阶段  PVT, EVT, PreEVT

            //******
            //SMT: 填空
            //FATP: 从MES抓取
            jd["DUT_CONFIG"] = _Context.DUT_Config;  //当前测试产品Config，从MES上获取的

            jd["TesterName"] = _mesSetting.TesterNames[Project.ProjectIndex].Value;   //机台号，是人工配制的，在MES CheckLineAndStation的时候会用到，确保机台号是MES上可识别的

            jd["Appraiser"] = _appraiser.Get();   //****  具体意义不明，目前固定A，可以留外面配制
                                                  //类似24小时检测一次的功能，用几个Golden sample测试几次Pass才算Pass
            jd["Audit_Mode"] = _Context.IsAudit ? "ON" : "OFF";  //OFF，开户Audit点检功能才会是ON，正常测试是OFF， 点检的SN在MES上有配制过，所以使用点检机台ONLINE跑也可以上传过站

            //FATP  Location_ID 他们写死在代码中的，由G20固定匹配23
            //G20 -> 23 
            jd["Location_ID"] = _buildSetting.FactoryId;  //FATP Audio站配的23固定值 ，PCBA此值为空，目前不确定具体含义

            //Rubik配制例子 LineName=G_TEST_01, StationDesc=G_AUDIO_01_02
            //SMT 人工在Ini中配制
            jd["Line_ID"] = _mesSetting.LineID; //从StationDesc用下划线截取出来的，StationDesc=G_AUDIO_01_02，截取倒数第二个01

            //SMT: 人工填写在ini中配制
            //FATP: 从mes抓取，到时再次确认一下
            jd["Station_Name"] = _buildSetting.AssemblyPhase == AssemblyEnum.SMT ? _mesSetting.StationName : (_Context.MESClient != null ? _Context.MESClient.mes.CurrentStationDesc : _mesSetting.StationName);  //在调用完CheckLineAndStation之后 通过_mes.CurrentSectionDesc获得

            //SMT: 留在Ini中配制
            //fatp: 再确认
            jd["Station_Number"] = _mesSetting.StationNumbers[Project.ProjectIndex].Value;   //从StationDesc用下划线截取出来的，StationDesc=G_AUDIO_01_02，截取倒数第一个02 
            jd["Station_Type_ID"] = _buildSetting.StationSequencing;   //固定配制，由人工配制，目前FATP Audio站填写12

            //SMT:表示Slot编号1234
            //FATP: 再确认
            jd["Station_Instance"] = (Project.ProjectIndex + 1).ToString();  //***** 目前看好像是Rubik上面配制的StationDesc的用逗号分隔的数量,可能是指一对几
            jd["Parameter_ID"] = "";  //空，用不到

            //SMT:表示Slot编号1234
            //FATP: 再确认
            jd["Test_ID"] = (Project.ProjectIndex + 1).ToString();  // ****** 有可能是SlotID，要再确认
            jd["Section"] = "NA";  //固定写NA
            jd["Assembly_phase"] = _buildSetting.AssemblyPhase.ToString();   //SMT或FATP，取决于实际在哪
            jd["Workflow_component"] = _buildSetting.WorkflowComponent.ToString();   //产口类型，目前FATP是Headset，是人工配制的
            jd["Station_sequencing"] = _buildSetting.StationSequencing;  //与Station_Type_ID是同一个值 ，也是人工配制的
            jd["ErrorCode"] = "";   //现在都是空，不管PASS/FAIL都是空
            jd["diags_version"] = ""; //目前留空
            jd["firmware_version"] = ""; //目前留空
            jd[ConstKeys.OS_Version] = ""; //目前留空

            return jd;
        }

        /// <summary>
        /// 在导出数据模块的后面
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        [AfterScriptAttribute(TEST_STATION.ANY_STATION, level: 50)]
        public int AfterScript_MES_CommitTestData(ITimeLogger logger)
        {
            logger.AddLog("_isAuditCheckFail = " + _Context.IsAuditCheckFail);

            bool result = false;

            try
            {
                string ret = string.Empty;
                Project.AllowCloseWindow = false;

                var serialNumber = Project.SerialNumber; //只是为了防止以后可能用其它SN上传数据
                logger.AddLog("SerialNumber = " + Project.SerialNumber);
                logger.AddLog("IsUploadFail = " + Project.IsFailToSFIS.ToString());

                if (!_Context.IsCheckRoutePass)
                {
                    logger.AddLog("skip CheckRoute fail to MES!");
                    result = true;
                    return 0;
                }

                if (!Project.IsFailToSFIS && Project.HasFailed)
                {
                    logger.AddLog("skip uploading fail data to MES!");
                    result = true;
                    return 0;
                }

                var jd = GenerateMESData(serialNumber, logger);
                var collector = (DataCollector)App_GetDataCollector();
                var resultData = collector.GetTestResultData();
                resultData.ForEach(r =>
                {
                    var d = new JsonData();
                    d["isPass"] = r.Status.Equals("1") ? "True" : "False";
                    d["data"] = r.Value;
                    d["unit"] = r.Unit;
                    d["lowerLimit"] = r.LowerLimit;
                    d["upperLimit"] = r.UpperLimit;
                    if (jd.ContainsKey(r.TestName))
                        throw new Exception("duplicated item found = " + r.TestName);
                    jd[r.TestName] = d;
                });

                string testData = jd.ToJson();
                logger.AddLog(testData);

                //离线和点检testResult=4
                int testResult = Project.IsOnLine ? (Project.HasFailed ? 1 : 0) : 4;
                logger.AddLog("testResult = " + testResult);

                int resultType = 0;
                logger.AddLog("resultType = " + resultType);

                string serverTime = _Context.MESClient.mes.GetServerTime();
                logger.AddLog("serverTime = " + serverTime);
                Project.PathDictionary["ServerTime"] = serverTime;

                string errorCode = null;
                logger.AddLog("errorCode = null");

                string testFileName = "";
                logger.AddLog("testFileName = " + testFileName);

                logger.AddLog("errorDesc = " + _Context.FirstFailData.TestName);

                string fileName = Project.ParsePath("[\"SN\"][\"ServerTime\"][\"Product\"][\"LineName\"][\"StationDesc\"][" + (Project.HasFailed ? "FAIL" : "PASS") + "].txt");
                FileInfo fi = new FileInfo(Path.Combine(_Context.BackupFolder, fileName));
                if (!fi.Directory.Exists)
                    fi.Directory.Create();
                File.WriteAllText(fi.FullName, testData);

                List<string> files = new List<string>();
                //log上传mes
                if (Project.HasFailed && Project.IsFailToSFIS)
                {
                    var log = Project.GetLogs();
                    string path = string.Format("..\\CaesarLog\\{0}\\{1}\\[{2}][{3}][{4}][{5}][{6}][{7}][{8}][{9}].log",
                       DateTime.Now.ToString("yyyy-MM-dd"), (Project.IsOnLine ? "ONLINE" : "OFFLINE"),
                       _Config.Product, _mesSetting.StationName,
                       _Context.MESClient.mes.CurrentStationDesc, (Project.IsOnLine ? "ONLINE" : "OFFLINE"),
                       DateTime.Now.ToString("yyyyMMddHHmmss"), serialNumber,
                       (Project.HasFailed ? "Fail" : "Pass"), Project.ProjectIndex + 1);

                    FileInfo info = new FileInfo(path);
                    if (!info.Directory.Exists)
                        info.Directory.Create();

                    File.WriteAllText(path, log);
                    files.Add(path);
                }
                files.ForEach(f => logger.AddLog("files = " + f));
                //string sn,
                //int testResult  0=pass, 1=fail,
                //int ResultType 固定 0,
                //string testtime mes.GetServerTime(),
                //string errorCode,  //始终 null
                //string errorDesc,  //第一个fail item名称
                //string testData,   //JSON
                //byte[] fileBytes, //null
                //string testFileName, //""
                //string[] testFilePaths  //files
                //_mes.CommitTestData("2G0Y1P5D7P002V", 0, 0, mes.GetServerTime(), null, "", datastr, null, "", files.ToArray());
                ret = _Context.MESClient.mes.SaveTestData(serialNumber, testResult, resultType, serverTime,
                     errorCode, _Context.FirstFailData.TestName, fi.Name, testData, null, "", files.ToArray());

                //_Context.MESClient.mes.SaveCorrectFileContent("", "key", "", "username");
                //var dt = _Context.MESClient.mes.GetCorrectFileContent("", "", "key");
                //dt.Rows[0]["FILE_NAME"].ToString();

                //ret = _mes.CommitTestData(SN, testResult, resultType, serverTime,
                //    errorCode, firstFailedItem, testData, fileBytes, testFileName, files.ToArray());

                logger.AddLog("CommitTestData = " + ret);

                result = ret.Equals("OK");
            }
            catch (Exception ex)
            {
                logger.AddLog(ex.ToString());
                UIMessageBox.Show(Project, ex.ToString());
            }
            finally
            {
                if (!result)
                {
                    UIMessageBox.Show(Project, "Upload data to MES failed!", "MES fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }

            return 0;
        }

        /// <summary>
        /// 站名 FF12_SYS_Display_Test
        /// 綫體 NM_TEST_01
        /// 工位名稱 N_SYS_DISPLAY_TEST_01_01
        /// 工位號  12782
        /// 幾臺名稱 N_SYS_DISPLAY_TEST_01_01
        /// 幾臺代碼 N_SYS_DISPLAY_TEST_01_01
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int MES_Login(ITestItem item)
        {
            bool result = false;

            try
            {
                if (!_Context.IsMesLoggedIn)
                {
                    var client = _Context.MESClient;
                    item.AddLog("OperatorID = " + _mesSetting.UserName);
                    result = client.mes.Login(_mesSetting.UserName, _mesSetting.PWD);
                    item.AddLog("login = " + result);
                }
                else
                {
                    item.AddLog("already logged in");
                    result = true;
                }

            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);

            return (result ? 0 : 1);
        }

        public int MES_GetConfig(ITestItem item, bool isSubLine = false, bool isCheckConfig = false)
        {
            bool result = false;

            string configCode = string.Empty;
            string config = string.Empty;
            try
            {
                string SN = Project.SerialNumber;
                var client = _Context.MESClient;
                item.AddLog("SN = " + SN);
                if (!string.IsNullOrEmpty(SN))
                {
                    configCode = SN.Substring(5, 2);

                    if (isSubLine)
                    {

                        var dt = client.wipSvc.GetMesWoInfo(SN);
                        if (dt == null || dt.Rows == null || dt.Rows.Count <= 0)
                        {
                            item.AddLog("table rows count = 0");
                            goto ReturnAndExit;
                        }
                        config = dt.Rows[0]["MO"].ToString();
                    }
                    else
                    {
                        config = client.wipSvc.GetConfigBySn(SN);
                    }

                    _Context.DUT_Config = config;
                    item.AddLog("configName = " + config);
                    _Context.Variables[ConstKeys.MES_ConfigName] = config;
                    if (isCheckConfig)
                    {
                        if (!string.IsNullOrEmpty(config))
                        {
                            var fiConfig = new FileInfo(Path.Combine(CaesarConfigPath, "MESConfigs.txt"));
                            item.AddLog("config file = " + fiConfig.FullName);
                            var lines = File.ReadAllLines(fiConfig.FullName).Where(l => !string.IsNullOrEmpty(l)).Select(l => l.Trim()).ToList();
                            
                            item.AddLog("config file content = " + lines.CombineToString(","));
                            result = lines.Any(l => l.Equals(config));
                            if(result)
                                item.AddLog("config found!");
                            else
                                item.AddLog("config not found!");
                        }
                    }
                    else
                        result = !string.IsNullOrEmpty(config);
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            AddResult(item, new ResultData("HMD_ConfigCode", CreateErrorCode(!string.IsNullOrEmpty(configCode), "HMD_ConfigCode"), configCode));
            AddResult(item, new ResultData("HMD_ConfigName", CreateErrorCode(result, "HMD_ConfigName"), config));
            return (result ? 0 : 1);
        }

        public int MES_GetStationType(ITestItem item, bool IsUsedPack = false)
        {
            bool result = false;
            string station = string.Empty;

            try
            {
                string SN = Project.SerialNumber;
                var mes = _Context.MESClient.mes;
                item.AddLog("SN = " + SN);
                if (!string.IsNullOrEmpty(SN))
                {
                    if (!string.IsNullOrEmpty(mes.CurrentSectionCode))
                    {
                        var dt = mes.Search("BASE_SECTION", "SECTION_CUSTOMER", $"SECTION_CODE=\'{mes.CurrentSectionCode}\'", "");
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            station = dt.Rows[0]["SECTION_CUSTOMER"]?.ToString() ?? "";
                        }
                    }
                    if (string.IsNullOrEmpty(station))
                    {
                        throw new Exception("请联系MES工程师配置SECTION_CUSTOMER信息");
                    }
                    item.AddLog("station = " + station);
                    string qdf_stationType = station.Substring(station.LastIndexOf("_") + 1);
                    item.AddLog("qdf_stationType = " + qdf_stationType);
                    _Context.Variables[ConstKeys.MES_Station_Type] = station;
                    _Context.Variables[ConstKeys.QDF_Station_Type] = qdf_stationType;
                    result = !string.IsNullOrEmpty(station);
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            AddResult(item, new ResultData(item.Title, CreateErrorCode(result, item.Title), station));
            return (result ? 0 : 1);
        }

        public int MES_GetPRIMEInfoBySN(ITestItem item, bool IsUsedPack = false)
        {
            bool result = false;
            int testStatus = (int)Test_Mode.PRIME;
            int testCount = 0;

            try
            {
                string SN = Project.SerialNumber;
                var mes = _Context.MESClient.mes;
                item.AddLog("SN = " + SN);

                //1 represents PRIME
                //2 represents FA
                //3 represents REWORK
                //4 represents GR&R
                //5 represents REL
                if (_Context.IsAudit)
                {
                    testStatus = 6;
                    testCount = 0;
                    result = true;
                    goto ReturnAndExit;
                }

                if (!Project.IsOnLine)
                {
                    testStatus = (int)_Config.TestMode;
                    testCount = 0;
                    result = true;
                    goto ReturnAndExit;
                }

                if (string.IsNullOrEmpty(SN))
                {
                    testStatus = (int)Test_Mode.PRIME;
                    testCount = 0;
                    result = true;
                    goto ReturnAndExit;
                }

                var _mes = _Context.MESClient.mes;

                string json = _mes.GetSnLogInfo(SN, _mes.CurrentSectionCode, _mes.CurrentStationCode);
                JsonData jd = JsonMapper.ToObject(json);
                if (jd == null)
                {
                    testStatus = (int)Test_Mode.PRIME;
                    testCount = 0;
                    result = true;
                    goto ReturnAndExit;
                }

                bool flag = true;
                if (jd.ContainsKey("REWORK"))
                {
                    string REWORK = jd["REWORK"].ToString().ToLower();
                    if (REWORK.Equals("true"))
                    {
                        flag = false;
                        testStatus = (int)Test_Mode.REWORK;
                    }
                }

                if (jd.ContainsKey("PRIME") && flag)
                {
                    string PRIME = jd["PRIME"].ToString().ToLower();
                    if (PRIME.Equals("true"))
                        testStatus = (int)Test_Mode.PRIME;
                }

                if (jd.ContainsKey("TEST_COUNT"))
                {
                    string count = jd["TEST_COUNT"]?.ToString() ?? "0";
                    if (int.TryParse(count, out int i))
                        testCount = i;
                }

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }
            finally
            {
                item.AddLog("testStatus = " + testStatus);
                item.AddLog("testCount = " + testCount);

                _Context.Variables[ConstKeys.QDF_TestStatus] = testStatus;
                _Context.Variables[ConstKeys.QDF_TestCount] = testCount;
            }

        ReturnAndExit:

            AddResult(item, new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL));
            return (result ? 0 : 1);
        }

        public int MES_CheckLineAndStation(ITestItem item)
        {
            bool result = false;


            try
            {

                item.AddLog("IsMESCheckedLineAndStation = " + _Context.IsMESCheckedLineAndStation);
                item.AddLog("lineName = " + _mesSetting.LineName);
                item.AddLog("stationDesc = " + _mesSetting.StationDescs[Project.ProjectIndex].Value);
                if (!_Context.IsMESCheckedLineAndStation)
                {
                    result = _Context.MESClient.mes.CheckLineAndStation(_mesSetting.LineName, _mesSetting.StationDescs[Project.ProjectIndex].Value);
                    item.AddLog("CheckLineAndStation = " + result);
                    _Context.IsMESCheckedLineAndStation = result;
                    item.AddLog("IsMESCheckedLineAndStation = " + _Context.IsMESCheckedLineAndStation);
                }
                else
                {
                    item.AddLog("already CheckLineAndStation!");
                    result = true;
                }

                item.AddLog("CurrentLineCode = " + _Context.MESClient.mes.CurrentLineCode); //lineName
                item.AddLog("CurrentLineDesc = " + _Context.MESClient.mes.CurrentLineDesc); //lineName
                item.AddLog("CurrentSectionCode = " + _Context.MESClient.mes.CurrentSectionCode); //RF
                item.AddLog("CurrentSectionDesc = " + _Context.MESClient.mes.CurrentSectionDesc); //RF测试
                item.AddLog("CurrentStationCode = " + _Context.MESClient.mes.CurrentStationCode); //G20-SMT-RF-001
                item.AddLog("CurrentStationDesc = " + _Context.MESClient.mes.CurrentStationDesc); //G20-SMT-RF-001  

            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);

            return (result ? 0 : 1);
        }
         
 
        #region SMT
        public int MES_Get_BT_MAC(ITestItem item)
        {
            bool result = false;
            string mac = string.Empty;
            try
            {
                item.AddLog("sn = " + Project.SerialNumber);
                var client = _Context.MESClient;
                string ret = client.mes.Get_CUST_MAC(Project.SerialNumber, "BT");
                mac = ret.Trim();
                Regex regex = new Regex("^[0-9A-Fa-f]{12}");
                item.AddLog("Get_CUST_MAC = " + mac);
                result = !string.IsNullOrEmpty(mac) && regex.IsMatch(mac);
                if (result)
                {
                    _Context.Variables[ConstKeys.MES_BT_MAC] = mac;
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), mac);
            AddResult(item, data);

            return (result ? 0 : 1);
        }

        public int MES_Get_WIFI_MAC(ITestItem item)
        {
            bool result = false;
            string mac = string.Empty;
            try
            {
                item.AddLog("sn = " + Project.SerialNumber);
                var client = _Context.MESClient;
                string ret = client.mes.Get_CUST_MAC(Project.SerialNumber, "WIFI");
                mac = ret.Trim();
                item.AddLog("Get_CUST_MAC = " + mac);
                Regex regex = new Regex("^[0-9A-Fa-f]{12}");
                result = !string.IsNullOrEmpty(mac) && regex.IsMatch(mac);
                if (result)
                {
                    _Context.Variables[ConstKeys.MES_WIFI_MAC] = mac;
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), mac);
            AddResult(item, data);

            return result ? 0 : 1;
        }
        #endregion
        public int MES_Get_Vendor(ITestItem item)
        {
            bool result = false;
            string vendor = string.Empty;
            try
            {
                item.AddLog("sn = " + Project.SerialNumber);
                string ret = string.Empty;
                var client = new SMTTestInterface().client;
                ret = client.GetVendorBySn(Project.SerialNumber);
                item.AddLog("ret = " + ret);
                //正常返回：{ "MESSAGE":"OK","SN":"2G0Y1PKF0Z001T","Vendor":"鹏鼎国际有限公司"} 
                //异常返回：{ "MESSAGE":"NG,供应商名称不存在！","SN":"2G0Y1PKFZ001T","Vendor":""}
                JsonData json = JsonMapper.ToObject(ret);
                if (json.ContainsKey("MESSAGE") && json["MESSAGE"].ToString().Equals("OK") && json.ContainsKey("Vendor"))
                {
                    vendor = json["Vendor"].ToString().Trim();
                    if (string.IsNullOrEmpty(vendor))
                        goto ReturnAndExit;
                    item.AddLog("Vendor = " + vendor);
                    result = !string.IsNullOrEmpty(vendor);
                }
                else
                {
                    vendor = "Get_Vendor failed";
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), vendor);
            AddResult(item, data);

            return result ? 0 : 1;
        }

        //2Y0YB21G2B0004
        public int MES_CheckRoute(ITestItem item, string snType = "")
        {
            bool result = false;

            try
            {
                item.AddLog("sn = " + Project.SerialNumber);
                string SN = Project.SerialNumber;
                string ret = string.Empty;
                var client = _Context.MESClient;

                if (Project.IsOnLine)
                {
                    if (string.IsNullOrEmpty(snType))
                        ret = client.mes.CheckRoute(SN);
                    else
                        ret = client.mes.CheckRoute(SN, snType);
                    item.AddLog("CheckRoute = " + ret);
                    result = ret.Equals("OK");

                    if (!result)
                    {
                        UIMessageBoxConfig config = new UIMessageBoxConfig
                        {
                            Title = $"UUT[{Project.ProjectIndex + 1}]路由提示",
                            Text = ret,
                            TextFontSize = 16,
                            WaitForExit = true,
                            Button = UIMessageBoxButton.None,
                            AliveWith = Project.GetItems()[1],
                        };
                        UIMessageBox.Show(Project, config);
                    }
                }
                else
                {
                    ret = client.mes.CheckRouteOffline(SN);
                    result = ret.Equals("OK");
                    item.AddLog("OFFLINE  CheckRouteOffline");
                    goto ReturnAndExit;
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            _Context.IsCheckRoutePass = result;
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);

            return result ? 0 : 1;
        }

        public int Get_SN_BY_MAC(ITestItem item, string key = "PackSN", string pattern = "")
        {
            bool result = false;
            try
            {
                item.AddLog("sn = " + Project.SerialNumber);
                var client = _Context.MESClient.mes;
                string ret = client.GetMasterCodeBySn(Project.SerialNumber);
                item.AddLog("GetMasterCodeBySn = " + ret);
                Func<string, bool> Match = (string input) =>
                {
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        Regex regex = new Regex(pattern);
                        var match = regex.Match(input);
                        return match.Success;
                    }
                    return true;
                };

                if (!string.IsNullOrEmpty(ret) && !ret.Equals("NO SN") && Match(ret))
                {
                    if (!string.IsNullOrEmpty(key))
                        _Context.Variables[key] = ret;
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);

            return result ? 0 : 1;
        }

        // button project added
        public int BindAndUpdateSnAndNotMac(ITestItem item, string macType, string value)
        {
            bool result = false;
            try
            {
                item.AddLog("sn = " + Project.SerialNumber);
                item.AddLog("macType = " + macType);
                item.AddLog("value = " + value +", type: " + value.GetType());
                var client = _Context.MESClient.mes;
                string ret = client.BindAndUpdateSnAndNotMac(Project.SerialNumber, macType, value);
                item.AddLog($"ret = " + ret + $", {ret.GetType()}");

                if (ret == "OK")
                {
                    result = true;
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

        // button project added
        public string GetMacBySn(ITestItem item, string macType)
        {
            bool result = false;
            string ret = string.Empty;
            try
            {
                item.AddLog($"Mes GetMacBySn");
                var client = _Context.MESClient.mes;
                ret = client.GetMacBySn(Project.SerialNumber, macType);
                item.AddLog($"ret = " + ret + $", {ret.GetType()}");

                /*
                if (!string.IsNullOrEmpty(ret) && !ret.Equals("NO SN") && Match(ret))
                {
                    if (!string.IsNullOrEmpty(key))
                        _Context.Variables[key] = ret;
                    result = true;
                }
                else
                {
                    result = false;
                }
                */
            }
            catch (Exception ex)
            {
                result = false;
                item.AddLog(ex.ToString());
            }

            return ret;
        }

    }
}
