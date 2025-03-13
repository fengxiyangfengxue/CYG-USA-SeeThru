using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;



namespace Test.Modules.motion_control
{

    class RobotMotionServer:IDisposable
    {
        private readonly Object _lock = new Object();
        private readonly Socket _socket;
        private bool _disposed;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="iP">Destination IP address</param>
        /// <param name="port">Destination Prot</param>
        /// <param name="timeoutSeconds">connection timeout(S)</param>
        public RobotMotionServer(string iP, int port, int timeoutSeconds = 90)
        {
            try
            {
                // 创建TCP套接字
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // 设置连接超时时间
                _socket.ReceiveTimeout = timeoutSeconds * 1000;
                _socket.SendTimeout = timeoutSeconds * 1000;

                // 建立连接
                var endPoint = new IPEndPoint(IPAddress.Parse(iP), port);
                _socket.Connect(endPoint);

                // 验证初始响应
                var (success, response) = ReadResponse();
                if (!success || !response.Contains("success"))
                {
                    throw new Exception($"Failed to connect to robot motion server: {response}");
                }
                Logger.Info($"Connected to robot motion server:{iP} {response}");

            }
            catch (Exception ex)
            {

                Logger.Error(ex, "Failed to connect to robot motion server");
                Dispose();
                throw;
            }
        }

        public (bool Success,string Data) SendCommand(string command,int timeoutSeconds = 5)
        {
            lock (_lock)
            {
                try
                {
                    // 发送命令
                    var payload = Encoding.ASCII.GetBytes(command + "\n");
                    int sentBytes = _socket.Send(payload);

                    Logger.Debug($"Sent command: [{sentBytes} bytes] : {command}");
                    var (success, response) = ReadResponse(timeoutSeconds);

                    Logger.Debug($"Received response: {response}");
                    return (success, response);
                }

                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to send command({command}) to robot motion server");
                    return (false, $"Send Failed：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 读取响应数据（带超时和终止符检测）
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        private (bool Success, string Data) ReadResponse(int timeoutSeconds = 5)
        {
            var buffer = new byte[1024];
            var response = new StringBuilder();
            var startTime = DateTime.UtcNow;

            try
            {
                while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
                {
                    if (_socket.Available > 0)
                    {
                        int bytesRead = _socket.Receive(buffer);
                        response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                        // 错误检测
                        if (response.ToString().Contains("error_order"))
                        {
                            Logger.Warn($"Received error response: {response}");
                            return (false, "have error_order");
                        }

                        // 终止符检测
                        if (response.ToString().Contains("@_@"))
                        {
                            var cleanData = response.ToString().Split(new[] { "@_@" }, StringSplitOptions.None)[0].Trim();

                            return (true, cleanData);
                        }
                    }
                    Thread.Sleep(100);
                }
                Logger.Warn($"Timeout while waiting for response,  accepted data is {response} ");
                return (false, "Timeout not find @_@");
            }
            catch (Exception ex)
            {

                Logger.Error(ex, "data recv error");
                return (false, $"Data recv error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                try
                {
                    _socket?.Shutdown(SocketShutdown.Both);
                    _socket?.Close();
                    _socket?.Dispose();
                    Logger.Info("Connrct closed");

                }
                catch(Exception ex) 
                {
                    Logger.Error(ex, "Failed to close connection");
                }
                _disposed = true;

            }
            GC.SuppressFinalize(this);
        }

        ~RobotMotionServer()
        {
            Dispose();
        }
    }
}
