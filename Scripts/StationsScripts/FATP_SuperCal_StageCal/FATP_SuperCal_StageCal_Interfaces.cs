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
using Test.StationsScripts.FATP_SuperCal_StageCal; 
using UserHelpers.Helpers;
using Test.ModbusTCP;
using Test.Modules.SerialMotion;

namespace Test
{
    public partial class MainClass
    {
        
        SuperCalStageCal_Context _SuperCalStageCalContext = null;
        StageCalSetting _SuperCalStageCalSetting = null;
        bool _isStageCalPLCInitilized = false;

        [MainClassConstructor(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int MainClassConstructor_SuperCalStageCal_Test()
        {
            _SuperCalStageCalSetting = XmlSettingHelper.LoadSetting<StageCalSetting>(CaesarConfigPath, _Config.StartupConfig.Station);
            if (_SuperCalStageCalSetting == null)
                throw new Exception("load SuperCal failed!");

            bool _plc_init = FixtureInit_SuperCalStageCal_PLC();
            if (!_plc_init)
                throw new Exception("initial  PLC failed!");

            bool _Hc_plc_init = FixtureInit_SuperCalStageCal_BfPLC();

            bool _init_Motion_path = FixtureInit_SuperCalStageCal_MotionPath();
            if ((!_Hc_plc_init )|| (!_init_Motion_path))
            {
                throw new Exception("initial  PLC failed!");

            }

            return 0;
        }

        [MainClassConstructor(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public bool FixtureInit_SuperCalStageCal_PLC()
        {
            bool result = false;

            if (!_isStageCalPLCInitilized)
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
                _isStageCalPLCInitilized = true;

            ReturnAndExit:

            return result;
        }


        [MainClassConstructor(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public bool FixtureInit_SuperCalStageCal_BfPLC()
        {
            bool result = false;
            //UIMessageBox.Show(Project, _Station.ToString() + "BeforeT3213213esting failed!", $"BeforeTesting fail-->", UIMessageBoxButton.OK, 14, Colors.Red);

            if (!_isStageCalPLCInitilized)
            {
                // initilize PLC
                try
                {
                    //UIMessageBox.Show(Project, _Station.ToString() + "BeforeT3213213esting failed!", $"BeforeTesting fail-->", UIMessageBoxButton.OK, 14, Colors.Red);

                     BfPLC = new PlcMotion("Com1", 115200);

                    //BfPLC = Motion;
                   
                }
                catch (Exception ex)
                {
                    UIMessageBox.Show(Project, ex.ToString());
                    goto ReturnAndExit;
                }
                result = true;
            }

            else
                _isStageCalPLCInitilized = true;

            ReturnAndExit:

            return result;
        }
        [MainClassConstructor(TEST_STATION.FATP_SuperCal, level: 10)]
        public bool FixtureInit_SuperCalStageCal_MotionPath()
        {
            bool result = false;

            if (!_isStageCalPLCInitilized)
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
                _isStageCalPLCInitilized = true;

            ReturnAndExit:

            return result;
        }


        [ScriptInitialize(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int Script_Initialize_SuperCalStageCal_Test(ITestItem item)
        {
            _SuperCalStageCalContext = new SuperCalStageCal_Context();

            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Testing...", 14, 14, Colors.Black, Colors.Orange);

            _SuperCalStageCalSetting = XmlSettingHelper.LoadSetting<StageCalSetting>(CaesarConfigPath, _Station);

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



        [AfterScript(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int AfterScript_SuperCalStageCal_Test(ITimeLogger logger)
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


        [BeforeSavingLog(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int BeforeSavingLog_SuperCalStageCal_Test(ITimeLogger logger)
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

        [LogFilter(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int LogFilter_SuperCalStageCal_Test(ILogInformation logInfo, ITimeLogger logger)
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


        [AfterSavingLog(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int AfterSavingLog_SuperCalStageCal_Test()
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


        [BeforeShowingResult(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int BeforeShowingResult_SuperCalStageCal_Test()
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



        [AfterTesting(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int AfterTesting_SuperCalStageCal_Test()
        {
            bool result = false;
            try
            {
                _SuperCalStageCalContext.ClearUp();
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


        [AfterClosed(TEST_STATION.FATP_SuperCal_StageCal, level: 10)]
        public int AfterClosed_SuperCalStageCal_Test()
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


        [MainClassDisposeAttribute(TEST_STATION.FATP_SuperCal_StageCal)]
        public int MainClassDispose_SuperCalStageCal_Test()
        {
            bool result = false;
            try
            {
                _SuperCalStageCalContext.Dispose();
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
                    UIMessageBox.Show(Project, _Station.ToString() + "Dispose failed!", "Dispose fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }

            return 0;
        }


    }
}
