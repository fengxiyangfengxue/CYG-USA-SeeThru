using System;
using System.IO.Ports;
using System.Text;
//using NLog;
using System.Management;
//using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using MES.DLL.Test.Interface.TestResult;
using UserHelpers.Helpers;


namespace Test.Modules.SerialMotion { 
public class MovingData
{
    public double Velocity1 { get; set; }
    public double Position1 { get; set; }
    public double Velocity2 { get; set; }
    public double Position2 { get; set; }
    public double Delay { get; set; }
}


public class MotionPath
{

    public static string sequenceImu = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence_imu.json";
    public static string sequenceLED = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence_led.json";
    public static string sequenceCal = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence_cal.json";

    private readonly Dictionary<string, string> filePathDict = new Dictionary<string, string>
    {
        {"IMU",sequenceImu},
        {"LED",sequenceLED},
        {"STAGE_CAL",sequenceCal}
    };

    private readonly Dictionary<string, List<MovingData>> data = new Dictionary<string, List<MovingData>>
    {
        { "IMU", new List<MovingData> { new MovingData() } },
        { "STAGE_CAL", new List<MovingData> { new MovingData() } },
        { "LED_01", new List<MovingData> { new MovingData() } },
        { "LED_02", new List<MovingData> { new MovingData() } },
        { "LED_03", new List<MovingData> { new MovingData() } },
        { "LED_04", new List<MovingData> { new MovingData() } },
        { "LED_05", new List<MovingData> { new MovingData() } },
        { "LED_06", new List<MovingData> { new MovingData() } },
        { "LED_07", new List<MovingData> { new MovingData() } },
        { "LED_08", new List<MovingData> { new MovingData() } }
    };

    public MotionPath()
    {
        LoadData();
    }

    public List<MovingData> GetData(string name)
    {
        if (!data.ContainsKey(name))
        {
            throw new PLCExcept($"Motion data name '{name}' can't be found!");
        }

        return data[name];
    }

    private void LoadData()
    {
        LoadDataIMU();
        LoadDataLED();
        LoadDataCal();
    }

    private void LoadDataIMU()
    {
        var imuData = ReadFromFile("IMU");
        data["IMU"] = imuData;
    }

    private void LoadDataLED()
    {
        var ledKeys = data.Keys.Where(k => k.Contains("LED_0")).ToList();
        foreach (var key in ledKeys)
        {
            data[key] = UpdateMovingByKey(key);
        }
    }

    private void LoadDataCal()
    {
        var calData = ReadFromFile("STAGE_CAL");
        data["STAGE_CAL"] = calData;
    }

    private List<MovingData> ReadFromFile(string name)
    {
        List<MovingData> movingList = null;
        var filePath = filePathDict[name];
        string json = File.ReadAllText(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, List<MovingData>>>(json);

        if (data != null && data.ContainsKey("moving"))
        {
            movingList = data["moving"];
        }

        // If movingList is null, initialize it to an empty list
        if (movingList == null)
        {
            movingList = new List<MovingData>();
        }

        return movingList;
    }

    private List<MovingData> UpdateMovingByKey(string indexKey)
    {
        // Load data for the "LED" key from the corresponding JSON file.  
        var movingLed = ReadFromFile("LED");

        // Retrieve the LED index from the indexKey.     
        int ledIndex = int.Parse(indexKey.Substring(indexKey.Length - 1)) - 1;

        // Update the Position2 of each MovingData based on the calculation.  
        foreach (var item in movingLed)
        {
            // Update Position2 using the Velocity1 property if it exists.  
            item.Position2 += 45 * ledIndex; // Adjusting Position2 based on LED index.  
        }

        // Wrap around positions if they exceed 360 degrees.  
        foreach (var item in movingLed)
        {
            if (item.Position2 >= 360)
            {
                item.Position2 -= 360; // Wrap around the value to stay in the 0-359 range.  
            }
        }

        return movingLed;
    }
}

public class PLCExcept : Exception
{
    public PLCExcept(string message) : base(message)
    {
    }
}

public class SerialPortInfo
{
    public List<SerialPort> PortList { get; private set; }
    public Dictionary<string, string> SnPortDict { get; private set; }

