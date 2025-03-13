using System;
using System.Collections.Generic;
using System.Net;
using UserHelpers.Helpers;
using System.Windows.Forms;
using System.IO;
using Test._ScriptHelpers;
using Test.ScriptSettings;
using Test.Definition;
using Test._Definitions;
using Test._ScriptExtensions;
using Test._App;
using System.Threading.Tasks;
using Test.StationsScripts.Shared;
using MetaHelpers.ScriptHelpers;
using System.Windows.Media;
using System.Threading;
using System.Windows.Controls.Primitives;

namespace Test
{ 
    public partial class MainClass : IMainClass
    {
        ITestProject Project = null;
        LINE_TYPE _LineType = LINE_TYPE.NO_NAME;
        TEST_STATION _Station = TEST_STATION.ANY_STATION;

        CommonSetting _commonSetting = null;
        MESSetting _mesSetting = null;
        BuildPhaseSetting _buildSetting = null;
        ScannerSetting _scannerSetting = null;
        ADBLocationSetting _adbLocationSetting = null;
        AppraiserHelper _appraiser = new AppraiserHelper();
        
        public TestContext _Context = null;

        //public static string CaesarConfigPath = @"C:\Caesar\Configs";
        public static string CaesarConfigPath = @"Configs";
        public static string LocalConfigPath = @"LocalConfigs";

        public MainClass(object project)
        { 
            Project = (ITestProject)project;


            ConstKeys.XmlConfigredSlots = Project.TestConfig.ConfigSlots;
            Project.AddErrorCode(CreateErrorCode("ScriptException"));
            Project.AddErrorCode(CreateErrorCode("DefaultError"));
            Project.SetLogViewerHeaders(new List<LogViewerHeader>()
            {
                new LogViewerHeader("TestName","TestName", LoggerTextAlignment.Left), 
                new LogViewerHeader("Status","Status", LoggerTextAlignment.Center),
                new LogViewerHeader("Value","Value", LoggerTextAlignment.Right),
                new LogViewerHeader("LowerLimit","LowerLimit", LoggerTextAlignment.Right),
                new LogViewerHeader("UpperLimit","UpperLimit", LoggerTextAlignment.Right),
                new LogViewerHeader("Unit","Unit", LoggerTextAlignment.Center),
                new LogViewerHeader("Result","Result", LoggerTextAlignment.Center),
                new LogViewerHeader("Message","Message", LoggerTextAlignment.Left),
                new LogViewerHeader("ItemTitle","ItemTitle", LoggerTextAlignment.Left),
                new LogViewerHeader("RetryIndex","RetryIndex", LoggerTextAlignment.Center),
                new LogViewerHeader("TotalIndex","TotalRetryIndex", LoggerTextAlignment.Center),
                new LogViewerHeader("IsLatest","IsLatest", LoggerTextAlignment.Center),
                new LogViewerHeader("TestStartTime","TestStartTime", "{0:MM-dd-yyyy HH:mm:ss.fff}", LoggerTextAlignment.Center),
                new LogViewerHeader("TestEndTime","TestEndTime", "{0:MM-dd-yyyy HH:mm:ss.fff}", LoggerTextAlignment.Center),
                new LogViewerHeader("TestSeconds","Ticks", LoggerTextAlignment.Center),
                new LogViewerHeader("ECName","ECName", LoggerTextAlignment.Left),
                new LogViewerHeader("ErrorCode","Error.ErrorCode", LoggerTextAlignment.Center),
                new LogViewerHeader("CustomerCode","Error.CustomerCode", LoggerTextAlignment.Center),
                new LogViewerHeader("ErrorDescription","Error.ErrorDescription", LoggerTextAlignment.Left),
            });


            _Context = new TestContext();
            var di = new DirectoryInfo(Path.Combine("tmpfiles\\slot" + (Project.ProjectIndex + 1).ToString()) + "\\tmp");
            if (!di.Exists)
                di.Create();
            _Context.TmpFolder = di.FullName;
            _Context.SlotFolder = di.Parent.FullName;

            di = new DirectoryInfo(Path.Combine("tmpfiles\\slot" + (Project.ProjectIndex + 1).ToString()) + "\\backup");
            if (!di.Exists)
                di.Create();
            _Context.BackupFolder = di.FullName;

            _mesSetting = XmlSettingHelper.LoadSetting<MESSetting>(CaesarConfigPath);
            _commonSetting = XmlSettingHelper.LoadSetting<CommonSetting>(CaesarConfigPath);
            _buildSetting = XmlSettingHelper.LoadSetting<BuildPhaseSetting>(CaesarConfigPath);
            _Context.OperatorID = _mesSetting.UserName;

            _Config = LoadTestConfig();
            _Station = _Config.StartupConfig.Station;
            _LineType = _Config.StartupConfig.LineType;
            _Config.LoadConfig(_commonSetting, _mesSetting, _buildSetting);
            Project.WaterMark = _mesSetting.TesterNames[Project.ProjectIndex].Value;

            //Select_TestMode();

            Load_Limits();
            Load_Assembly_Resolvers();

            //App初始化时执行
            MainClassConstructor_Invoke();
             
        }


