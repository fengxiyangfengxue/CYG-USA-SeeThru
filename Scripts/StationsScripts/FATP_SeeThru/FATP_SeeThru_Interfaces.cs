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
using UserHelpers.Helpers;
using Test.ModbusTCP;
using Test.Modules.SerialMotion;
using Test.Modules.motion_control;
using Test.StationsScripts.FATP_SeeThru;


namespace Test
{
    public partial class MainClass : IDisposable
    {
        internal HardwareControl hardControl;
        public SeeThru_Setting _SeethruSetting = null;
        public SeeThru_Context _SeeThruContext = null;
        public Dictionary<string, object> jsonConfigData = null;
        public Dictionary<string , object> jsonCmdData = null;
        bool _isSeeThruControlSeverInitilized = false;


        [MainClassConstructor(TEST_STATION.FATP_SeeThru, level: 10)]
        public int MainClassConstructor_SeeThru_Test()
        {
            _SeethruSetting = XmlSettingHelper.LoadSetting<SeeThru_Setting>(CaesarConfigPath, _Config.StartupConfig.Station);
            if (_SuperCalSetting == null)
                throw new Exception("load SeeThru xml config failed!");

            bool _MotionControlInit = FixtureInit_SeeThru_MotionControl();
            if (!_MotionControlInit)
                throw new Exception("initial MotionControl Server failed!");


            UIMessageBox.Show(Project, "Click OK to start SeeThru!", "waiting for start", UIMessageBoxButton.OK, 24, 14);
            return 0;

        }


        [ScriptInitialize(TEST_STATION.FATP_SeeThru, level: 20)]
        public int Script_Initialize_SeeThru_Test(ITestItem item)
        {
            _SeeThruContext = new SeeThru_Context();

            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Testing...", 14, 14, Colors.Black, Colors.Orange);

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

        // todo:需要把这个函数加到第一个函数里面吗？
        [MainClassConstructor(TEST_STATION.FATP_SeeThru, level: 30)]
        public int MainClassConstructor_SeeThru_Json()
        {
            jsonCmdData = ReadWriteJson.LoadJsonConfig(Path.Combine(CaesarConfigPath, (_Config.StartupConfig.Station).ToString(), "adb_shell.json"));
            jsonConfigData = ReadWriteJson.LoadJsonConfig(Path.Combine(CaesarConfigPath, (_Config.StartupConfig.Station).ToString(), "config.json"));
            if (jsonCmdData == null || jsonConfigData == null)
                throw new Exception("load json config failed!");

            return 0;

        }

        [MainClassConstructor(TEST_STATION.FATP_SeeThru, level: 40)]
        public bool FixtureInit_SeeThru_MotionControl()
        {
            bool result = false;

            if (!_isSeeThruControlSeverInitilized)
            {
                // initilize 
                try
                {
                    UIMessageBox.Show(Project, "Click OK to start thru222222222!", "waiting for start", UIMessageBoxButton.OK, 24, 14);
                    HardwareControl hardwareControl = new HardwareControl();
                    _Context.MotionCotrol = hardwareControl;

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
                _isSeeThruControlSeverInitilized = true;

            ReturnAndExit:

            return result;
        }




        //   会自动弹窗等待
        //[TriggerAttribute(TEST_STATION.FATP_SeeThru, level: 10)]
        public int Test_Trigger_FATP_SeeThru(ITimeLogger logger)
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



        [BeforeTesting(TEST_STATION.FATP_SeeThru, level: 70)]
        public int BeforeTesting_SeeThru(ITimeLogger logger)
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

        [TriggerAttribute(TEST_STATION.FATP_SeeThru, level: 10)]
        public int Test_Trigger_SeeThru(ITimeLogger logger)
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


        


        [AfterScript(TEST_STATION.FATP_SeeThru, level: 10)]
        public int AfterScript_SeeThru_Test(ITimeLogger logger)
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


        [BeforeSavingLog(TEST_STATION.FATP_SeeThru, level: 10)]
        public int BeforeSavingLog_SeeThru_Test(ITimeLogger logger)
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

        [LogFilter(TEST_STATION.FATP_SeeThru, level: 10)]
        public int LogFilter_SeeThru_Test(ILogInformation logInfo, ITimeLogger logger)
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


        [AfterSavingLog(TEST_STATION.FATP_SeeThru, level: 10)]
        public int AfterSavingLog_SeeThru_Test()
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


        [BeforeShowingResult(TEST_STATION.FATP_SeeThru, level: 10)]
        public int BeforeShowingResult_SeeThru_Test()
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



        [AfterTesting(TEST_STATION.FATP_SeeThru, level: 10)]
        public int AfterTesting_SeeThru_Test()
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
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterTesting failed!", "AfterTesting fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }


            return 0;
        }


        [AfterClosed(TEST_STATION.FATP_SeeThru, level: 10)]
        public int AfterClosed_SeeThru_Test()
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