    public SerialPortInfo()
    {
        PortList = new List<SerialPort>();
        SnPortDict = new Dictionary<string, string>();
    }

    // Get the serial number to port mapping
    public Dictionary<string, string> GetSnPortDict()
    {
        SnPortDict.Clear();
        var availablePorts = SerialPort.GetPortNames();

        if (availablePorts.Length > 0)
        {
            foreach (string portName in availablePorts)
            {
                string portSn = GetSerialNumber(portName); // Fetch serial number for the port
                if (!string.IsNullOrEmpty(portSn) && !string.IsNullOrEmpty(portName))
                {
                    SnPortDict.Add(portSn, portName);
                }
            }
        }

        return SnPortDict;
    }

    // Get serial number using Windows Management Instrumentation (WMI)
    public string GetSerialNumber(string portName)
    {
        string serialNumber = null;
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_SerialPort WHERE DeviceID = '{portName}'");
            foreach (ManagementObject obj in searcher.Get())
            {
                // list all properties
                foreach (var property in obj.Properties)
                {
                    // show properties and value  
                    Console.WriteLine($"{property.Name}: {property.Value}");
                }

                // retrieve serialNumber
                serialNumber = obj["PNPDeviceID"]?.ToString();
            }
        }
        catch (Exception)
        {
            // Handle exception if necessary
        }
        return serialNumber;
    }

    // Get COM port list based on a timeout
    public List<string> GetComPortList(Dictionary<string, string> uartSnNestDict, int getTimeOut)
    {
        var comPortList = new List<string>(new string[8]);

        int uartNo = uartSnNestDict.Count;

        Console.WriteLine($"uart_sn_nest_dict: {string.Join(", ", uartSnNestDict.Select(kv => kv.Key + "=" + kv.Value))}");

        var uartNestSnDict = uartSnNestDict.ToDictionary(kv => kv.Value, kv => kv.Key);
        var serialPortDetectStart = DateTime.Now;

        while (true)
        {
            Thread.Sleep(10); // Sleep for 10 ms
            var snPortDictInNest = GetSnPortDict();

            Console.WriteLine($"sn_port_dict_in_nest: {string.Join(", ", snPortDictInNest.Select(kv => kv.Key + "=" + kv.Value))}");

            for (int i = 0; i < comPortList.Count; i++)
            {
                //comPortList[i] = snPortDictInNest.GetValueOrDefault(uartNestSnDict.GetValueOrDefault((i + 1).ToString()));
                //comPortList[i] = snPortDictInNest.uartNestSnDict.TryGetValue((i + 1).ToString());
            }

            var serialPortDetectEnd = DateTime.Now;
            var serialPortDetectTimeOut = (serialPortDetectEnd - serialPortDetectStart).TotalSeconds > getTimeOut;

            comPortList = comPortList.Select(comPort => comPort?.ToLower()).ToList();

            if (comPortList.Count(c => c != null) == uartNo || serialPortDetectTimeOut)
            {
                break;
            }
        }

        Console.WriteLine($"com_port_list: {string.Join(", ", comPortList)}");

        return comPortList;
    }
}


public class SerialPortHandler : IDisposable
    {
    //protected Logger log = LogManager.GetCurrentClassLogger();
    private SerialPort _serialPort;

    // Constructor to initialize the SerialPort with given parameters
    public SerialPortHandler(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
    {
        _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        //_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

        // Open serial port
        __Open();
    }

    public void Dispose()
    {
        Dispose(false);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            //释放托管资源 ：.NET运行时管理的资源，比如普通的C#对象。 *这些资源会由垃圾回收器（GC）自动清理
        }
        else
        {
            // 释放非托管资源： 如文件句柄，PLC句柄、数据库链接，（操作系统层面的资源）  *GC不会自动清理这些资源，需要手动清理
            if (_serialPort != null )
            {

                _serialPort.Close();
                _serialPort = null;

            }
        }
    }

        // Open the serial port connection
        private void __Open()
    {
        try
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                Console.WriteLine("Serial port opened.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening serial port: {ex.Message}");
        }
    }

