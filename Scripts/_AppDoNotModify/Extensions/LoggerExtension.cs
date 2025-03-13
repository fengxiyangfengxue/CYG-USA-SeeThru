using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test._ScriptExtensions
{
    public static class LoggerExtension
    {
        public static void AddLog(this Action<string> logger, string log)
        {
            if (logger != null && log != null)
                logger(log);
        }
        public static void AddLog(this Action<string> logger, string format, params object[] args)
        {
            if (logger != null && format != null && args != null)
            	logger(string.Format(format, args));
        }

        public static void AddLog(this Action<bool> logger, bool result)
        {
            if (logger != null)
                logger(result);
        }
    }
}