        //public void delayTest(ITestItem item)
        //{
        //    item.Sleep(500);
        //}


        public int NewTestItem123(ITestItem item)
        {
            item.AddLog("I'm a new test item123.");
            return 1;
        }

        public int Script_Initialize(ITestItem item)
        {
            bool result = false;
            try
            { 
                HttpWebRequest.DefaultWebProxy = null;
                //清空上下文
                _Context.Reset();
                _Context.TestStartTime = item.TestStartTime;
                _Context.TestEndTime = DateTime.Now;
                _Context.SlotId = (Project.ProjectIndex + 1).ToString();
                Project.ProjectDictionary.Clear();
                _Context.ScriptMode = Script_Mode.Test;

                //reload configs
                _mesSetting = XmlSettingHelper.LoadSetting<MESSetting>(CaesarConfigPath);
                _commonSetting = XmlSettingHelper.LoadSetting<CommonSetting>(CaesarConfigPath);
                _buildSetting = XmlSettingHelper.LoadSetting<BuildPhaseSetting>(CaesarConfigPath);

                _Config.LoadConfig(_commonSetting, _mesSetting, _buildSetting);
            
                Load_Limits();
                Project.SideBar.TopBar.Clear();
                RefreshTopBar();
                //在每个模块写一个Initialize特性的方法,来执行初始化
                Script_Initialize_Invoke(item);

                result = true;
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


        [ScriptInitialize(TEST_STATION.ANY_STATION, level: 0)]
        public int Script_Initialize_AllStations(ITestItem item)
        {
            _Context.TSRID = _Context.TestStartTime.ToString("yyyyMMddHHmmssfff") + (Project.ProjectIndex + 1).ToString("D2");
            item.AddLog("TSRID = " + _Context.TSRID);
            Project.PathDictionary["TSRID"] = _Context.TSRID;
            Project.PathDictionary["PCName"] = Environment.MachineName;
            Project.PathDictionary["SN"] = "NoSerialNumber";

            if (!string.IsNullOrEmpty(Project.SerialNumber))
            {
                Project.SideBar.TopBar.Add("SN", Project.SerialNumber);
                Project.PathDictionary["SN"] = Project.SerialNumber;
            }

            Project.PathDictionary["TestStartTime"] = _Context.TestStartTime.ToString("yyyyMMddHHmmssfff");
            Project.PathDictionary["TestEndTime"] = _Context.TestEndTime.ToString("yyyyMMddHHmmssfff");
            Project.PathDictionary["yyyy-MM-dd"] = DateTime.Now.ToString("yyyy-MM-dd");
            Project.PathDictionary["yyyyMMdd"] = DateTime.Now.ToString("yyyyMMdd");
            Project.PathDictionary["Product"] = _Config.Product;
            Project.PathDictionary["CM"] = _Config.CM;
            Project.PathDictionary["LineName"] = _mesSetting.LineName;
            Project.PathDictionary["StationDesc"] = _mesSetting.StationDescs[Project.ProjectIndex].Value;
            Project.PathDictionary["SlotID"] = (Project.ProjectIndex + 1).ToString();
            Project.PathDictionary["LineID"] = _mesSetting.LineID;
            Project.PathDictionary["StationNumber"] = _mesSetting.StationNumbers[Project.ProjectIndex].Value;
            Project.PathDictionary["StationID"] = _mesSetting.LineID;
            Project.PathDictionary["TesterName"] = _mesSetting.TesterNames[Project.ProjectIndex].Value;
            Project.PathDictionary["ErrorCode"] = string.Empty;
            Project.PathDictionary["ServerTime"] = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            Project.PathDictionary["StationName"] = _mesSetting.StationName;
            Project.PathDictionary["ScriptMode"] = _Context.ScriptMode.ToString();

            AddResult(item, new ResultData("TSRID", "", "_" + _Context.TSRID));
            AddResult(item, new ResultData("Test_StartTime", "", _Context.TestStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            AddResult(item, new ResultData("PCName", "", Environment.MachineName));
            AddResult(item, new ResultData("Product_Name", "", _Config.Product));
            AddResult(item, new ResultData("LineID", "", _mesSetting.LineID));
            AddResult(item, new ResultData("Tester_Name", "", _mesSetting.TesterNames[Project.ProjectIndex].Value));
            AddResult(item, new ResultData("SlotID", "", (Project.ProjectIndex + 1).ToString()));
            AddResult(item, new ResultData("Author", "", Project.Signature.Author));
            AddResult(item, new ResultData("Version", "", Project.Signature.Version));
            AddResult(item, new ResultData(ConstKeys.Test_EndTime, "", _Context.TestEndTime.ToString("yyyy-MM-dd HH:mm:ss:fff")));
            AddResult(item, new ResultData(ConstKeys.TestTotalSeconds, "", (_Context.TestEndTime - _Context.TestStartTime).TotalSeconds.ToString()));

            item.AddLog("SFIS Mode = " + (Project.IsOnLine ? "ONLINE" : "OFFLINE"));

            if (_Config.StartupConfig.IsPrintLimits)
                item.AddLog("Limit = " + Environment.NewLine + _Limits.ToLog());
             
            try
            {
                var di = new DirectoryInfo(_Context.SlotFolder);
                if (!di.Exists)
                    di.Create();
                 
                ClearDirectory(di.FullName);

                di = new DirectoryInfo(_Context.TmpFolder);
                if (!di.Exists)
                    di.Create();

                di = new DirectoryInfo(_Context.BackupFolder);
                if (!di.Exists)
                    di.Create();

                Project.BackUp.BackupDirectory(di.FullName);

            }
            catch { }



            return 0;
        }


        public int Package_Verify(ITestItem item)
        {
            bool result = false;
            try
            { 


                result = ShellHelper.RunHideRead(item.AddLog, "CaesarMD5.exe", $"/CMD5 -list {_Station}.checksum -hide ", 30000, 0);

                if (!result)
                {
                    UIMessageBox.Show(Project.AppWindow, "程式包已被修改，请恢复后再进行测试!", "程式包被修改", UIMessageBoxButton.OK, 20);
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

        public int NewTestItem(ITestItem item)
        {
            item.AddLog("I'm a new test item.");
            return 0;
        }

        public int ParentNode(ITestItem item)
        {
            item.ItemDictionary["ParentValue"] = "test value";
            return 0;
        }

        public int ChildNode(ITestItem item)
        {
            item.AddLog("in childenode: Parent Dic Value = " + item.IParent.ItemDictionary["ParentValue"].ToString());
            item.IParent.ItemDictionary["ParentValue"] = "test value child";
            return 0;
        }

        public int ChildNode2(ITestItem item)
        {
            item.AddLog("in childenode2: Parent Dic Value = " + item.IParent.ItemDictionary["ParentValue"].ToString());
            return 0;
        }

        public int NewTestItem2(ITestItem item)
        {
            item.AddLog($"New Test Item: {item.Title}");
            return 0;
        }

        public int NewTestItem3(ITestItem item, string name, double def=123.456)
        {
            item.AddLog("I'm test item3.");
            return 0;
        }

        public int NewTestItem4(ITestItem item, string name, double def = 123.456)
        {
            item.AddLog("I'm test item4.");
            return 0;
        }

        public int ItemDicTest(ITestItem item)
        {
            if (item.ItemDictionary.ContainsKey("ID"))
            {
                item.ItemDictionary["ID"] = (int)item.ItemDictionary["ID"] + 1;
            }
            else
            {
                item.ItemDictionary["ID"] = 0;
            }

            item.AddLog("ID = " + item.ItemDictionary["ID"].ToString());
            return 0;
        }

        public int ProjectDicTest(ITestItem item)
        {
            if (Project.ProjectDictionary.ContainsKey("ID"))
                Project.ProjectDictionary["ID"] = (int)Project.ProjectDictionary["ID"] + 1;
            else
                Project.ProjectDictionary["ID"] = 0;
            item.AddLog("Project Dic ID = " + Project.ProjectDictionary["ID"].ToString());
            return 0;
        }

        public int AppDicTest(ITestItem item)
        {
            if (Project.AppDictionary.ContainsKey("ID"))
                Project.AppDictionary["ID"] = (int)Project.AppDictionary["ID"] + 1;
            else
                Project.AppDictionary["ID"] = 0;

            item.AddLog("App Dic ID = " + Project.AppDictionary["ID"].ToString());
            return 0;

        }

        public int TestFunction(ITestItem item)
        {
            bool result = false;
            try
            {
                // do test ....

                // generate fail data
                item.AddResultData(new ResultData(item.Title, CreateErrorCode(item.Title).Name, "value1"));
                throw new Exception("script error!"); //throw an exception manally
                result = true;
            }
            catch (Exception ex) //catch exception yourself
            {
                item.AddLog(ex.ToString() );
            }
            return result ? 0 : 1;
        }

        public int TestPass(ITestItem item)
        {
            item.Sleep(500);
            ResultData resultData = new ResultData(item.Title, "", "Pass");
            item.AddResultData(resultData);

            return 0;
        }

        public int TestFail(ITestItem item) 
        {
            item.Sleep(500);
            ECData ec = new ECData() { Name="ErrorCodeName", ErrorCode = "123456", ErrorDescription = "Errorcode Sample"};
            Project.AddErrorCode(ec);

            ResultData resultData = new ResultData(item.Title, ec.Name, "123.456");
            item.AddResultData(resultData);

            return 1;
        }

        public int TestAging(ITestItem item)
        {
            item.Sleep(500);
            item.AddLog("Aging Test.");
            return 0;
        }

        public int AddDataList(ITestItem item)
        {
            List<IResultData> dataList = new List<IResultData>();
            for (int i = 0; i < 10; i++)
            {
                dataList.Add(new ResultData(item.Title + "_" + i.ToString(), "", "123.456"));

            }

            item.AddResultData(dataList);
            return 0;
        }

        public int BarCodeSample(ITestItem item)
        {
            BarCodeConfig config = new BarCodeConfig()
            {
                Title = "Input barcode(length = 6)",
            };

            // check barcode length = 6

            config.ValidationHandler += (s) =>
            {
                return s.Length == 6;
            };


            string barcode = BarCodeHelper.Get(Project, config);
            item.AddLog("barcode = " + barcode);

            config.MakeLower = false;
            config.MakeUpper = true;
            config.TitleColor = Colors.Green;
            config.TitleFontSize = 12;
            config.TextBoxColor = Colors.Blue;
            config.TextBoxFontSize = 14;
            barcode = BarCodeHelper.Get(Project, config);
            item.AddLog("barcode = " + barcode);


            return 0;
        }

        public int AddInformation(ITestItem item)
        {
            Project.SideBar.TopBar.Add("SN1", "11111111111");
            Project.SideBar.TopBar.Add("SN2", "22222222222", Colors.Red);
            Project.SideBar.TopBar.Add("SN3", "33333333333", 12, 12, Colors.Black, Colors.Black);
            Project.SideBar.TopBar.Add("SN4", "44444444444", 16, 20, Colors.Blue, Colors.Green);

            Project.SideBar.RightBar.Add("SN1", "11111111111");
            Project.SideBar.RightBar.Add("SN2", "22222222222", Colors.Red);
            Project.SideBar.RightBar.Add("SN3", "33333333333", 12, 12, Colors.Black, Colors.Black);
            Project.SideBar.RightBar.Add("SN4", "44444444444", 16, 20, Colors.Blue, Colors.Green);

            Project.SideBar.BottomBar.Add("SN1", "11111111111");
            Project.SideBar.BottomBar.Add("SN2", "22222222222", Colors.Red);
            Project.SideBar.BottomBar.Add("SN3", "33333333333", 12, 12, Colors.Black, Colors.Black);
            Project.SideBar.BottomBar.Add("SN4", "44444444444", 16, 20, Colors.Blue, Colors.Green);

            return 0;

        }

        public int ModifyInformation(ITestItem item)
        {
            Project.SideBar.TopBar.Add("SN1", "new_11111111111");
            Project.SideBar.RightBar.Add("SN1", "new_11111111111");
            Project.SideBar.BottomBar.Add("SN1", "new_11111111111");

            return 0;
        }

        public int RemoveInformation(ITestItem item)
        {
            Project.SideBar.TopBar.Remove("SN1");
            Project.SideBar.RightBar.Remove("SN1");
            Project.SideBar.BottomBar.Remove("SN1");

            return 0;
        }

        public int ClearBottomInformation(ITestItem item)
        {
            Project.SideBar.BottomBar.Clear();
            return 0;
        }

        public int ClearAllInformation(ITestItem item)
        {
            Project.SideBar.Clear();
            return 0;
        }

        public int SFISControl(ITestItem item)
        {
            Project.IsOnLine = true;
            item.Sleep(1000);

            Project.IsOnLine = false;
            item.Sleep(1000);

            Project.IsOnLine = true;
            item.Sleep(1000);

            Project.IsOnLine = false;
            return 0;
        }

        public int SkipTest(ITestItem item)
        {
            string fwVersion = "V1.1";

            if(fwVersion.Equals("V1.1"))
            {
                item.IsSkipTest = true;
                return 0;
            }

            ResultData resultData = new ResultData(item.Title, "", "Pass");
            item.AddResultData(resultData);
            return 0;
        }

        public int MessageBox_CenterScreen(ITestItem item)
        {
            UIMessageBox.Show(null, "cneter screen");
            return 0;
        }
       
        public int MessageBox_CenterSlot(ITestItem item)
        {
            UIMessageBox.Show(Project, "center slot");
            return 0;
        }

        public int MessageBox_Normal(ITestItem item)
        {
            UIMessageBoxResult ret = UIMessageBox.Show(Project, "OKCancel", "title", UIMessageBoxButton.OKCancel);
            item.AddLog(ret.ToString());

            ret = UIMessageBox.Show(Project, "YesNoCancel", "title", UIMessageBoxButton.YesNoCancel);
            item.AddLog(ret.ToString());

            UIMessageBox.Show(Project, "FontSize = 20", "title", UIMessageBoxButton.OK, 20, Colors.Green);

            UIMessageBox.Show(Project, "FontSize = 20, ButtonFontSize = 20", "title", UIMessageBoxButton.OK, 20, Colors.Blue, 20);

            return 0;

        }

        public int MessageBox_CountDown(ITestItem item)
        {
            var config = new UIMessageBoxConfig()
            {
                Title = "title",
                Text = "press Ok please",
                TextFontSize = 20,
                TextColor = Colors.Green,
                Button = UIMessageBoxButton.OK,
                TimeOut = 3000
            };

            item.AddLog("start countdown");
            UIMessageBox.Show(Project, config);
            item.AddLog("message closed");

            return 0;
            
        }

        public int MessageBox_NonBlock(ITestItem item)
        {
            var config = new UIMessageBoxConfig()
            {
                Title = "title",
                Text = "press OK please",
                TextFontSize = 20,
                TextColor = Colors.Green,
                Button = UIMessageBoxButton.OK,
                WaitForExit = false
            };

            UIMessageBox.Show(Project, config);

            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);

            return 0;
        }

        public int MessageBox_NonBlock2(ITestItem item)
        {
            var config = new UIMessageBoxConfig()
            {
                Title = "title",
                Text = "press OK please",
                TextFontSize = 20,
                TextColor = Colors.Green,
                Button = UIMessageBoxButton.YesNo,
                IsClone = false,
                WaitForExit = false
            };
            UIMessageBox.Show(Project, config);

            while(true)
            {
                item.AddLog("code is running");
                item.Sleep(500);

                if(config.IsClosed)
                {
                    item.AddLog("messagebox closed");
                    item.AddLog("button = " + config.ResultButton);
                    break;
                }
            }
            return 0;
        }

        public int MessageBox_AliveWith(ITestItem item)
        {
            var config = new UIMessageBoxConfig()
            {
                Title = "title",
                Text = "press OK please!",
                TextFontSize = 20,
                TextColor = Colors.Green,
                Button = UIMessageBoxButton.OK,
                AliveWith = item, //alive with item
                WaitForExit = false //non-block
            };

            UIMessageBox.Show(Project, config);

            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");
            item.Sleep(500);
            item.AddLog("code is running");

            return 0;
        }

        public int EnumberateLimits(ITestItem item)
        {
            foreach(var k in _Limits.LimitDict.Keys)
            {
                var limit = _Limits.LimitDict[k];
                item.AddLog("LCL = " + limit.LCL.ToString() +
                    "UCL = " + limit.UCL.ToString() + ", " +
                    "[LCL] = " + limit.LCLClosedInterval.ToString() + ", " +
                    "[UCL] = " + limit.UCLClosedInterval.ToString() + ", " +
                    "Unit = " + limit.Unit + ", " +
                    "CheckString = " + limit.CheckString);
            }
            return 0;
        }

        public int RunShow(ITestItem item)
        {
            ShellHelper.RunShow(item.AddLog, "D:\\tmp\\console_app\\ConsoleApp1.exe", "arg0 arg1 arg2");
            return 0;
        }

        public int RunHide(ITestItem item)
        {
            ShellHelper.RunHide(item.AddLog, "D:\\tmp\\console_app\\ConsoleApp1.exe", "arg0 arg1 arg2");
            return 0;
        }

        public int RunHideRead(ITestItem item)
        {
            ShellHelper.RunHideRead(item.AddLog, "D:\\tmp\\console_app\\ConsoleApp1.exe", "arg0 arg1 arg2", 5000);
            return 0;
        }

        public int RunHideRead_GetLog(ITestItem item)
        {
            string logs = string.Empty;
            int exitCode = 0;
            ShellHelper.RunHideRead(item.AddLog, "D:\\tmp\\console_app\\ConsoleApp1.exe", "arg0 arg1 arg2", 5000, false, ref exitCode, ref logs);

            item.AddLog("read logs = " + logs);
            item.AddLog("exitCode = " + exitCode);
            return 0;
        }

        public int RunHideRead_TimeOut(ITestItem item)
        {
            ShellHelper.RunHideRead(item.AddLog, "D:\\tmp\\console_app2\\ConsoleApp1.exe", "arg0 arg1 arg2", 2500);
            return 0;
        }

        public int RunWaitAlive(ITestItem item)
        {
            ShellHelper.RunWaitAlive(item.AddLog, "D:\\tmp\\console_app3\\ConsoleApp1.exe", "arg0 arg1 arg2", "Caesar Test", 2500);
            return 0;
        }

        ConsoleHelper _console = null;
        
        void WaitForPrompt()
        {
            while (true)
            {
                if (_console.Logs.EndsWith("Caesar>"))
                    break;
                Thread.Sleep(10);
            }
        }

        public int AddInfo(ITestItem item)
        {
            int targetIndex = Project.ProjectIndex == 0 ? 1 : 0;
            Project.Projects[targetIndex].SideBar.TopBar.Add("Info", "Added By Slot" + (Project.ProjectIndex + 1));
            return 0;
        }

        public string SharedString = string.Empty;
        public int SetStringValue(ITestItem item)
        {
            int targetIndex = Project.ProjectIndex == 0 ? 1 : 0;
            MainClass mainClass = Project.Projects[targetIndex].GetInstance<MainClass>();
            mainClass.SharedString = "SetBySlot" + (Project.ProjectIndex + 1);

            return 0;
        }

        public int ShowString(ITestItem item)
        {
            Project.SideBar.TopBar.Add("Value", SharedString);
            return 0;
        }

    }
}