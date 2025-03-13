using System;
using UserHelpers.Helpers;
using Test._App;
using Test.Definition;
using Test._Definitions;
using System.Windows.Media;
using System.IO;
using Test._ScriptExtensions; 
using Test.StationsScripts.Shared;

namespace Test
{
    public partial class MainClass : IDisposable, IMainClass
    {
        bool _isDisplsed = false;
        public bool IsTestFinished = true;
        public static readonly object SummaryLocker = new object();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool isDisposing)
        {
            if (!_isDisplsed)
            {
                if (isDisposing) //如果有未释放的资源要释放，在这里添加，如COM没关闭，Socket没关闭等
                {

                    //BfPLC.CloseBf();
                    BfPLC.Dispose();
                    SocketCameraClient.CloseTcpClient();
                    SocketDataClient.CloseTcpClient();
                    HcPLCTag.Dispose();

                    Unload_Assembly_Resolvers();

                    try
                    {
                        Script_Dispose_Invoke();
                    }
                    catch { }
                }
            }
            _isDisplsed = true;
        }

        public int App_BeforeTesting(ITimeLogger logger)
        {
            RefreshTopBar();
            //Input_OPID(); 




            App_BeforeTesting_Invoke(logger);

            if (Project.IsDebug) //Debug
            {
                Project.SerialNumber = string.Empty;
                Test_Trigger_Invoke(logger);
            }
            else //Release模式
            {
                switch (_Config.StartupConfig.TriggerMode)
                {
                    case TestTriggerMode.ParallelIndividually:
                        RunMode_ParallelIndividually(logger);
                        break;

                    case TestTriggerMode.ParallelFixtureReady:
                        RunMode_ParallelFixtureReady(logger);
                        break;

                    case TestTriggerMode.ParallelFirstOneReady:
                        RunMode_ParallelFirstOneReady(logger);
                        break;

                    case TestTriggerMode.Sequential:
                        RunMode_Sequential(logger);
                        break;

                    default:
                        RunMode_Error(logger);
                        break;
                }
            }

            return 0;
        }

        public IResultData App_CreateResultData(string item, string ecName, string value)
        {
            return new ResultData(item, ecName, value);
        }

        public IDataCollector App_GetDataCollector()
        {
            return new DataCollector(Project);
        }

        public int App_AfterScript(ITimeLogger logger)
        {
            App_AfterScript_Invoke(logger);
            return 0;
        }

        public int App_BeforeSavingLog(ITimeLogger logger)
        {
            App_BeforeSavingLog_Invoke(logger);
            return 0;
        }

        public int App_LogFilter(ILogInformation logInfo, ITimeLogger logger)
        { 

            bool result = false;
            try
            {

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
                    UIMessageBox.Show(Project, _Station.ToString() + "LogFilter failed!", "LogFilter fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }

            App_LogFilter_Invoke(logInfo, logger);

            return 0;
        }

        public int App_AfterSavingLog()
        {
            App_AfterSavingLog_Invoke();

            bool result = false;

            try
            {

                result = true;
            }
            catch (Exception ex)
            {
                SaveExtraLogs(ex.ToString());
                UIMessageBox.Show(Project, ex.ToString());
            }
            finally
            {
                if (!result)
                {
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterSavingLog failed!", "AfterSavingLog fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }
             
            return 0;
        }

        public int App_BeforeShowingResult()
        {
            App_BeforeShowingResult_Invoke();
            return 0;
        }

        public int App_AfterTesting()
        {
            //上传QDF,清理隐藏设备,上传PLC结果
            App_AfterTesting_Invoke();
            RefreshTopBar();
            IsTestFinished = true;


            //try
            //{
            //    string ec = string.Empty;
            //    Project.GetErrorCodeList().ForEach(e =>
            //    {
            //        ec = ec + e.ErrorCode + "," + e.ErrorDescription + Environment.NewLine;
            //    });

            //    FileInfo fi = new FileInfo(Path.Combine(_Context.TmpFolder, "eclist.csv"));
            //    if (!fi.Directory.Exists)
            //        fi.Directory.Create();
            //    File.WriteAllText(fi.FullName, ec); 
            //}
            //catch { }

            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);
            _Context.ClearUp();
            return 0;
        }

        public int App_AfterClosed()
        {
            App_AfterClosed_Invoke();
            return 0;
        }


    }
}