using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using OpenCvSharp;

namespace Test.Modules.WebCameraRecord
{
    public class HikvisionCameraRecorder
    {
        private string IP1, IP2, Admin, Password, ChannelNumber, BasePath;
        private string RtspUrl1, RtspUrl2;
        private volatile bool saveFlag1 = true;
        private volatile bool saveFlag2 = true;
        private VideoCapture cap1, cap2;
        private VideoWriter output1, output2;

        public HikvisionCameraRecorder()
        {
            IP1 = "192.168.254.2";
            IP2 = "192.168.254.3";
            Admin = "admin";
            Password = "et123456";
            ChannelNumber = "D1";
            BasePath = "E:\\WEBCamera_video";
            GenerateRtspUrls();
        }

        private void GenerateRtspUrls()
        {
            RtspUrl1 = $"rtsp://{Admin}:{Password}@{IP1}/{ChannelNumber}/main";
            RtspUrl2 = $"rtsp://{Admin}:{Password}@{IP2}/{ChannelNumber}/main";
        }

        private string GenerateSavePath(string ip)
        {
            string date = DateTime.Now.ToString("yyyyMMdd");
            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string folderPath = Path.Combine(BasePath, date);
            Directory.CreateDirectory(folderPath);
            return Path.Combine(folderPath, $"WEBCameraVideo_{time}_{ip.Split('.')[3]}.mp4");
        }

        public void StartRecording()
        {
            Task.Run(() => RecordVideo(RtspUrl1, ref cap1, ref output1, ref saveFlag1, IP1));
            Task.Run(() => RecordVideo(RtspUrl2, ref cap2, ref output2, ref saveFlag2, IP2));
        }

        private void RecordVideo(string rtspUrl, ref VideoCapture cap, ref VideoWriter output, ref bool saveFlag, string ip)
        {
            cap = new VideoCapture(rtspUrl);
            if (!cap.IsOpened())
            {
                Console.WriteLine($"无法打开摄像头: {ip}");
                return;
            }

            int width = (int)cap.Get(VideoCaptureProperties.FrameWidth);
            int height = (int)cap.Get(VideoCaptureProperties.FrameHeight);
            double fps = cap.Get(VideoCaptureProperties.Fps);
            string filePath = GenerateSavePath(ip);

            output = new VideoWriter(filePath, FourCC.MP4V, fps, new OpenCvSharp.Size(width, height));
            if (!output.IsOpened())
            {
                Console.WriteLine($"无法创建视频文件: {filePath}");
                return;
            }

            while (saveFlag)
            {
                using (var frame = new Mat())
                {
                    if (!cap.Read(frame) || frame.Empty())
                    {
                        Console.WriteLine($"无法获取视频帧: {ip}");
                        break;
                    }
                    output.Write(frame);
                }
            }
        }

        public void StopRecording()
        {
            saveFlag1 = false;
            saveFlag2 = false;
            Thread.Sleep(1000);

            cap1?.Release();
            cap2?.Release();
            output1?.Release();
            output2?.Release();
        }
    }
}


// class Program
// {
//     static void Main(string[] args)
//     {
//         var config = new Dictionary<string, string>
//         {
//             {"IP_1", "192.168.254.2"},
//             {"IP_2", "192.168.254.3"},
//             {"admin", "admin"},
//             {"password", "et123456"},
//             {"channel_number", "D1"},
//             {"base_path", "E:\\WEBCamera_video"}
//         };
//
//         var recorder = new HikvisionCameraRecorder();
//         recorder.StartRecording();
//         Thread.Sleep(6000);
//         recorder.StopRecording();
//     }
// }
