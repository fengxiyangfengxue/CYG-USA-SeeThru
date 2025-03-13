
using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using Test._App;
using Test.Definition;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.StationsScripts.FATP_Button;
using UserHelpers.Helpers;
using MetaHelpers.ScriptHelpers;
using Test.ModbusTCP;
using NModbus;

namespace Test
{

    public partial class MainClass
    {
        FATP_Button_Context _buttonContext = null;

        ConsoleHelper _pyCameraFixture = null;
        FATP_CameraSetting _cameraSetting = null;
        FATP_CameraBlemishSetting _blemishPySetting = null;
        bool _isCameraFixtureInitilized = false;

        bool _isButtonPLCInitilized_button = false;        //和super中的冲突 改名 2.13


        [MainClassConstructor(TEST_STATION.FATP_Button, level: 10)]
        public int MainClassConstructor_FATP_Button()
        {
            _buttonContext = new FATP_Button_Context();
            _cameraSetting = XmlSettingHelper.LoadSetting<FATP_CameraSetting>(CaesarConfigPath, _Config.StartupConfig.Station);
            if (_cameraSetting == null)
                throw new Exception("load FATP_CameraConfig failed!");

            _blemishPySetting = XmlSettingHelper.LoadSetting<FATP_CameraBlemishSetting>(CaesarConfigPath, _Config.StartupConfig.Station);
            if (_blemishPySetting == null)
                throw new Exception("load FATP_CameraBlemishSetting failed!");

            bool _plc_init = FixtureInit_Button_PLC();
            if (!_plc_init)
                throw new Exception("initial Button PLC failed!");

            return 0;
        }

        bool FixtureInit_Button_PLC()
        {
            bool result = false;

            if (!_isButtonPLCInitilized)
            {
                // initilize PLC
                try
                {
                    ModbusTcpClient modbusClient = new ModbusTcpClient("192.168.1.10");
                    ModbusTcpClient modbusClientR = new ModbusTcpClient("192.168.1.10");
                    ModbusTcpClient modbusClientData = new ModbusTcpClient("192.168.1.10");
                    _Context.PLCClient = modbusClient;
                    _Context.PLCClientR = modbusClientR;
                    _Context.PLCClientData = modbusClientData;
                    Project.AppDictionary["PLCUpperBusy"] = true;
                }
                catch (Exception ex)
                {
                    goto ReturnAndExit;
                }
                result = true;
            }
            else
                _isButtonPLCInitilized = true;

            ReturnAndExit:

            return result;
        }

        [BeforeTesting(TEST_STATION.FATP_Button, level: 10)]
        public int BeforeTesting_FATP_Button(ITimeLogger logger)
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

        [TriggerAttribute(TEST_STATION.FATP_Button, level: 10)]
        public int Test_Trigger_FATP_Button(ITimeLogger logger)
        {
            ushort startAddress = (ushort)(Project.ProjectIndex == 0 ? 400 : 405);
            ushort waitStartValue = 3;
            int startButtonState = OnlyPLCReadContinously(1, startAddress, waitStartValue, 0);

            RefreshTopBar();
            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);


            if (_pyCameraFixture != null)
            {
                _pyCameraFixture.Terminate();
                _pyCameraFixture = null;
            }


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

            return 0;
        }


        [ScriptInitialize(TEST_STATION.FATP_Button, level: 10)]
        public int Script_Initialize_FATP_Button(ITestItem item)
        {
            _buttonContext.Reset();
            Project.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Testing...", 14, 14, Colors.Black, Colors.Orange);

            _mainAppVersionLogs = string.Empty;
            _snLogs = string.Empty;
            _handednessLogs = string.Empty;
            _InputFWLogs = string.Empty;
            itemData.Clear();
            mesData.Clear();
            mesDataFFT.Clear();
            rec_data.Clear();
            deadbandXAdc.Clear();
            deadbandYAdc.Clear();
            deadbandXAdcAvg.Clear();
            deadbandYAdcAvg.Clear();
            foreAdc.Clear();
            gripAdc.Clear();
            do_rec_data = false;
            fftResultFile = string.Empty;
            _fftLogs = string.Empty;

            _syncBossLogs = string.Empty;
            _cameraSetting = XmlSettingHelper.LoadSetting<FATP_CameraSetting>(CaesarConfigPath, _Station);

            return 0;
        }



        [AfterScript(TEST_STATION.FATP_Button, level: 10)]
        public int AfterScript_FATP_Button(ITimeLogger logger)
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


        [BeforeSavingLog(TEST_STATION.FATP_Button, level: 10)]
        public int BeforeSavingLog_FATP_Button(ITimeLogger logger)
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

        [LogFilter(TEST_STATION.FATP_Button, level: 10)]
        public int LogFilter_FATP_Button(ILogInformation logInfo, ITimeLogger logger)
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

        [AfterSavingLog(TEST_STATION.FATP_Button, level: 10)]
        public int AfterSavingLog_FATP_Button()
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

        [BeforeShowingResult(TEST_STATION.FATP_Button, level: 10)]
        public int BeforeShowingResult_FATP_Button()
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


        [AfterTesting(TEST_STATION.FATP_Button, level: 10)]
        public int AfterTesting_FATP_Button()
        {
            bool result = false;
            try
            { 
                _buttonContext.ClearUp();
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


        [AfterClosed(TEST_STATION.FATP_Button, level: 10)]
        public int AfterClosed_FATP_Button()
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


        [MainClassDisposeAttribute(TEST_STATION.FATP_Button)]
        public int MainClassDispose_FATP_Button()
        {
            bool result = false;
            try
            {
                _buttonContext.Dispose();
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
