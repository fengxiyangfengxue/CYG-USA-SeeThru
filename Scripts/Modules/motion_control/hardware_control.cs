using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NLog;
using UserHelpers.Helpers;




namespace Test.Modules.motion_control
{
    public class HardwareControl: IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly RobotMotionServer _controller = new RobotMotionServer("127.127.127.1", 8088);
        private bool _disposed;

        #region 机器人部分
        public (bool Success, string Data) ResetFixture()
        {
            const string command = "cmd_reset_fixture()";
            return _controller.SendCommand(command);
        }

        public (bool Success, string Data) ReleaseFixture()
        {
            const string command = "cmd_release_fixture()";
            return _controller.SendCommand(command);
        }

        public (bool Success, string Data) MoveJointRel(double[] data)
        {
            var joints = new[] { "Jog_01", "Jog_02", "Jog_03", "Jog_04", "Jog_05", "Jog_06" };
            var command = $"cmd_move_joint_increment({BuildDictionary(joints, data)})";
            Logger.Info($"Moving joints relative: {command}");
            return _controller.SendCommand(command);
        }

        public (bool Success, string Data) MovePosRel(double[] data)
        {
            var axes = new[] { "x", "y", "z", "rx", "ry", "rz" };
            var command = $"cmd_move_position_increment({BuildDictionary(axes, data)})";
            Logger.Info($"Moving position relative: {command}");
            return _controller.SendCommand(command);
        }

        public (bool Success, string Data) MovePosAbs(double[] data)
        {
            var axes = new[] { "x", "y", "z", "rx", "ry", "rz" };
            var command = $"cmd_move_position_absolute({BuildDictionary(axes, data)})";
            var ret = _controller.SendCommand(command);
            return ret.Success ? CheckPosition(data, 5.0, 10) : ret;
        }

        public (bool Success, string Data) MoveJointAbs(double[] data)
        {
            var joints = new[] { "Jog_01", "Jog_02", "Jog_03", "Jog_04", "Jog_05", "Jog_06" };
            var command = $"cmd_move_joint_absolute({BuildDictionary(joints, data)})";
            Logger.Info($"Moving joints absolute: {command}");
            var ret = _controller.SendCommand(command);
            return ret.Success ? CheckJoint(data, 5.0, 10) : ret;
        }

