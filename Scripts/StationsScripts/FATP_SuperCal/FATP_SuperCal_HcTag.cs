using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Test._App;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.HcLabelCommunication;
using UserHelpers.Helpers;

namespace Test
{
    public partial class MainClass
    {

        HCTagCommunicate HcPLCTag = new HCTagCommunicate("192.168.1.88");

        public int ReadHcPlcTag_Bool(ITestItem item, string TagName, bool expectationValue)
        {
            bool result = false;
            try
            {
                var readRes = (bool)HcPLCTag.CommandRead("boolean", TagName);
                if (readRes == expectationValue)
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                item.AddLog($"read bool tag have error:{e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public int HcTagEnableAxis(ITestItem item, int axis, int timeout)
        {
            bool result = false;
            try
            {
                item.AddLog($"carrier plc axis {axis} enable");
                var readRes = (bool)HcPLCTag.CommandRead("boolean", $"Application.GVL.stInfo[{axis}].xStsPwrDone");
                if (!readRes)
                {
                    HcPLCTag.CommandWrite("boolean", $"Application.GVL.stInfo[{axis}].xCmdPwr", true);
                }

                CheckEnableAxis(axis, timeout);
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"set axis have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public void CheckEnableAxis(int axis, int timeout)
        {
            try
            {
                DateTime stTime = DateTime.Now;
                while ((DateTime.Now - stTime).Seconds < timeout)
                {
                    Thread.Sleep(100);
                    bool readRes = (bool)HcPLCTag.CommandRead("boolean", $"Application.GVL.stInfo[{axis}].xStsPwrDone");
                    if (readRes)
                    {
                        return;
                    }
                }

                throw new Exception($"plc axis {axis} check enable error!--timeout");

            }
            catch (Exception e)
            {
                throw new Exception($"plc axis {axis} check enable error!---{e}");
            }
        }

        /// <summary>
        /// 运动轴回home点位并检测位置
        /// </summary>
        /// <param name="item"></param>
        /// <param name="axis">运动的轴</param>
        /// <returns></returns>
        public int HcTagAxisRunHome(ITestItem item, int axis, int timeout)
        {
            bool result = false;
            try
            {
                item.AddLog($"carrier plc axis {axis} home");
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.stInfo[{axis}].xCmdHome", false);
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xStepMotorStop", false);
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xStepMotorStart", false);
                Thread.Sleep(100);
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.stInfo[{axis}].xCmdHome", true);
                item.Sleep(2000);
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.stInfo[{axis}].xCmdHome", false);


                CheckHomeAxis(axis, timeout);
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"set axis have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;

        }


        public void CheckHomeAxis(int axis, int timeout)
        {
            try
            {
                DateTime stTime = DateTime.Now;
                while ((DateTime.Now - stTime).Seconds < timeout)
                {
                    Thread.Sleep(100);
                    // 
                    bool readRes = (bool)HcPLCTag.CommandRead("boolean", $"Application.GVL.stInfo[{axis}].xStsHomeDone");

                    var current_pos = (double)HcPLCTag.CommandRead("double", $"Application.GVL.stInfo[{axis}].fStsAxisPos");
                    
                    if (readRes && (current_pos > -1) && (current_pos < 1))
                    {
                        return;
                    }
                }

                throw new Exception($"plc axis {axis} check enable error!--timeout");

            }
            catch (Exception e)
            {
                throw new Exception($"plc axis {axis} check enable error!---{e}");
            }

        }

        /// <summary>
        /// 让所有轴开始运动
        /// </summary>
        /// <param name="item"></param>
        /// <param name="status">true/false</param>
        /// <returns></returns>
        public int HcTagStartRun(ITestItem item, bool status)
        {
            bool result = false;
            try
            {
                item.AddLog($"carrier plc RUN");
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xStepMotorStart", status);
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"set axis have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;
        }

        /// <summary>
        /// 让所有轴停止运行
        /// </summary>
        /// <param name="item"></param>
        /// <param name="status"></param>
        /// <returns></returns>

        public int HcTagStopRun(ITestItem item, bool status)
        {
            bool result = false;
            try
            {
                item.AddLog($"carrier plc stop run");
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xStepMotorStop", status);
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"set axis have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int HCAxisRunAnyTime(ITestItem item, int runTime)
        {
            bool result = false;
            try
            {
                item.AddLog($"carrier plc  run");
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xStepMotorStart", true);
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xStepMotorStop", false);
                item.Sleep(runTime*1000);

                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"run plc have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public int PcControlHc(ITestItem item)
        {
            bool result = false;
            try
            {
                item.AddLog($"PC control  axis");
                HcPLCTag.CommandWrite("boolean", $"Application.GVL.xHmiCmdReqPC", false);
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"PC control  axis have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;

        }

        /// <summary>
        /// 设置轴的位置值，2个位置 位置值1和位置值2
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Position1">360-1圈，180-半圈</param>
        /// <param name="Position2">0-原点，</param>
        /// <returns></returns>
        public int HcSetPositionValue(ITestItem item, double Position1, double Position2 = 0)

        {

            bool result = false;
            try
            {
                item.AddLog($"set Axis position 1axis:{Position1},2axis:{Position2}");
                HcPLCTag.CommandWrite("double", $"Application.GVL.rfSetDist[1]", Position1);
                HcPLCTag.CommandWrite("double", $"Application.GVL.rfSetDist[2]", Position2);

                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"set Axis position  have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;
        }

        /// <summary>
        /// 设置2个位置的速度值
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Position1">360-转1圈1S，180-转1圈2S</param>
        /// <param name="Position2">360-转1圈1S，180-转1圈2S</param>
        /// <returns></returns>

        public int HcSetSpeedValue(ITestItem item, double speed1, double speed2 = 0)

        {

            bool result = false;
            try
            {
                item.AddLog($"set Axis Speed 1axis:{speed1},2axis:{speed2}");
                HcPLCTag.CommandWrite("double", $"Application.GVL.rfSetVel[1]", speed1);
                HcPLCTag.CommandWrite("double", $"Application.GVL.rfSetVel[2]", speed2);

                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"set Axis position  have error: {e}");

            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="axis">哪个轴</param>
        /// <param name="expectationState">期望状态</param>
        /// <returns></returns>
        public int ReadPLCBusyStatus(ITestItem item, int axis, bool expectationState,int timeout)
        {
            var result = false;
            try
            {
                bool busyState = false;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                while (stopwatch.Elapsed.TotalSeconds < timeout)
                {
                    item.Sleep(10);
                    busyState = (bool)HcPLCTag.CommandRead("boolean", $"Application.GVL.stInfo[{axis}].xStsAbsBusy");
                    if (busyState == expectationState)
                    {
                        result = true;
                        goto ReturnAndExit;
                    }

                }
                item.AddLog($"read busy axis{axis} timeout");
            }
            catch (Exception e)
            {
                item.AddLog($"read busy axis{axis} error {e}");

            }

            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? "PASS" : "FAIL");
            AddResult(item, data);
            return result ? 0 : 1;
        }
    }



}
