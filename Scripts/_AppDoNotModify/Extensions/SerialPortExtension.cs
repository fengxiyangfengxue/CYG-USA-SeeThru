using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using UserHelpers.Helpers;

namespace Test._ScriptExtensions
{
    public static class SerialPortExtension
    { 
        public static void LogConfig(this SerialPort com, Action<string> logger = null)
        { 
            logger.AddLog("PortName = " + com.PortName);
            logger.AddLog("BaudRate = " + com.BaudRate.ToString());
            logger.AddLog("Parity = " + com.Parity.ToString());
            logger.AddLog("DataBits = " + com.DataBits);
            logger.AddLog("StopBits = " + com.StopBits.ToString());
        }
         
        public static void CharWrite(this SerialPort com, string cmd, Action<string> logger = null)
        {
            logger.AddLog("Write : " + cmd); 
            for (int i = 0; i < cmd.Length; i++)
            {
                string ch = cmd.Substring(0, 1); 
                Thread.Sleep(5);
                com.Write(ch);
            } 
        }

        public static void ByteWrite(this SerialPort com, byte[] cmd, Action<string> logger = null)
        {
            logger.AddLog("Write : " + cmd.ToHexString("0x", " "));
            com.Write(cmd, 0, cmd.Length);
        }

        public static void LogWrite(this SerialPort com, string cmd, Action<string> logger = null)
        {
            logger.AddLog("Write : " + cmd);
            com.Write(cmd);
        }
        public static void LogWriteCRLF(this SerialPort com, string cmd, Action<string> logger = null)
        {
            cmd = cmd + "\r\n";
            logger.AddLog("Write : " + cmd);
            com.Write(cmd);
        }


        public static void LogWriteLine(this SerialPort com, string cmd, Action<string> logger = null)
        {
            logger.AddLog("WriteLine : " + cmd);
            com.WriteLine(cmd);
        }

        public static string LogReadLine(this SerialPort com, Action<string> logger = null)
        {
            string read = com.ReadLine();
            logger.AddLog("ReadLine : " + read);
            return read;
        }

        public static string LogReadExisting(this SerialPort com, Action<string> logger = null)
        {
            string read = string.Empty;
            read = com.ReadExisting();
            if (read.Length > 0)
                logger.AddLog("ReadExisting : " + read);
            return read;
        }

        public static string LogReadToCRLF(this SerialPort com, int timeOut = 5000, Action<string> logger = null)
        {
            string read = string.Empty;
            bool result = false;
            DateTime dtEnd = DateTime.Now.AddMilliseconds(timeOut);

            while(DateTime.Now < dtEnd)
            { 
                var tmp = com.ReadExisting();
                read = read + tmp;
                if (read.EndsWith("\r\n"))
                {
                    result = true;
                    break;
                }
                Thread.Sleep(1);
            }
            
            if(!result)
                logger.AddLog("LogReadToCRLF : CRLF not found!");

            if (read.Length > 0)
                logger.AddLog("LogReadToCRLF : " + read);

            return read;
        }
    }
}