    // Close the serial port connection
    public void Close()
    {
        try
        {
            if (IsOpen)
            {
                _serialPort.Close();
                //_serialPort = null ;
                UIMessageBox.Show(this, "Serial port closed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing serial port: {ex.Message}");
        }
    }

    // Write data to the serial port
    public void WriteData(string data)
    {
        if (IsOpen)
        {
            _serialPort.Write(data);
            Console.WriteLine($"Data written to serial port: {data}");
        }
        else
        {
            Console.WriteLine("Serial port is not open.");
        }
    }

    public string ReadLine()
    {
        if (IsOpen)
        {
            return _serialPort.ReadLine();
        }
        else
        {
            Console.WriteLine($"Serial port not opened");
            return string.Empty; // Return an empty string if the serial port is not open
        }
    }

    public byte[] ReadSize(int size)
    {
        if (IsOpen)
        {
            byte[] buffer = new byte[size];
            int bytesRead = _serialPort.Read(buffer, 0, size);
            return buffer;
        }
        else
        {
            Console.WriteLine($"Serial port not opened");
            return new byte[0]; // Return an empty byte array if the serial port is not open
        }
    }

    // Read data until a specific delimiter using ReadExisting()
    public string ReadUntilDelimiter(string delimiter, int timeout = 5000)
    {
        StringBuilder response = new StringBuilder();
        DateTime startTime = DateTime.Now;

        while (_serialPort.IsOpen && (DateTime.Now - startTime).TotalMilliseconds < timeout)
        {
            if (_serialPort.BytesToRead > 0)
            {
                // Read all available data at once
                string data = _serialPort.ReadExisting();
                response.Append(data);

                // Stop reading if the delimiter is found
                if (response.ToString().Contains(delimiter))
                {
                    break;
                }
            }
        }

        return response.ToString();
    }

    // Read data until expected size using ReadExisting()
    public string ReadUntilSize(int expectedSize, int timeout = 5000)
    {
        StringBuilder response = new StringBuilder();
        DateTime startTime = DateTime.Now;

        while (_serialPort.IsOpen && (DateTime.Now - startTime).TotalMilliseconds < timeout)
        {
            if (_serialPort.BytesToRead > 0)
            {
                string data = _serialPort.ReadExisting();
                response.Append(data);

                if (response.Length >= expectedSize)
                {
                    break;
                }
            }
        }

        return response.ToString();
    }

    // Event handler for when data is received
    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        string incomingData = _serialPort.ReadExisting();
        Console.WriteLine($"Data received: {incomingData}");
    }

    // Check if the serial port is open
    public bool IsOpen
    {
        get { return _serialPort.IsOpen; }
    }
}


