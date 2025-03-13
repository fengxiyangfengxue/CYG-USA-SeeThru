using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.Definition;
using Test.StationsScripts.FATP_SuperCal; 
using UserHelpers.Helpers;
using Test.ModbusTCP;
using Test.Modules.SerialMotion;
using Test.StationsScripts.Shared;
using Test.HcLabelCommunication;


namespace Test
{
    public partial class MainClass : IDisposable
    {
        public static PlcMotion BfPLC;
        
        public static TcInt SocketCameraClient;
        public static TcInt SocketDataClient;


        SuperCal_Context _SuperCalContext = null;
        SuperCal_Setting _SuperCalSetting = null;
        bool _isButtonPLCInitilized = false;


        //public void Dispose()
        //{
        //    Dispose(false);
        //    GC.SuppressFinalize(this);
        //}
        //protected virtual void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        //释放托管资源 ：.NET运行时管理的资源，比如普通的C#对象。 *这些资源会由垃圾回收器（GC）自动清理
        //    }
        //    else
        //    {
        //        // 释放非托管资源： 如文件句柄，PLC句柄、数据库链接，（操作系统层面的资源）  *GC不会自动清理这些资源，需要手动清理
        //        if (bfPLC.IsOpen)
        //        {

        //            try
        //            {
        //                bfPLC.Close();
        //                bfPLC = null;
        //            }
        //            catch (Exception)
        //            {

