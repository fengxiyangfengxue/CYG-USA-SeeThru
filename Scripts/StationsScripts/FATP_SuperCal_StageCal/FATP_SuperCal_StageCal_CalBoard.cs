using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.StationsScripts.FATP_SuperCal;
using NLog;
using System.IO;
using Test;
using UserHelpers.Helpers;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using NLog.Config;
using NLog.Targets;
using Test.Definition;



namespace Test.StationsScripts.FATP_SuperCal_StageCal
{
    internal class FATP_SuperCal_StageCal_CalBoard
    {

        // 创建 logger 实例  
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public TcIntStageCal tc = new TcIntStageCal();
        public TcIntStageCal com_c = new TcIntStageCal(20237);


        private static readonly object filelock = new object();
        public uint nestId = 0;
        public bool algo = false;
        public static string SuperCalConfigPath = System.Windows.Forms.Application.StartupPath + "\\Configs\\SuperCal\\";
        public static string CaesarConfigPath = @"Configs\FATP_SuperCal_StageCal";
        StageCalSetting SuperCalStageCalSetting = XmlSettingHelper.LoadSetting<StageCalSetting>(CaesarConfigPath);

        public Dictionary<string, object> boardInfo = new Dictionary<string, object>()
        {
            {"start_date_time",string.Empty },
            {"start_date",string.Empty },
            {"start_time",string.Empty },
            {"start_time_cnt",0 },
            {"test_id",string.Empty },
            {"cal_log_path",string.Empty },
            {"date_path",string.Empty },
            {"stage_cal_path",string.Empty },
            {"sum_csv_header",string.Empty },
            {"nest_id",0 },
            {"result","Fail" },
            {"fail_item",string.Empty },
            {"cost_time",0.0 },
            {"algo",false },
        };
        public bool InitBoard()
        
        {   
            string logFolder = System.Windows.Forms.Application.StartupPath + "\\Record";
            boardInfo["log_folder"]= logFolder;
            boardInfo["algo"] = algo;
            InitStartTime();
            bool createResult = CreateDatePath();
            return createResult;
        }

        public void SnSend()
        {
            string snValue = boardInfo["test_id"].ToString();
            string total_path = boardInfo["stage_cal_path"].ToString();
            string _total_path_log = boardInfo["cal_log_path"].ToString();
            string zip_name = SuperCalStageCalSetting.zip_stage_cal_file_name;
            Dictionary<string, object> sendDictionary = new Dictionary<string, object>();
            sendDictionary["sn"] = snValue;
            sendDictionary["zip_name"] = zip_name;
            sendDictionary["sn_path"] = total_path;
            sendDictionary["sn_log_path"] = _total_path_log;
            sendDictionary["nest_id"] = nestId;
            var( result, message )= tc.ClientCommunicate($"{sendDictionary}");
            if (!result )
                throw new Exception($" server sn log error:{message}");

        }

        public void AlgoOnSendBoard()
        {
            string __test_id = boardInfo["test_id"].ToString();
            string total_path = boardInfo["dst_stage_cal_path"].ToString();
            
            Dictionary<string, Dictionary<string,object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();
            string algorithmKey = "algo on cal";
            Dictionary<string, object> internalDictionary = new Dictionary<string, object>() {
                { "sn_path", total_path },
                { "test_id", __test_id },
                { "nest_id", nestId }
            };
            sendDictionary[algorithmKey] = internalDictionary;

            var (result, message) = tc.ClientCommunicate($"{sendDictionary}");
            if (!result)
                throw new Exception($" server sn log error:{message}");

        }

        public void ProcessSend()
        {
            string calPath = boardInfo["stage_cal_path"].ToString();

            Dictionary<string, Dictionary<string, object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();
            string algorithmKey = "processing data";
            Dictionary<string, object> internalDictionary = new Dictionary<string, object>() {
                { "sn_path", calPath },
                { "nest_id", nestId }
            };
            sendDictionary[algorithmKey] = internalDictionary;

            var (result, message) = tc.ClientCommunicate($"{sendDictionary}");
            if (!result)
                throw new Exception($" server sn log error:{message}");
        }

        public void ImagePath(ITestItem item)
        {
            string total_path = boardInfo["stage_cal_path"].ToString();
            item.AddLog($"stage_cal_path:{total_path}");
            var (result, message) = com_c.ClientCommunicate($"path,{total_path}");
            if (!result)
                throw new Exception($"camera image error: {message}");
        }