public class PlcCom : SerialPortHandler
{
    //private readonly byte[] delimiter = Encoding.ASCII.GetBytes("@_@\r\n");
    //public static byte[] delimiterByte = Encoding.UTF8.GetBytes("@_@\r\n");
    private readonly string delimiter = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("@_@\r\n")); // Decodes byte array to string

    public PlcCom(string com, int bps, Parity parity = Parity.None) : base(com, bps, parity) { }


    // Get the station ID
    public string GetStationName()
    {
        if (IsOpen)
        {
            WriteData("cmd_fixture_info()");
            string response = ReadUntilDelimiter(delimiter);
            Console.WriteLine(response);
            return response.Split(':')[1];
        }
        return string.Empty;
    }

    // Get all available commands and notes
    public string GetAllHelp()
    {
        if (IsOpen)
        {
            WriteData("cmd_help()");
            return ReadUntilDelimiter(delimiter);
        }
        return string.Empty;
    }

    // Reset the fixture to its initial state
    public bool ResetFixture()
    {
        if (IsOpen)
        {
            WriteData("cmd_reset_fixture()");
            string response = ReadUntilDelimiter(delimiter);
            return response.Split(':')[1].ToLower().Contains("true");
        }
        return false;
    }

    // Return fixture ID
    public string GetFixtureId()
    {
        if (IsOpen)
        {
            WriteData("cmd_reset_fixture()");
            string response = ReadUntilDelimiter(delimiter);
            return response.Split(':')[1];
        }
        return string.Empty;
    }

    public bool SendMovePositionIncrement(int number, double value = 0, double value2 = 0)
    {
        //string command = number switch
        //{
        //    0 => $"cmd_move_position_increment({{\"axis01\": {value}, \"axis02\": {value2}}})",
        //    1 => $"cmd_move_position_increment({{\"axis01\": {value}}})",
        //    2 => $"cmd_move_position_increment({{\"axis02\": {value}}})",
        //    _ => ""
        //};
        string command = string.Empty;
        if (number == 0)
        {
            command = $"cmd_move_position_increment({{\"axis01\": {value}, \"axis02\": {value2}}})";
        }
        else if (number == 1)
        {
            command = $"cmd_move_position_increment({{\"axis01\": {value}}})";
        }
        else if (number == 2)
        {
            command = $"cmd_move_position_increment({{\"axis02\": {value}}})";
        }
        else
        {
            command = "";
        }


        if (string.IsNullOrEmpty(command)) return false;

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        return !(resResult.Contains("false") || resResult.Contains("error"));
    }

    public bool SendMovePositionAbsolute(int number, double value = 0, double value2 = 0)
    {
        //string command = number switch
        //{
        //    0 => $"cmd_move_position_absolute({{\"axis01\": {value}, \"axis02\": {value2}}})",
        //    1 => $"cmd_move_position_absolute({{\"axis01\": {value}}})",
        //    2 => $"cmd_move_position_absolute({{\"axis02\": {value}}})",
        //    _ => ""
        //};



        string command = string.Empty;
        if (number == 0)
        {
            command = $"cmd_move_position_absolute({{\"axis01\": {value}, \"axis02\": {value2}}})";
        }
        else if (number == 1)
        {
            command = $"cmd_move_position_absolute({{\"axis01\": {value}}})";
        }
        else if (number == 2)
        {
            command = $"cmd_move_position_absolute({{\"axis02\": {value}}})";
        }
        else
        {
            command = "";
        }

        if (string.IsNullOrEmpty(command)) return false;

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        return !(resResult.Contains("false") || resResult.Contains("error"));
    }

    public bool SendMoveVelocity(int number, double value = 0, double value2 = 0)
    {
        //string command = number switch
        //{
        //    0 => $"cmd_move_velocity({{\"axis01\": {value}, \"axis02\": {value2}}})",
        //    1 => $"cmd_move_velocity({{\"axis01\": {value}}})",
        //    2 => $"cmd_move_velocity({{\"axis02\": {value}}})",
        //    _ => ""
        //};

        string command = string.Empty;
        if (number == 0)
        {
            command = $"cmd_move_velocity({{\"axis01\": {value}, \"axis02\": {value2}}})";
        }
        else if (number == 1)
        {
            command = $"cmd_move_velocity({{\"axis01\": {value}}})";
        }
        else if (number == 2)
        {
            command = $"cmd_move_velocity({{\"axis02\": {value}}})";
        }
        else
        {
            command = "";
        }


        if (string.IsNullOrEmpty(command)) return false;

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        return !(resResult.Contains("false") || resResult.Contains("error"));
    }

    public bool MovePositionSetVelocity(int number, double value = 0.00, double value2 = 0.00)
    {
        //string command = number switch
        //{
        //    0 => $"cmd_move_position_set_velocity({{\"axis01\": {value}, \"axis02\": {value2}}})",
        //    1 => $"cmd_move_position_set_velocity({{\"axis01\": {value}}})",
        //    2 => $"cmd_move_position_set_velocity({{\"axis02\": {value}}})",
        //    _ => ""
        //};

        string command = string.Empty;
        if (number == 0)
        {
            command = $"cmd_move_position_set_velocity({{\"axis01\": {value}, \"axis02\": {value2}}})";
        }
        else if (number == 1)
        {
            command = $"cmd_move_position_set_velocity({{\"axis01\": {value}}})";
        }
        else if (number == 2)
        {
            command = $"cmd_move_position_set_velocity({{\"axis02\": {value}}})";
        }
        else
        {
            command = "";
        }


        if (string.IsNullOrEmpty(command)) return false;

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        return !(resResult.Contains("false") || resResult.Contains("error"));
    }

    public bool SetWait(double value)
    {
        WriteData($"cmd_wait({value})");
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine($"{value}: {resResult}");

        return resResult.Contains("true");
    }

    public bool SetCmdHome(int number = 0)
    {
        //string? command = number switch
        //{
        //    0 => "cmd_home()",
        //    1 => "cmd_home(\"axis01\")",
        //    2 => "cmd_home(\"axis02\")",
        //    _ => null
        //};

        string command = string.Empty;
        if (number == 0)
        {
            command = "cmd_home()";
        }
        else if (number == 1)
        {
            command = "cmd_home(\"axis01\")";
        }
        else if (number == 2)
        {
            command = "cmd_home(\"axis02\")";
        }
        else
        {
            command = string.Empty;
        }



        if (string.IsNullOrEmpty(command)) return false;

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        // Return based on specific responses
        return resResult.Contains(delimiter);
    }

    public bool SetCmdAbort(int number)
    {
        string command = "cmd_abort()";

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        // Return based on specific responses
        return resResult.Contains(delimiter);
    }

    public bool SetCmdStop(int number)
    {
        //string? command = number switch
        //{
        //    0 => "cmd_stop()",
        //    1 => "cmd_stop(\"axis01\")",
        //    2 => "cmd_stop(\"axis02\")",
        //    _ => null
        //};

        string command = string.Empty;
        if (number == 0)
        {
            command = "cmd_stop()";
        }
        else if (number == 1)
        {
            command = "cmd_stop(\"axis01\")";
        }
        else if (number == 2)
        {
            command = "cmd_stop(\"axis02\")";
        }
        else
        {
            command = string.Empty;
        }


        if (string.IsNullOrEmpty(command)) return false;

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        return resResult.Contains("true");
    }

    public List<double> GetCheckPosition(int number)
    {
        //// Determine the axis to query based on the number
        //string? axis = number switch
        //{
        //    0 => null, // Both axes
        //    1 => "axis01", // Axis 1
        //    2 => "axis02", // Axis 2
        //    _ => throw new ArgumentException("Invalid axis number")
        //};


        string axis = string.Empty;
        if (number == 0)
        {
                axis = string.Empty;
        }
        else if (number == 1)
        {
            axis = "axis01";
        }
        else if (number == 2)
        {
            axis = "axis02";
        }
        else
        {
            throw new ArgumentException("Invalid axis number");
        }


        // Send the data and read the response
        string cmd = string.IsNullOrEmpty(axis) ? "cmd_check_position()" : $"cmd_check_position(\"{axis}\")";
        WriteData(cmd);
        string response = ReadUntilDelimiter(delimiter);
        Console.WriteLine(response);

        // Parse the position values
        if (string.IsNullOrEmpty(axis))
        {
            var positions = response.Replace(delimiter, "")
                                    .Split(new[] { "check_position_axis01:" }, StringSplitOptions.None)[1]
                                    .Split(new[] { "check_position_axis02:" }, StringSplitOptions.None);
            return new List<double> { double.Parse(positions[0].Trim()), double.Parse(positions[1].Trim()) };
        }
        else
        {
            string position = response.Replace(delimiter, "")
                                      .Split(new[] { $"check_position_{axis}:" }, StringSplitOptions.None)[1]
                                      .Trim();
            return new List<double> { double.Parse(position) };
        }
    }

    public bool CheckPosition(int number, List<double> position, double posRange = 0.02)
    {
        // Get the check position based on the axis number
        var ret = GetCheckPosition(number);

        if (number != 0)
        {
            return Math.Abs(ret[0] - position[0]) < posRange;
        }
        else
        {
            return Math.Abs(ret[0] - position[0]) < posRange && Math.Abs(ret[1] - position[1]) < posRange;
        }
    }

    public void CheckPosTill(int number, List<double> position, double timeout, double posRange = 0.02)
    {
        // Record the start time
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalSeconds < timeout)
        {
            if (CheckPosition(number, position, posRange))
            {
                Console.WriteLine($"Move {number}--{position} complete");
                break;
            }
            Thread.Sleep(20); // Sleep for 20 milliseconds
        }
    }

    public string GetCheckVelocity(int number)
    {
        Regex pattern = new Regex(@"(?<=:)\d+\.?\d*");

        //string cmd = number switch
        //{
        //    0 => "cmd_check_velocity()",
        //    1 => "cmd_check_velocity(\"axis01\")",
        //    2 => "cmd_check_velocity(\"axis02\")",
        //    _ => throw new ArgumentException("Invalid number")
        //};


        string cmd = string.Empty;
        if (number == 0)
        {
            cmd = "cmd_check_velocity()";
        }
        else if (number == 1)
        {
            cmd = "cmd_check_velocity(\"axis01\")";
        }
        else if (number == 2)
        {
            cmd = "cmd_check_velocity(\"axis02\")";
        }
        else
        {
            throw new ArgumentException("Invalid number");
        }


        WriteData(cmd);
        string _res = ReadUntilDelimiter(delimiter);
        Console.WriteLine($"{_res}");
        var res_velocity = pattern.Matches(_res);

        return number == 0
            ? $"{res_velocity[0].Value}, {res_velocity[1].Value}"
            : res_velocity[0].Value;
    }

    // TODO: 验证返回的输入信号有问题
    public string GetInput()
    {
        string command = "cmd_get_output()";

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        // Return based on specific responses
        return resResult;
    }

    public string GetOutput()
    {
        string command = "cmd_get_input()";

        WriteData(command);
        string resResult = ReadUntilDelimiter(delimiter).ToLower();
        Console.WriteLine(resResult);

        // Return based on specific responses
        return resResult;
    }

    public bool CmdRecordStart(double rate, double sample)
    {
        // Send the command with the provided rate and sample values
        WriteData($"cmd_record_start{{\"rate\":{rate},\"sample\":{sample}}}");

        // Check if the response contains "true"
        return ReadUntilDelimiter(delimiter).ToLower().Contains("true");
    }

    public bool CmdRecordStop(double rate, double sample)
    {
        WriteData($"cmd_record_stop()");

        // Check if the response contains "true"
        return ReadUntilDelimiter(delimiter).ToLower().Contains("true");
    }

    public bool CmdSetTestResult(string result)
    {
        // result_dict: 想要设置的结果'{"SN123":1,"SN234":2,"SN345":3,"SN456":1}'
        WriteData($"cmd_set_test_result({result})");

        // Check if the response contains "true"
        return ReadUntilDelimiter(delimiter).ToLower().Contains("true");
    }
}