        //                throw;
        //            }
        //        }
        //    }
        //}


        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 10)]
        public int MainClassConstructor_SuperCal_Test()
        {
            _SuperCalSetting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath, _Config.StartupConfig.Station);
            if (_SuperCalSetting == null)
                throw new Exception("load SuperCal failed!");

            bool _plc_init = FixtureInit_SuperCal_PLC();
            if (!_plc_init)
                throw new Exception("initial Button PLC failed!");

            bool _Hc_plc_init = FixtureInit_SuperCal_BfPLC();

            bool _init_Motion_path = FixtureInit_SuperCal_MotionPath();
            if ((!_Hc_plc_init )|| (!_init_Motion_path))
            {
                throw new Exception("initial  PLC failed!");
            }

            bool generateClintCam = FixtureInit_SuperCal_CamClint();
            if (!generateClintCam)
                throw new Exception("initial camClint  failed!");

            // todo
            bool generateSocketDataClient = FixtureInit_SuperCal_SocketDataClient();
            if (!generateSocketDataClient)
                throw new Exception("initial dataClint  failed!");

            return 0;


        }

        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 20)]
        public bool FixtureInit_SuperCal_PLC()
        {
            bool result = false;

            if (!_isButtonPLCInitilized)
            {
                // initilize PLC
                try
                {
                    
                    ModbusTcpClient modbusClient = new ModbusTcpClient("192.168.1.88");
                    
                    _Context.PLCClient = modbusClient;
                    Project.AppDictionary["PLCUpperBusy"] = true;

                }
                catch (Exception ex)
                {
                    UIMessageBox.Show(Project, ex.ToString());
                    goto ReturnAndExit;
                }
                result = true;
            }

            else
                _isButtonPLCInitilized = true;

            ReturnAndExit:

            return result;
        }


        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 30)]
        public bool FixtureInit_SuperCal_BfPLC()
        {
            bool result = false;
            //UIMessageBox.Show(Project, _Station.ToString() + "BeforeT3213213esting failed!", $"BeforeTesting fail-->", UIMessageBoxButton.OK, 14, Colors.Red);

            if (!_isButtonPLCInitilized)
            {
                // initilize PLC
                try
                {
                    //UIMessageBox.Show(Project, _Station.ToString() + "BeforeT3213213esting failed!", $"BeforeTesting fail-->", UIMessageBoxButton.OK, 14, Colors.Red);
                    if (Project.ProjectIndex == 0 )
                    {
                        BfPLC = new PlcMotion("Com1", 115200);

                        //BfPLC = Motion;
                    }
                   
                   
                }
                catch (Exception ex)
                {
                    UIMessageBox.Show(Project, ex.ToString());
                    goto ReturnAndExit;
                }
                result = true;
            }

            else
                _isButtonPLCInitilized = true;

            ReturnAndExit:

            return result;
        }


        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 40)]
        public bool FixtureInit_SuperCal_MotionPath()
        {
            bool result = false;

            if (!_isButtonPLCInitilized)
            {
                // initilize PLC
                try
                {
                    MotionPath motionPath = new MotionPath();

                    _Context.Motion_Path = motionPath;
                    //UIMessageBox.Show(Project, _Station.ToString() + "BeforeTesting failed!", $"BeforeTesting fail-->{_Context.Motion_Path}", UIMessageBoxButton.OK, 14, Colors.Red);
                    
                }
                catch (Exception ex)
                {
                    UIMessageBox.Show(Project, ex.ToString());
                    goto ReturnAndExit;
                }
                result = true;
            }

            else
                _isButtonPLCInitilized = true;

            ReturnAndExit:

            return result;
        }

        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 50)]
        public bool FixtureInit_SuperCal_CamClint()
        {
            bool result = false;

            try
            {
                if (Project.ProjectIndex == 0)
                {
                     SocketCameraClient = new TcInt(20237);
                }
                    
            }
            catch (Exception ex)
            {
                UIMessageBox.Show(Project, ex.ToString());
                goto ReturnAndExit;
            }
            result = true;

            ReturnAndExit:

            return result;
        }

        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 60)]
        public bool FixtureInit_SuperCal_SocketDataClient()
        {
            bool result = false;

            try
            {
                if (Project.ProjectIndex == 0)
                {
                    //UIMessageBox.Show(Project, "Click OK to start!", "waiting for start", UIMessageBoxButton.OK, 24, 14);
                    SocketDataClient = new TcInt();
                }

            }
            catch (Exception ex)
            {
                UIMessageBox.Show(Project, ex.ToString());
                goto ReturnAndExit;
            }
            result = true;

            ReturnAndExit:

            return result;
        }

        //  2.18 会自动弹窗等待
        //[TriggerAttribute(TEST_STATION.FATP_SuperCal, level: 10)]
        public int Test_Trigger_FATP_SuperCal(ITimeLogger logger)
        {
            RefreshTopBar();
            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);

            bool isSkipTrigger = false;

            if (IsLooping())
            {
                isSkipTrigger = (_Context.TestCount < _commonSetting.LoopTimes) && (_commonSetting.LoopTimes >= 0);
                if (isSkipTrigger || _commonSetting.LoopTimes == 0)
                {
                    logger.AddLog("looping " + (_Context.TestCount + 1));
                    return 0;
                }

                UIMessageBox.Show(Project, "Loop finished!", "Loop", UIMessageBoxButton.OK, 24, 14);
            }

            if (!isSkipTrigger)
                UIMessageBox.Show(Project, "Click OK to start!", "waiting for start", UIMessageBoxButton.OK, 24, 14);

            return 0;
        }



        [BeforeTesting(TEST_STATION.FATP_SuperCal, level: 70)]
        public int BeforeTesting_SuperCal_Test(ITimeLogger logger)
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
                    UIMessageBox.Show(Project, _Station.ToString() + "BeforeTesting failed!", "BeforeTesting fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }
            return 0;
        }

        [TriggerAttribute(TEST_STATION.FATP_Audio, level: 10)]
        public int Test_Trigger_SuperCal_Test(ITimeLogger logger)
        {
            RefreshTopBar(); 
            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);

            bool isSkipTrigger = false;

            if (IsLooping())
            {
                isSkipTrigger = (_Context.TestCount < _commonSetting.LoopTimes) && (_commonSetting.LoopTimes >= 0);
                if (isSkipTrigger || _commonSetting.LoopTimes == 0)
                {
                    logger.AddLog("looping " + (_Context.TestCount + 1));
                    return 0;
                }

                UIMessageBox.Show(Project, "Loop finished!", "Loop", UIMessageBoxButton.OK, 24, 14);
            }

            if (!isSkipTrigger)
                UIMessageBox.Show(Project, "Click OK to start!", "waiting for start", UIMessageBoxButton.OK, 24, 14);

            return 0;
        }


        [ScriptInitialize(TEST_STATION.FATP_SuperCal, level: 80)]
        public int Script_Initialize_SuperCal_Test(ITestItem item)
        {
            _SuperCalContext = new SuperCal_Context();

            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Testing...", 14, 14, Colors.Black, Colors.Orange);

            _SuperCalSetting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(CaesarConfigPath, _Station);

            try
            {
                var di = new DirectoryInfo(_Context.TmpFolder);
                if (!di.Exists)
                    di.Create();

                Project.BackUp.BackupDirectory(di.FullName);

                ClearDirectory(di.FullName);
            }
            catch { }

            return 0;
        }



        [AfterScript(TEST_STATION.FATP_SuperCal, level: 10)]
        public int AfterScript_SuperCal_Test(ITimeLogger logger)
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
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterScript failed!", "AfterScript fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }
            return 0;
        }


        [BeforeSavingLog(TEST_STATION.FATP_SuperCal, level: 10)]
        public int BeforeSavingLog_SuperCal_Test(ITimeLogger logger)
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
                    UIMessageBox.Show(Project, _Station.ToString() + "BeforeSavingLog failed!", "BeforeSavingLog fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }
            return 0;
        }

        [LogFilter(TEST_STATION.FATP_SuperCal, level: 10)]
        public int LogFilter_SuperCal_Test(ILogInformation logInfo, ITimeLogger logger)
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
            return 0;
        }


        [AfterSavingLog(TEST_STATION.FATP_SuperCal, level: 10)]
        public int AfterSavingLog_SuperCal_Test()
        {
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


        [BeforeShowingResult(TEST_STATION.FATP_SuperCal, level: 10)]
        public int BeforeShowingResult_SuperCal_Test()
        {
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
                    UIMessageBox.Show(Project, _Station.ToString() + "BeforeShowingResult failed!", "BeforeShowingResult fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }

            return 0;
        }



        [AfterTesting(TEST_STATION.FATP_SuperCal, level: 10)]
        public int AfterTesting_SuperCal_Test()
        {
            bool result = false;
            try
            {
                _SuperCalContext.ClearUp();
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
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterTesting failed!", "AfterTesting fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }


            return 0;
        }


        [AfterClosed(TEST_STATION.FATP_SuperCal, level: 10)]
        public int AfterClosed_SuperCal_Test()
        {
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
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterClosed failed!", "AfterClosed fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }

            return 0;
        }


        //[MainClassDisposeAttribute(TEST_STATION.SuperCal_Test)]
        //public int MainClassDispose_SuperCal_Test()
        //{
        //    bool result = false;
        //    try
        //    {
        //        _SuperCalContext.Dispose();
        //        result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        SaveExtraLogs(ex.ToString());
        //        UIMessageBox.Show(Project, ex.ToString());
        //    }
        //    finally
        //    {
        //        if (!result)
        //        {
        //            UIMessageBox.Show(Project, _Station.ToString() + "Dispose failed!", "Dispose fail", UIMessageBoxButton.OK, 14, Colors.Red);
        //        }
        //    }

        //    return 0;
        //}
        

    }
}
