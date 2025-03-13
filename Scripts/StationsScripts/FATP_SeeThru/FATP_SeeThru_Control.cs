using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.StationsScripts.FATP_SeeThru;
using UserHelpers.Helpers;
using Test._App;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetaHelpers.ScriptHelpers;
using Test.ModbusTCP;
using Test._ScriptExtensions;
using System.Diagnostics;
using System.Threading;
using NModbus;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Test.Modules.motion_control;
using MES.DLL.Test.Interface.TestResult;
using System.Runtime.Remoting.Contexts;
using NLog;


namespace Test
{
    public partial class MainClass
    {
        public int StartTest(ITestItem item)
        {
            bool result = false;
            try
            {
                item.AddLog("Start Test");
                Dictionary<string, bool> testStart = new Dictionary<string, bool>() { ["testing"] = true };
                _Context.MotionCotrol.ControlHcPlc(item, testStart);
                var OpenRet = _Context.MotionCotrol.OpenDoor(item, 10);
                if (!OpenRet)
                {
                    item.AddLog("Open Door Fail");
                    goto ReturnAndExit;
                }

                _Context.SeeThruStartTime = DateTime.Now;
                item.AddLog("Start Test Time: " + _Context.SeeThruStartTime.ToString("yyyy-MM-dd HH:mm:ss"));

                _Context.MotionCotrol.ControlHcPlc(item, new Dictionary<string, bool> { ["reset_grating"] = true, });
                item.Sleep(300);
                _Context.MotionCotrol.ControlHcPlc(item, new Dictionary<string, bool> { ["reset_grating"] = false, });
                var resetRet = _Context.MotionCotrol.ResetAlarm("reset_alarm");
                item.AddLog($"Reset Alarm: {resetRet}");

                if (!resetRet.Success)
                {
                    item.AddLog("Reset  Alarm FAIL");
                    goto ReturnAndExit;
                }

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int SetLights(ITestItem item, bool LightStatue, List<string> channel, List<int> lightValue)
        {
            bool result = false;

            try
            {
                item.AddLog(
    $"Channel (JSON): {JsonConvert.SerializeObject(channel ?? new List<string>(), Formatting.Indented)}\n" +
    $"LightValue (JSON): {JsonConvert.SerializeObject(lightValue ?? new List<int>(), Formatting.Indented)}"
);
                //item.AddLog($"channel: [{string.Join(", ", channel)}], Type: {channel.GetType()}");
                //item.AddLog($"lightValue: [{string.Join(", ", lightValue)}], Type: {lightValue.GetType()}");


                //空值检查（包含集合空检查）
                if (channel == null || lightValue == null ||
                    !lightValue.Any() || !channel.Any())
                {
                    item.AddLog($"Light channel is empty! Channels: {string.Join(",", channel)}");
                    goto ReturnAndExit;

                }

                // 长度一致性校验
                if (channel.Count != lightValue.Count)
                {
                    item.AddLog($"Channel/Value count mismatch! Channels:{channel.Count} Values:{lightValue.Count}");
                    goto ReturnAndExit;
                }


                // 构建参数字典（使用LINQ Zip）
                var parameters = channel.Zip(lightValue, (ch, val) => new { ch, val })
                                        .ToDictionary(p => p.ch, p => p.val);

                var parameters2 = parameters.ToDictionary(p => p.Key, p => (object)p.Value);

                item.AddLog($"parameter->{parameters2}");
                // 调用灯光控制
                var (success, errorMessage) = _Context.MotionCotrol.ControlLight(item, parameters2);
                if (!success)
                {
                    item.AddLog($"Light control failed! Channels:{string.Join(",", channel)} Values:{string.Join(",", lightValue)}");
                    goto ReturnAndExit;
                }

                // PLC控制（带状态校验）
                var plcControlParams = new Dictionary<string, bool> {
                    ["light"] = LightStatue
                };

                var ret = _Context.MotionCotrol.ControlHcPlc(item, plcControlParams);
                if (!ret.Success)
                {
                    item.AddLog("PLC control failed after light operation");
                    goto ReturnAndExit;
                }

                result = true;

            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int RobotServoControl(ITestItem item, bool RobotServoStatus)
        {
            bool result = false;

            try
            {
                item.AddLog("Set robot Servo Status");
                // 空值检查（包含集合空检查）
                var ret = _Context.MotionCotrol.CheckRobotAlarm();
                if (!ret)
                {
                    item.AddLog("Robot Alarm");
                    goto ReturnAndExit;
                }

                bool operationResult;
                string message;
                if (RobotServoStatus)
                {
                    (operationResult, message) = _Context.MotionCotrol.ResetFixture();
                }
                else
                {
                    (operationResult, message) = _Context.MotionCotrol.ReleaseFixture();
                }
                if (operationResult)
                {
                    item.AddLog($"set servo {operationResult} {message}");
                    result = true;
                }

            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int CloseAutoDoor(ITestItem item, int timeoutSecond)
        {
            bool result = false;

            try
            {
                item.AddLog("CloseAutoDoor");
                // 空值检查（包含集合空检查）
                var ret = _Context.MotionCotrol.CheckRobotAlarm();
                if (!ret)
                {
                    item.AddLog("Robot Alarm");
                    goto ReturnAndExit;
                }

                bool recvResult;
                string message;
                (recvResult, message) = _Context.MotionCotrol.GetPlcFuncResult(item, "to_station");
                if (recvResult)
                {
                    if (!message.ToLower().Contains("true"))
                    {
                        item.AddLog($"Clamping cylinder is not clamped ========== returns a value of -->{message}============");
                        goto ReturnAndExit;
                    }
                }
                else
                {
                    item.AddLog("GetPlcFuncResult Fail");
                    item.AddLog($"motion_controlCommunication appears to be broken chain ========== return value is-->{recvResult}={message}===========");
                    goto ReturnAndExit;
                }
                item.AddLog($"start close door");
                result = _Context.MotionCotrol.CloseDoor(item, timeoutSecond);
                item.AddLog($"end close door");
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        // open door
        public int OpenAutoDoor(ITestItem item, bool RobotServoStatus)
        {
            bool result = false;

            try
            {
                item.AddLog("OPenAutoDoor");
                // 空值检查（包含集合空检查）
                var ret = _Context.MotionCotrol.CheckRobotAlarm();
                if (!ret)
                {
                    item.AddLog("Robot Alarm");
                    goto ReturnAndExit;
                }

                item.AddLog($"start open door");
                result = _Context.MotionCotrol.OpenDoor(item, 10);
                item.AddLog($"end open door");
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        public int CheckLoadOrHome(ITestItem item, string checkName, int timeout)
        {
            bool result = false;

            try
            {
                item.AddLog($"Check {checkName} position ");
                // 空值检查（包含集合空检查）

                if (checkName.ToLower() == "load")
                {
                    var recvResult = _Context.MotionCotrol.CheckDigitalInput("unload position");
                    if (recvResult.Success && recvResult.Data.TryGetValue("unload position", out bool unloadPositionValue))
                    {
                        if (unloadPositionValue == true)
                        {

                            item.AddLog("Robot is in the unload position.");
                            result = true;
                        }
                        else
                        {
                            item.AddLog("Robot is NOT in the unload position.");
                            goto ReturnAndExit;
                        }
                    }
                    else goto ReturnAndExit;
                }

                else if (checkName.ToLower() == "home")
                {
                    var recvResult = _Context.MotionCotrol.CheckDigitalInput("home position");
                    if (recvResult.Success && recvResult.Data.TryGetValue("home position", out bool unloadPositionValue))
                    {
                        if (unloadPositionValue == true)
                        {

                            item.AddLog("Robot is in the home position.");
                            result = true;
                        }
                        else
                        {
                            item.AddLog("Robot is NOT in the home position.");
                            goto ReturnAndExit;
                        }
                    }
                    else goto ReturnAndExit;
                }
                else
                {
                    item.AddLog($"No such position can be detected. Please check the parameters");
                    result = false;
                }

            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

        /// <summary>
        /// Let the robot run according to the job
        /// </summary>
        /// <param name="item"></param>
        /// <param name="jobName"> </param>
        /// <param name="timeout"></param>
        /// <param name="minimumExerciseTime">
        /// The minimum time for robot movement; exceeding this time is considered a movement failure.
        /// </param>
        /// <returns></returns>
        public int RobotCallJob(ITestItem item, string jobName, int timeout, int minimumExerciseTime)
        {
            bool result = false;

            try
            {
                item.AddLog($"call job  {jobName} movement ");
                // 空值检查（包含集合空检查）
                bool recvResult;
                string message;
                (recvResult, message) = _Context.MotionCotrol.GetPlcFuncResult(item, "to_station");
                if (recvResult)
                {
                    if (!message.ToLower().Contains("true"))
                    {
                        item.AddLog($"Clamping cylinder is not clamped ========== returns a value of -->{message}============");
                        goto ReturnAndExit;
                    }
                }
                else
                {
                    item.AddLog("GetPlcFuncResult Fail");
                    item.AddLog($"motion_controlCommunication appears to be broken chain ========== return value is-->{recvResult}={message}===========");
                    goto ReturnAndExit;
                }

                var ret = _Context.MotionCotrol.CheckRobotAlarm();
                if (!ret)
                {
                    item.AddLog("Robot Alarm");
                    goto ReturnAndExit;
                }

                if (string.IsNullOrEmpty(jobName)) goto ReturnAndExit;

                if (jobName.Contains("LOAD_2_HOME"))
                {
                    var recvResult_home = _Context.MotionCotrol.CheckDigitalInput("home position");
                    if (recvResult_home.Success && recvResult_home.Data.TryGetValue("home position", out bool homePositionValue))
                    {
                        if (homePositionValue == true)
                        {
                            result = true;
                            item.AddLog("Robot is in the home position");
                            goto ReturnAndExit;
                        }
                        else
                        {
                            var recvResult_load = _Context.MotionCotrol.CheckDigitalInput("unload position");
                            if (recvResult_load.Success && recvResult_load.Data.TryGetValue("unload position", out bool loadPositionValue))
                            {
                                if (loadPositionValue == true)
                                {
                                    item.AddLog("When the robot is in the load position, it can move.");

                                }
                                else
                                {
                                    item.AddLog("The robot is not in the load position");
                                    result = false;
                                    goto ReturnAndExit;
                                }
                            }

                        }
                    }
                    else goto ReturnAndExit;
                }

                if (jobName.Contains("HOME_2_LOAD"))
                {
                    var recvResult_home = _Context.MotionCotrol.CheckDigitalInput("unload position");
                    if (recvResult_home.Success && recvResult_home.Data.TryGetValue("unload position", out bool homePositionValue))
                    {
                        if (homePositionValue == true)
                        {
                            result = true;
                            item.AddLog("Robot is in the unload position");
                            goto ReturnAndExit;
                        }
                        else
                        {
                            var recvResulthome = _Context.MotionCotrol.CheckDigitalInput("home position");
                            if (recvResulthome.Success && recvResulthome.Data.TryGetValue("home position", out bool home_PositionValue))
                            {
                                if (home_PositionValue == true)
                                {
                                    item.AddLog("When the robot is in the home position, it can move.");

                                }
                                else
                                {
                                    item.AddLog("The robot is not in the home position");
                                    result = false;
                                    goto ReturnAndExit;
                                }
                            }

                        }
                    }
                    else goto ReturnAndExit;
                }

                DateTime dateTime = DateTime.Now;
                item.AddLog("robot start run");
                var robotMoveResult = _Context.MotionCotrol.CallJob(jobName, timeout);
                item.AddLog("robot run end");
                if ((DateTime.Now - dateTime).TotalSeconds < minimumExerciseTime)
                {
                    item.AddLog("The movement time is less than the set minimum movement duration. Please check if the," +
                        "robotic arm teach pendant is in normal condition");
                }

                if (robotMoveResult.Success)
                {
                    result = true;
                }
                else result = false;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                result = false;
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }


        public int FinishTest(ITestItem item)
        {
            bool result = false;
            try
            {
                item.AddLog("end Test");
                var resetRet = _Context.MotionCotrol.ControlHcPlc(item, new Dictionary<string, bool> { ["testing"] = false });
                if (!resetRet.Success)
                {

                    goto ReturnAndExit;
                }

                result = true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name,
                result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

    }
}
