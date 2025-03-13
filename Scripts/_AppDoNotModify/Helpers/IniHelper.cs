using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Test._ScriptHelpers
{
    public class IniHelper
    {

        [DllImport("kernel32.dll")]
        public static extern int GetPrivateProfileString(string section, string key, string defval, StringBuilder retval, int size, string filepath);


        [DllImport("kernel32.dll")]
        public static extern int WritePrivateProfileString(string section, string key, string val, string filepath);


        public static string Read(string section, string key, string filepath, bool canEmpty = false)
        {
            string read = string.Empty;
            StringBuilder stringBuilder = new StringBuilder(4096);
            int readed = IniHelper.GetPrivateProfileString(section, key, "", stringBuilder, 4096, filepath);

            if (readed == 0)
            {
                if (!canEmpty)
                    throw new Exception("read " + section + "." + key + " error!");
            }
            else
            {
                read = stringBuilder.ToString().Trim();
            }
            return read; 
        }


    }
}