        public (bool Success, string Data) CallJob(string jobName, int timeout = 5)
        {
            var command = $"run_job('{jobName}')";
            var ret = _controller.SendCommand(command);
            Thread.Sleep(2000);

            if (ret.Success)
            {
                var startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalSeconds < timeout)
                {
                    var diResult = CheckDigitalInput("a job is running");
                    if (!diResult.Success) return (false,"recv error");

                    if (!diResult.Data["a job is running"])
                    {
                        Logger.Info($"Job {jobName} completed");
                        return (true, "Job completed");
                    }
                    Thread.Sleep(500);
                }
                return (false, $"Job {jobName} timed out");
            }
            return ret;
        }

        private (bool Success, string Data) CheckJoint(double[] target, double range, int timeout)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeout)
            {
                var ret = _controller.SendCommand("cmd_check_joint()");
                if (!ret.Success) return ret;

                var current = ParseJointResponse(ret.Data);
                if (CheckPositionDifference(current, target, range))
                {
                    return (true, "Move complete");
                }
                Thread.Sleep(300);
            }
            return (false, "Joint check timeout");
        }

        private (bool Success, string Data) CheckPosition(double[] target, double range, int timeout)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeout)
            {
                var ret = _controller.SendCommand("cmd_check_position()");
                if (!ret.Success) return ret;

                var current = ParsePositionResponse(ret.Data);
                if (CheckPositionDifference(current, target, range))
                {
                    return (true, "Position reached");
                }
                Thread.Sleep(300);
            }
            return (false, "Position check timeout");
        }

        public (bool Success, Dictionary<string, bool> Data) CheckDigitalInput(string name)
        {
            var command = $"cmd_check_input('{name}')";
            var ret = _controller.SendCommand(command);
            if (!ret.Success) return (false, null);

            try
            {
                var parts = ret.Data.Split(new[] { "check_input:" },StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    Logger.Error("Invalid response format");
                    return (false, null);
                }

                var data = parts.Last().Trim();
                return (true, ParseDictionary(data));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to parse DI response");
                return (false, null);
            }
        }
        public bool CheckRobotAlarm()
        {
            if (!RobotConnectState()) return false;

            var checkItems = new[] { "e_stop_satisfied", "gate" };
            foreach (var item in checkItems)
            {
                var ret = CheckDigitalInput(item);
                if (!ret.Success || ret.Data.Any(kv => !kv.Value))
                {
                    Logger.Error($"Robot alarm state: {item}");
                    return false;
                }
            }
            return true;
        }

        public (bool Success, string Data) CheckState(string stateName)
        {
            var command = $"cmd_check_state('{stateName}')";
            return ExecuteWithLog(command, $"check state: {stateName}");
        }

        private bool RobotConnectState()
        {
            var ret = _controller.SendCommand("cmd_robot_connect()");
            if (!ret.Success) return false;

            var parts = ret.Data.Split(new[] { "robot_connect:" }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 && bool.TryParse(parts[1], out var state) && state;
        }

        public (bool Success, string Data) ResetAlarm(string alarmType)
        {
            if (string.IsNullOrEmpty(alarmType))
                return (false, "cannot be empty");

            const string command = "cmd_reset_alarm";
            return ExecuteWithLog(command, $"reset alarm: {alarmType}");
        }
        #endregion

        #region PLC部分
        public (bool Success, string Data) ControlHcPlc(ITestItem testItem, Dictionary<string, bool> data)
        {
            if (data == null)
            {
                Logger.Error("The PLC control parameter cannot be empty");
                testItem.AddLog($"The PLC control parameter cannot be empty");
                return (false, "Invalid parameters");
            }

            try
            {
                var command = $"cmd_hc_plc_control({SerializeBoolDict(data)})";
                Logger.Info($"Send PLC control instructions:: {command}");
                testItem.AddLog($"Send PLC control instructions:{command}");
                return _controller.SendCommand(command);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PLC control instruction construction failed");
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// 获取PLC功能执行结果
        /// </summary>
        public (bool Success, string Data) GetPlcFuncResult(ITestItem testItem, string command)
        {
            var validCommands = new HashSet<string>
            {
            "door_is_closed",
            "door_is_opened",
            "to_home_position",
            "to_station"
        };

            if (!validCommands.Contains(command))
            {
                testItem.AddLog($"Invalid PLC function command: {command}");
                Logger.Warn($"Invalid PLC function command: {command}");
                return (false, $"{command} Not a valid PLC control command");
            }

            var fullCommand = $"cmd_{command}";
            return _controller.SendCommand(fullCommand);
        }

        /// <summary>
        /// 读取温度
        /// </summary>
        public (bool Success, string Data) ReadTemperature()
        {
            const string command = "cmd_read_temperature()";
            return _controller.SendCommand(command);
        }

        public void CheckDoorCylinderState()
        {
            while (true)
            {
                // 检查气缸报警
                var cylRet = GetPlcAlarmInformation("cylinder_alarm");
                if (cylRet.Success)
                {
                    var value = ParseBoolResponse(cylRet.Data);
                    
                    if (value) break;

                }
                else
                {
                   
                    break;
                }

                Thread.Sleep(3000);

                // 检查门报警
                var doorRet = GetPlcAlarmInformation("door_alarm");
                if (doorRet.Success)
                {
                    var value = ParseBoolResponse(doorRet.Data);
                    
                    if (value) break;
                }
                else
                {
                    
                    break;
                }

                Thread.Sleep(3000);
            }
        }

        /// <summary>
        /// 开门操作（带超时）
        /// </summary>
        public bool OpenDoor(ITestItem testItem, int timeoutSeconds)
        {
            var controlRet = ControlHcPlc(testItem,new Dictionary<string, bool> {
                ["door_open"] = true,
                ["door_close"] = false
            });

            if (!controlRet.Success) return false;

            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                var statusRet = GetPlcFuncResult(testItem,"door_is_opened");
                if (statusRet.Success)
                {
                    if (ParseBoolResponse(statusRet.Data))
                    {
                        ControlHcPlc(testItem,new Dictionary<string, bool> {
                            ["door_open"] = false,
                            ["door_close"] = false
                        });
                        return true;
                    }
                }
                else
                {
                    StopDoorMovement(testItem);
                    return false;
                }
                Thread.Sleep(500);
            }

            StopDoorMovement(testItem);
            return false;
        }

        /// <summary>
        /// 关门操作（带超时）
        /// </summary>
        public bool CloseDoor(ITestItem testItem,int timeoutSeconds)
        {
            var controlRet = ControlHcPlc(testItem, new Dictionary<string, bool> {
                ["door_open"] = false,
                ["door_close"] = true
            });

            if (!controlRet.Success) return false;

            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                var statusRet = GetPlcFuncResult(testItem, "door_is_closed");
                if (statusRet.Success)
                {
                    if (ParseBoolResponse(statusRet.Data))
                    {
                        StopDoorMovement(testItem);
                        return true;
                    }
                }
                else
                {
                    StopDoorMovement(testItem);
                    return false;
                }
                Thread.Sleep(500);
            }

            StopDoorMovement(testItem);
            return false;
        }


        /// <summary>
        /// 获取PLC报警信息（内部方法）
        /// </summary>
        private (bool Success, string Data) GetPlcAlarmInformation(string alarmType)
        {
            var validTypes = new HashSet<string> { "cylinder_alarm", "door_alarm" };
            if (!validTypes.Contains(alarmType))
            {
                Logger.Error($"Invalid alarm type: {alarmType}");
                return (false, "Invalid alarm type");
            }

            var command = $"cmd_{alarmType}";
            return _controller.SendCommand(command);
        }
        #endregion

        #region 灯控制
        /// <summary>
        /// 控制标定板灯光
        /// </summary>
        /// <param name="para">字典类型{"channel1":100,"channel2":200,"channel3":300}</param>
        /// <returns> (bool,data)</returns>
        public (bool Success, string Data) ControlLight(ITestItem item, Dictionary<string,object> para)
        {
            var command = $"cmd_control_light({para})";
            Logger.Info($"light_control({para})");
            item.AddLog($"light_control({para})");

            return _controller.SendCommand(command);
        }
        #endregion
        public void Dispose()
        {
            if (!_disposed)
            {
                _controller.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }


        // PLC辅助方法
        
        private void StopDoorMovement(ITestItem testItem)
        {
            ControlHcPlc(testItem, new Dictionary<string, bool> {
                ["door_open"] = false,
                ["door_close"] = false
            });
        }

        private static string SerializeBoolDict(Dictionary<string, bool> data)
        {
            var items = new List<string>();
            foreach (var kvp in data)
            {
                items.Add($"'{kvp.Key}': {kvp.Value.ToString().ToLower()}");
            }
            return $"{{{string.Join(", ", items)}}}";
        }

        private bool ParseBoolResponse(string response)
        {
            try
            {
                var parts = response.Split(':');
                if (parts.Length < 2) return false;

                string value = parts[1].Trim().ToLower();
                if (value == "true" || value == "1")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // ROBOT辅助方法
        private (bool Success, string Data) ExecuteWithLog(string command, string operationName)
        {
            Logger.Info($"[{operationName}] send instructions: {command}");
            var ret = _controller.SendCommand(command);
            Logger.Info($"[{operationName}] receiving response: {ret.Data}");
            return ret;
        }

        private static string BuildDictionary(string[] keys, double[] values)
        {
            // 确保键值数量匹配
            if (keys.Length != values.Length)
                throw new ArgumentException("Keys and values length mismatch");

            // 生成Python风格的字典项
            var items = keys.Zip(values, (k, v) =>
                $"'{k}': {v.ToString("0.######", CultureInfo.InvariantCulture)}"); // 保留6位小数

            // 正确转义花括号
            return $"{{{string.Join(", ", items)}}}";
        }

        private static bool CheckPositionDifference(IReadOnlyList<double> current, IReadOnlyList<double> target, double range)
        {
            return current.Zip(target, (c, t) => Math.Abs(c - t))
                          .All(diff => diff < range);
        }

        private static double[] ParseJointResponse(string response)
        {
            var data = response.Split(new[] { "check_joint:" }, StringSplitOptions.RemoveEmptyEntries)
                               .Last()
                               .Trim();
            return ParseValues(data);
        }

        private static double[] ParsePositionResponse(string response)
        {
            var data = response.Split(new[] { "check_position:" }, StringSplitOptions.RemoveEmptyEntries)
                               .Last()
                               .Trim();
            return ParseValues(data);
        }

        private static double[] ParseValues(string input)
        {
            return input.Trim('{', '}')
                        .Split(',')
                        .Select(p => p.Split(':').Last().Trim())
                        .Select(double.Parse)
                        .ToArray();
        }

        private static Dictionary<string, bool> ParseDictionary(string input)
        {
            return input.Trim('{', '}')
                        .Split(',')
                        .Select(p => p.Split(':'))
                        .ToDictionary(
                            pair => pair[0].Trim().Trim('\''),
                            pair => bool.Parse(pair[1].Trim())
                        );
        }
    }

}


//// 使用示例
//class Program
//{
//    static void Main()
//    {
//        using var robot = new GP8Robot();

//        // 示例调用
//        var moveResult = robot.MoveJointAbs(new[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 });
//        if (moveResult.Success)
//        {
//            Console.WriteLine("Move successful");
//        }

//        var jobResult = robot.CallJob("YYLOAD_TO_HOME");
//        Console.WriteLine($"Job result: {jobResult.Data}");
//    }
//}