public class PlcMotion : IDisposable
    {
    private bool plcStatus;
    private PlcCom bfPLC;

    public PlcMotion(string comPort, int baudRate, Parity parity = Parity.None)
    {
        plcStatus = true;
        bfPLC = new PlcCom(comPort, baudRate);
    }

        // TODO: define home position in a json file



        public void Dispose()
        {
            Dispose(false);
            //GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //释放托管资源 ：.NET运行时管理的资源，比如普通的C#对象。 *这些资源会由垃圾回收器（GC）自动清理
            }
            else
            {
                // 释放非托管资源： 如文件句柄，PLC句柄、数据库链接，（操作系统层面的资源）  *GC不会自动清理这些资源，需要手动清理
                if (bfPLC.IsOpen )
                {

                    try
                    {
                        bfPLC.Close();
                        //bfPLC = null;
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
            }
        }

        public void CloseBf()
        {
            try
            {
                //if (bfPLC.IsOpen)
                //{
                bfPLC.Close();
                //bfPLC = null;
                //}
            }
            catch (Exception)
            {

                throw;
            }
           
        }
    public void Home(double precision, int delay, double vel1 = 360, double homePos1 = 0, double vel2 = 360, double homePos2 = 0, int timeout = 100000)
    {
        Console.WriteLine("PLC home motion");
        var homeMovingPath = new List<MovingData>
        {
            new MovingData
            {
                Velocity1 = vel1,
                Position1 = homePos1,
                Velocity2 = vel2,
                Position2 = homePos2,
                Delay = delay
            }
        };

        bfPLC.SetCmdHome(0);
        __CheckPos(homeMovingPath, precision, timeout);
    }

    public void MoveOnePoint(double precision, int delay, double Pos1 = 0, double Pos2 = 0, int timeout = 100000)
    {
        Console.WriteLine("PLC home motion");
        var homeMovingPath = new List<MovingData>
        {
            new MovingData
            {
                Velocity1 = 360,
                Position1 = Pos1,
                Velocity2 = 360,
                Position2 = Pos2,
                Delay = delay
            }
        };

        bfPLC.SetCmdHome(0);
        __CheckPos(homeMovingPath, precision, timeout);
    }

    public void MoveAb(List<MovingData> movingList, double precision, int timeout = 100000, bool isCheck = true, bool waitAnyway = false)
    {
        Console.WriteLine($"Path: {movingList} Is Check: {isCheck}");

        __SendPos(movingList, waitAnyway);

        if (isCheck)
        {
            __CheckPos(movingList, precision, timeout);
        }
    }

    private void __SendPos(List<MovingData> movingList, bool waitAnyway)
    {
        foreach (var _path in movingList)
        {
            var v1 = _path.Velocity1;
            var p1 = _path.Position1;
            var v2 = _path.Velocity2;
            var p2 = _path.Position2;
            var delay = _path.Delay;

            // Log(v1, p1, v2, p2, delay);

            bfPLC.MovePositionSetVelocity(0, v1, v2);
            bfPLC.SendMovePositionAbsolute(0, p1, p2);

            if (delay > 0 || waitAnyway)
            {
                bfPLC.SetWait(delay / 1000);
            }
        }
    }

    private bool __CheckPos(List<MovingData> movingPath, double precision, int timeout = 100000)
    {
        var lastPath = movingPath.Last();  // Get the last element in the list
        var lastV1 = lastPath.Velocity1;
        var lastP1 = lastPath.Position1;
        var lastV2 = lastPath.Velocity2;
        var lastP2 = lastPath.Position2;
        var lastDelay = lastPath.Delay;

        Console.WriteLine($"check position: {lastV1}, {lastP1}, {lastV2}, {lastP2}");

        DateTime startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
        {
            Thread.Sleep(10);
            var pos = bfPLC.GetCheckPosition(0); // (pos1, pos2)
            var pos1 = pos.First();
            var pos2 = pos.Last();

            Console.WriteLine($"Json v1 {lastV1}, p1 {lastP1}, v2 {lastV2}, p2 {lastP2}; Current: pos1={pos1}, pos2={pos2}, Precision={precision}");

            if (lastP1 < 0) lastP1 += 360;
            if (lastP2 < 0) lastP2 += 360;
            if ((Math.Abs(pos1 - lastP1) < precision || ((360 - precision) < Math.Abs(pos1 - lastP1) && Math.Abs(pos1 - lastP1) <= 360)) &&
                (Math.Abs(pos2 - lastP2) < precision || ((360 - precision) < Math.Abs(pos2 - lastP2) && Math.Abs(pos2 - lastP2) <= 360)))
            {
                return true;
            }
        }

        plcStatus = false;
        throw new PLCExcept($"PLC check error: {movingPath}");
    }
}


    //class Program
    //{
    //    class MovingData
    //    {
    //        public double Velocity1 { get; set; }
    //        public double Position1 { get; set; }
    //        public double Velocity2 { get; set; }
    //        public double Position2 { get; set; }
    //        public double Delay { get; set; }
    //    }

    //    static void Main(string[] args)
    //    {

    //        SerialPortInfo seirialInfo = new SerialPortInfo();
    //        seirialInfo.GetSerialNumber("COM1");


    //var plc = new PlcCom("COM1", 115200);

    //plc.SetCmdHome(0);

    //string json = File.ReadAllText("test_sequence_cal.json");
    //var data = JsonConvert.DeserializeObject<Dictionary<string, List<MovingData>>>(json);

    //List<MovingData>? movingList = null;

    //if (data != null && data.ContainsKey("moving"))
    //{
    //    movingList = data["moving"];
    //}

    //// If movingList is null, initialize it to an empty list
    //if (movingList == null)
    //{
    //    movingList = new List<MovingData>();
    //}

    //foreach (var item in movingList)
    //{
    //    Console.WriteLine($"Velocity1: {item.Velocity1}, Position1: {item.Position1}, Velocity2: {item.Velocity2}, Position2: {item.Position2}, Delay: {item.Delay}");
    //    plc.MovePositionSetVelocity(0, item.Velocity1, item.Velocity2);
    //    plc.SendMovePositionAbsolute(0, item.Position1, item.Position2);
    //    plc.SetWait(item.Delay / 1000);
    //}

    //    }
    //}
}