        public void ClearPath()
        {
            var (result, message) = com_c.ClientCommunicate($"empty,{111111}");
            if (!result)
                throw new Exception($"camera image error: {message}");
        }

        public void sumCsvSend()
        {
            DateTime timeD  = (DateTime) boardInfo["start_time_cnt"];
            int costTime = (int)(DateTime.Now - timeD).TotalSeconds;
            
            boardInfo["costTime"] = costTime;
            var dictTemp = boardInfo;

            Dictionary<string, Dictionary<string, object>> sendDictionary = new Dictionary<string, Dictionary<string, object>>();

            string algorithmKey = "sum csv";
            Dictionary<string, object> internalDictionary = new Dictionary<string, object>
            {
                { "dut_info", dictTemp },
            };

            sendDictionary[algorithmKey] = internalDictionary;

            var (result, message) = tc.ClientCommunicate($"{sendDictionary}");
            if (!result)
                throw new Exception($" server sn log error:{message}");
        }

        public bool CreateDatePath()
        {
            bool result = false;
            try
            {
                lock (filelock)
                {
                    var config = new LoggingConfiguration();

                    string lastTestIdPath = SuperCalConfigPath + "//last_test_id.json";
                    TestIdUpdater testIdUpdater = new TestIdUpdater(lastTestIdPath);
                    boardInfo["test_id"] = testIdUpdater.UpdateTestId(false);
                    
                    string calLogPath = Path.Combine(SuperCalStageCalSetting.cal_image_path, (boardInfo["start_date"]).ToString(),
                        "log");
                    FindFile(calLogPath);

                    boardInfo["date_path"] = Path.Combine(SuperCalStageCalSetting.cal_image_path,
                        (boardInfo["start_date"]).ToString());

                    boardInfo["cal_log_path"] = calLogPath;
                    string logCalLogPath = calLogPath +"//"+ DateTime.Now.ToString("MM_dd")+".log";
                    var fileTarget = new FileTarget("fileTarget") {
                        FileName = logCalLogPath,
                        Layout = "${longdate} ${level} ${message} ${exception:format=tostring}"
                    };
                    // 将文件目标添加到配置中  
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
                    LogManager.Configuration = config;
                    string stage_cal_path = Path.Combine(SuperCalStageCalSetting.cal_image_path,
                        (boardInfo["start_date"]).ToString(), boardInfo["test_id"].ToString());

                    string temp = Path.Combine(SuperCalStageCalSetting.Convert);
                    string temp2 = stage_cal_path.Substring(Path.GetPathRoot(stage_cal_path).Length);
                    logger.Debug($"路径1 {temp}，路径2 {temp2}");

                    string dst_stage_cal_path = Path.Combine(SuperCalStageCalSetting.Convert,
                        stage_cal_path.Substring(Path.GetPathRoot(stage_cal_path).Length));

                    FindFile(stage_cal_path);
                    boardInfo["stage_cal_path"] = stage_cal_path;
                    boardInfo["dst_stage_cal_path"] = dst_stage_cal_path;

                    GenerateSummaryCsv();
                    result = true;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine( e );
                result = false;
            }

            return result;
        }

        public void GenerateSummaryCsv()
        {
            var csvHeader = new List<string> { "start_date_time", "test_id", "result", "cost_time" };
            var csvPath = Path.Combine(Path.GetDirectoryName(boardInfo["cal_log_path"].ToString()), "summary.csv");
            boardInfo["sum_file_path"] = csvPath;
            boardInfo["sum_csv_header"] = string.Join(",",csvHeader);
            MultiSumCsv sumCsv = new MultiSumCsv(csvPath, csvHeader, logger);
            sumCsv.CreateSumCsv();


        }

        /// <summary>  
        /// 寻找文件或者文件夹是否存在  
        /// </summary>  
        /// <param name="path">文件或者文件夹完整名称</param>  
        /// <returns>原始路径</returns>  
        public static string FindFile(string path)
        {
            // 检查路径是否存在  
            if (File.Exists(path) || Directory.Exists(path))
            {
                // 路径存在，什么都不做  
                return path;
            }
            else
            {
                // 获取文件名及路径  
                string directoryPath = Path.GetDirectoryName(path);

                // 检查当前路径中是否包含文件扩展名  
                if (Path.GetFileName(path).Contains("."))
                {
                    // 创建文件所在的目录  
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
                else
                {
                    // 创建文件夹  
                    Directory.CreateDirectory(path);
                    logger.Info($"Create folder is successful! {path}");
                }
            }

            return path;
        }
    

        public void InitStartTime()
        {
            DateTime timeNow = DateTime.Now;
            boardInfo["start_date_time"] = timeNow.ToString("yyyy_MM_dd HH:mm:ss");
            boardInfo["start_date"] = timeNow.ToString("yyyy_MM_dd");
            boardInfo["start_time"] = timeNow.ToString("HH:mm:ss");
            boardInfo["start_time_cnt"] = DateTime.Now;

        }

        public bool CheckImgLed(int plc_trigger_cnt)
        {
            bool result = false;
            int all_cam_bmp_num_list = 0;

            try
            {
                string stage_cal_path = boardInfo["stage_cal_path"].ToString();
                var countList = GetAllCamImgNumber(stage_cal_path, "bmp");
                SuperCal_Setting setting = XmlSettingHelper.LoadSetting<SuperCal_Setting>(@"Configs");

                if (countList.Count == setting.cam_num && countList.Distinct().Count() == 1 &&
                    Math.Abs(plc_trigger_cnt - countList[0]) < 4)
                {
                    result = true;
                }

            }
            catch (Exception e)
            {
                result = false;
            }

            return result;

        }

        public List<int> GetAllCamImgNumber(string imagePath, string imageEndName)
        {
            List<int> imageNumberList = new List<int>();


            // 使用 Directory.GetDirectories 来选择所有匹配的文件夹 

            // 使用 LINQ 筛选出满足条件的图像文件  
            foreach (var directory in Directory.GetDirectories(imagePath, "cam_*"))
            {
                // 获取文件夹中的所有文件
                string[] allDir = Directory.GetFiles(directory);
                // 使用 LINQ 筛选出满足条件的图像文件 
                var fileList = allDir.Where(file => file.EndsWith($".{imageEndName}", StringComparison.OrdinalIgnoreCase)).ToList();
                imageNumberList.Add(fileList.Count);

            }

            return imageNumberList;

        }



    }





    public class MultiSumCsv
    {
        private string csvPath;
        private List<string> fileHeader;
        private ILogger logger;

        public MultiSumCsv(string csvPath, List<string> fileHeader, ILogger logger)
        {
            this.csvPath = csvPath;
            this.fileHeader = fileHeader;
            this.logger = logger;
        }

        public void CreateSumCsv()
        {
            if (this.logger != null)
            {
                this.logger.Debug("Create sum csv");
            }

            while (true)
            {
                if (!File.Exists(this.csvPath))
                {
                    var sumFileHeader = fileHeader.ToDictionary(x => x, x => new List<string>());
                    if (this.logger != null)
                    {
                        this.logger.Debug(string.Join(", ", sumFileHeader.Select(kv => $"{kv.Key}: {string.Join(",", kv.Value)}")));
                    }
                    ColWCsv(this.csvPath, sumFileHeader, "w");
                }
                else
                {
                    if (this.logger != null)
                    {
                        this.logger.Debug($"Summary csv exists at: {this.csvPath}");
                    }
                    break;
                }
            }
        }

        public void AppendDataToSumCsv(Dictionary<string, string> data)
        {
            // ---- Update summary.csv ----
            if (this.logger != null)
            {
                this.logger.Debug("---- Append data to summary csv ----");
                this.logger.Debug($"Summary csv path: {this.csvPath}");
                this.logger.Debug($"Appended data: {string.Join(", ", data.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            }

            var dataDict = data.Where(kv => fileHeader.Contains(kv.Key))
                               .ToDictionary(kv => kv.Key, kv => new List<string> { kv.Value });

            // Order dataDict according to fileHeader
            var orderedData = fileHeader.ToDictionary(h => h, h => dataDict.ContainsKey(h) ? dataDict[h] : new List<string>());

            ColWCsv(this.csvPath, orderedData, "a");
        }

        private void ColWCsv(string path, Dictionary<string, List<string>> data, string mode)
        {
            using (var writer = new StreamWriter(path, mode == "a"))
            {
                if (mode == "w")
                {
                    // Write the header
                    writer.WriteLine(string.Join(",", data.Keys));
                }

                // Write data (assuming all lists have the same length)
                var rows = data.Values.FirstOrDefault()?.Count ?? 0;
                for (int i = 0; i < rows; i++)
                {
                    var row = data.Select(kv => kv.Value[i]).ToList();
                    writer.WriteLine(string.Join(",", row));
                }
            }
        }
    }

}
