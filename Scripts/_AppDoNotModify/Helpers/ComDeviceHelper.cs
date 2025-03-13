using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Test._ScriptExtensions;

namespace Test._ScriptHelpers
{
    public class ComDeviceHelper
    {
        public static string FindByName(Action<string> logger, string name)
        {
            return FindDevice(logger, name, string.Empty);
        }

        public static string FindByPID(Action<string> logger, string pid)
        {
            return FindDevice(logger, string.Empty, pid);
        }

        public static string FindByNamePID(Action<string> logger, string name, string pid)
        {
            return FindDevice(logger, name, pid);
        }
          
        public static string FindDevice(Action<string> logger, string name, string pid)
        {
            string comPort = string.Empty;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");

            foreach (ManagementObject d in searcher.Get())
            {
                string devName = Convert.ToString(d["Name"]);
                bool isPresent = Convert.ToString(d["Present"]).Equals("True");
                bool status = Convert.ToString(d["Status"]).Equals("OK");
                string deviceId = Convert.ToString(d["DeviceID"]);
                if (devName.ToUpper().Contains("(COM") && devName.EndsWith(")"))
                {
                    logger.AddLog("--------");
                    logger.AddLog("name = " + devName);
                    logger.AddLog("isPresent = " + Convert.ToString(d["Present"]));
                    logger.AddLog("status = " + Convert.ToString(d["Status"]));
                    logger.AddLog("DeviceID = " + Convert.ToString(d["DeviceID"]));

                    if (isPresent && status)
                    {
                        if (!string.IsNullOrEmpty(name) && devName.IndexOf(name, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

                        if (pid != null && deviceId.IndexOf(pid, StringComparison.OrdinalIgnoreCase) == -1)
                            continue;

                        var tmp = devName.Substring(devName.LastIndexOf("(") + 1);
                        if (tmp.IndexOf(")") > 0)
                        {
                            comPort = tmp.Substring(0, tmp.IndexOf(")"));
                            break;
                        }
                    }
                }
            }

        ReturnAndExit:
            return comPort;

        }

    }
}
