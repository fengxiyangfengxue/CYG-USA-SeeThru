// WebCameraRecorder.cs
using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV; // 需要安装 Emgu.CV 和 Emgu.CV.runtime.windows
// using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using NLog;
using UserHelpers.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Emgu.CV.Ocl;
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




namespace Test.Modules.WebCameraRecord
{
    public class WebCameraRecorder : IDisposable
    {
        private VideoCapture _cam1, _cam2;
        private VideoWriter _writer1, _writer2;
        private volatile bool _isRecording;
        private readonly Dictionary<string, object> jsonWebConfig = null;

        public WebCameraRecorder()
        {
            jsonWebConfig = ReadWriteJson.LoadJsonConfig(Path.Combine(@"Configs", "FATP_SeeThru", "Webconfig.json"));

        }

        /// <summary>
        /// 开始录制（异步方法）
        /// </summary>
        public async Task StartRecordingAsync()
        {
            if (_isRecording) return;

            // 生成保存路径
            var (path1, path2) = GenerateSavePaths();
            Directory.CreateDirectory(path1);
            Directory.CreateDirectory(path2);

            // 初始化摄像头
            var rtspUrl1 =
                $"rtsp://{(jsonWebConfig.ContainsKey("admin") ? jsonWebConfig["admin"].ToString() : "admin")}:{(jsonWebConfig.ContainsKey("password") ? jsonWebConfig["password"].ToString() : "et123456")}@{(jsonWebConfig.ContainsKey("ip_1") ? jsonWebConfig["ip_1"].ToString() : "192.168.254.2")}/{(jsonWebConfig.ContainsKey("channel_number") ? jsonWebConfig["channel_number"].ToString() : "D1")}/main";
            var rtspUrl2 =
                $"rtsp://{(jsonWebConfig.ContainsKey("admin") ? jsonWebConfig["admin"].ToString() : "admin")}:{(jsonWebConfig.ContainsKey("password") ? jsonWebConfig["password"].ToString() : "et123456")}@{(jsonWebConfig.ContainsKey("ip_2") ? jsonWebConfig["ip_2"].ToString() : "192.168.254.3")}/{(jsonWebConfig.ContainsKey("channel_number") ? jsonWebConfig["channel_number"].ToString() : "D1")}/main";

            _cam1 = new VideoCapture(rtspUrl1);
            _cam2 = new VideoCapture(rtspUrl2);

            if (!_cam1.IsOpened || !_cam2.IsOpened)
            {
                throw new Exception("摄像头连接失败");
            }

            // 配置视频写入器
            var fourcc = VideoWriter.Fourcc('M', 'P', '4', 'V');
            var fps = _cam1.Get(Emgu.CV.CvEnum.CapProp.Fps);
            var width = (int)_cam1.Get(Emgu.CV.CvEnum.CapProp.FrameWidth);
            var height = (int)_cam1.Get(Emgu.CV.CvEnum.CapProp.FrameHeight);


            _writer1 = new VideoWriter(
                Path.Combine(path1, GetFileName()), // 文件路径
                fourcc,                             // 编码格式（如MP4V）
                0,                                  // API 后端（0=自动选择）
                fps,                                // 帧率
                new Size(width, height),            // 必须封装成 Size 对象
                true                                // 是否为彩色
            );

            _writer2 = new VideoWriter(
                Path.Combine(path1, GetFileName()), // 文件路径
                fourcc,                             // 编码格式（如MP4V）
                0,                                  // API 后端（0=自动选择）
                fps,                                // 帧率
                new Size(width, height),            // 必须封装成 Size 对象
                true                                // 是否为彩色
            );



            _isRecording = true;

            // 启动双通道录制任务
            var task1 = Task.Run(() => RecordCamera(_cam1, _writer1));
            var task2 = Task.Run(() => RecordCamera(_cam2, _writer2));

            await Task.WhenAll(task1, task2);
        }

        /// <summary>
        /// 停止录制
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
            Thread.Sleep(1000); // 等待最后一帧写入

            _cam1?.Dispose();
            _cam2?.Dispose();
            _writer1?.Dispose();
            _writer2?.Dispose();
        }

        private void RecordCamera(VideoCapture camera, VideoWriter writer)
        {
            try
            {
                while (_isRecording)
                {
                    using (var frame = new Mat())
                    {
                        if (camera.Read(frame) && !frame.IsEmpty)
                        {
                            writer.Write(frame);
                        }
                        else
                        {
                            Console.WriteLine("视频帧获取失败");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"录制异常: {ex.Message}");
            }
        }

        private (string Path1, string Path2) GenerateSavePaths()
        {
            var timeStamp = DateTime.Now.ToString("yyyyMMdd");
            var basePath = jsonWebConfig.ContainsKey("base_path") ? jsonWebConfig["base_path"].ToString() : @"E:\WEBCamera_video";

            return (
                Path.Combine(basePath, timeStamp, "2"),
                Path.Combine(basePath, timeStamp, "3")
            );
        }

        private string GetFileName() =>
            $"WEBCameraVideo_{DateTime.Now:HHmmss}.mp4";

        public void Dispose() => StopRecording();
    }
}



// appsettings.json 配置类
public class CameraConfig
{
    public string IP1 { get; set; } = "192.168.254.2";
    public string IP2 { get; set; } = "192.168.254.3";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "et123456";
    public string Channel { get; set; } = "D1";
    public string BaseSavePath { get; set; } = @"E:\WEBCamera_video";
}

// Program.cs 使用示例
// class Program
// {
//     static async Task Main(string[] args)
//     {
//         using var recorder = new WebCameraRecorder();
//
//         try
//         {
//             await recorder.StartRecordingAsync();
//             Console.WriteLine("录制已启动，按任意键停止...");
//             Console.ReadKey();
//         }
//         finally
//         {
//             recorder.StopRecording();
//             Console.WriteLine("录制已停止");
//         }
//     }
// }