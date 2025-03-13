using GTKWebServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using GTKWebServices.GTKWebServices.SMT;
using Test._Definitions;
using System.Net.Sockets;

namespace Test.StationsScripts.FATP_SuperCal

{

    public class SuperCal_Context
    {

        public SuperCal_Context()
        {
            Reset();
        }


        public void Reset()
        {

        }

        public void ClearUp()
        {

        }

        public void Dispose()
        {
            Reset();
        }

    }



    public class TestIdUpdater
    {
        private readonly object lockObj = new object();
        private string testIdJson = "test_id.json";


        public TestIdUpdater(string testIdPath)
        {
            this.testIdJson = testIdPath;

        }

        public string UpdateTestId(bool isOnMes)
        {
            lock (lockObj)
            {
                // 读取json文件
                Dictionary<string, int> lastTestId = new Dictionary<string, int>();
                if (File.Exists(testIdJson))
                {
                    var jsonData = File.ReadAllText(testIdJson);
                    lastTestId = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonData);
                }

                int testId;
                if (isOnMes)
                {
                    testId = lastTestId.ContainsKey("mes_last_test_id") ? lastTestId["mes_last_test_id"] + 1 : 1;
                    lastTestId["mes_last_test_id"] = testId;
                }
                else
                {
                    testId = lastTestId.ContainsKey("last_test_id") ? lastTestId["last_test_id"] + 1 : 1;
                    lastTestId["last_test_id"] = testId;
                }

                string dir = Path.GetDirectoryName(testIdJson);
                if (!string.IsNullOrEmpty(dir) && dir.Length > 2)
                {
                    Directory.CreateDirectory(dir);
                }

                // 写回Json文件
                var jsonDataToWrite = JsonConvert.SerializeObject(lastTestId, Formatting.Indented);
                File.WriteAllText(testIdJson,jsonDataToWrite);

                return testId.ToString();


            }
        }


    }



    public class MotionPathContent
    {
        public static string sequenceImu = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence.json";
        public static string sequenceLED = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence2.json";
        public static string sequenceCal = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\test_sequence_cal.json";

        public Dictionary<string, string> filePath = new Dictionary<string, string>()
        {

            {"IMU",sequenceImu},
            {"LED",sequenceLED},
            {"STAGE_CAL",sequenceCal}
        };

        public Dictionary<string, List<Dictionary<string, object>>> data;

        public MotionPathContent()
        {
            // 定义一个包含空字典的列表
            var defaultValue = new List<Dictionary<string, Object>> { new Dictionary<string, object>() };
            // 初始化字典
            data = new Dictionary<string, List<Dictionary<string, object>>>()
            {
                { "IMU", defaultValue },
                { "STAGE_CAL", defaultValue },
                { "LED_01", defaultValue },
                { "LED_02", defaultValue },
                { "LED_03", defaultValue },
                { "LED_04", defaultValue },
                { "LED_05", defaultValue },
                { "LED_06", defaultValue },
                { "LED_07", defaultValue },
                { "LED_08", defaultValue }
            };

            LoadData();
        }

        public void LoadData()
        {
            LoadDataIMU();
            LoadDataLED();
            LoadDataCal();
        }
        public List<Dictionary<string, int>> GetData(string name)
        {
            if (data.ContainsKey(name))
            {
                List<Dictionary<string, int>> listInt = data[name].Select(dict => dict.ToDictionary(pair => pair.Key,
                    pair => Convert.ToInt32(pair.Value))).ToList();

                return listInt;
            }
            else
            {
                throw new Exception($"motion data name {name} can't be found!");
            }
        }
        public void LoadDataIMU()
        {
            var __IMU = ReadFromFile("IMU");
            data["IMU"] = __IMU;
        }

        public void LoadDataLED()
        {
            for (int i = 1; i <= 8; i++)
            {
                string key = $"LED_0{i}";
                data[key] = UpdateMovingByKey(key);
            }
        }

        public void LoadDataCal()
        {
            var __STAGE_CAL = ReadFromFile("STAGE_CAL");
            data["STAGE_CAL"] = __STAGE_CAL;
        }

        public List<Dictionary<string, object>> ReadFromFile(string name)
        {
            if (filePath.TryGetValue(name, out string dataPath))
            {
                string jsonContent = File.ReadAllText(dataPath);
                JObject doc = JObject.Parse(jsonContent);

                // 将 doc["moving"] 转换为 JArray
                JArray movingArray = (JArray)doc["moving"];


                List<Dictionary<string, object>> movingData = movingArray
                    .Select(item => item.ToObject<Dictionary<string, object>>())
                    .ToList();

                return movingData;

            }
            else
            {
                throw new Exception($"文件路径未定义: {name}");
            }
        }

        private List<Dictionary<string, object>> UpdateMovingByKey(string indexKey)
        {
            List<Dictionary<string, object>> movingLed = ReadFromFile("LED");
            foreach (var moveDate in movingLed)
            {
                if (moveDate.ContainsKey("position2"))
                {
                    int updatedPosition = Convert.ToInt32(moveDate["position2"]) + 45 * (int.Parse(indexKey[indexKey.Length - 1].ToString()) - 1);
                    moveDate["position2"] = updatedPosition >= 360 ? updatedPosition - 360 : updatedPosition;
                }
            }

            return movingLed;
        }

    }


    public class TcInt
    {
        private const int Port = 21567;
        private const int BufSize = 8192;

        private string host;
        private int port;
        private int bufsize;
        private TcpClient tcpCliSock;
        private NetworkStream stream;
        private readonly object lockObj = new object();

        public TcInt(int port = Port)
        {
            this.port = port;
            this.host = "localhost";
            this.bufsize = BufSize;

            this.tcpCliSock = new TcpClient();
            // todo:暂时屏蔽
            InitTcpClient();
        }

        public void InitTcpClient()
        {
            tcpCliSock.Connect(host, port);
            stream = tcpCliSock.GetStream();
        }

        public (bool, string) ClientCommunicate(string cliData, string end = "ok", int timeout = 20)
        {
            lock (lockObj)
            {
                byte[] byteToSend = Encoding.UTF8.GetBytes(cliData);
                stream.Write(byteToSend, 0, byteToSend.Length);

                StringBuilder dataBuilder = new StringBuilder();
                bool result = false;
                DateTime dateTime = DateTime.Now;

                while (true)
                {
                    byte[] buffer = new byte[bufsize];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    dataBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    if (dataBuilder.ToString().Contains(end))
                    {
                        result = true;
                        break;
                    }

                    else if ((DateTime.Now - dateTime).TotalSeconds > timeout)
                    {
                        break;
                    }

                }

                return (result, dataBuilder.ToString());

            }
        }

        public void ClientSend(string cliData)
        {
            lock (lockObj)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(cliData);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public string ClientRecv(string cliData)
        {
            lock (lockObj)
            {
                byte[] byteRead = new byte[bufsize];
                int readDataLength = stream.Read(byteRead, 0, byteRead.Length);

                return Encoding.UTF8.GetString(byteRead, 0, readDataLength);
            }
        }

        public void CloseTcpClient()
        {
            stream.Close();
            tcpCliSock.Close();

        }
    }
}

