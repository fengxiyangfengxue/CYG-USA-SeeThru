using LitJson;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Test._ScriptExtensions
{
    public static class StringExtension
    {
        public static bool IsNumeric(this string s)
        {
            return Regex.IsMatch(s, @"^[+-]?\d*[.]?\d*$");
        }

        public static bool IsNumber(this string s)
        {
            return Regex.IsMatch(s, @"^[0-9]*$");
        }

        public static bool IsABC(this string s)
        {
            return Regex.IsMatch(s, @"^[A-Z]+$");
        }

        public static bool Isabc(this string s)
        {
            return Regex.IsMatch(s, @"^[a-z]+$");
        }

        public static bool IsAbcNumber(this string s)
        {
            return Regex.IsMatch(s, @"^[A-Za-z0-9]+$");
        }

        public static bool IsAbc(this string s)
        {
            return Regex.IsMatch(s, @"^[A-Za-z]+$");
        }

        public static bool HasChinese(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }
  
        public static string FormatResultData(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            //string pattern = "^[A-Za-z0-9_\\-\\.]+$";
            string pattern = @"^[A-Za-z0-9_\.\-]+$";
            if (!Regex.IsMatch(str, pattern))
            {
                //包含特殊字符的去掉
                //str = Regex.Replace(str, "[^A-Za-z0-9_\\-\\.]+", "_");
                str = Regex.Replace(str, @"[^A-Za-z0-9_\.\-]+", "_");
            }
            //将连续的多个替换掉
            str = Regex.Replace(str, @"[_]+", "_");
            return str;
        }

        public static string VBReplace(this string s, string oldValue, string newValue)
        {
            return Strings.Replace(s, oldValue, newValue, 1, -1, CompareMethod.Text);
        }

        public static string CharsReplace(this string s, string oldChars, char newChar)
        { 
            var chs = oldChars.ToCharArray();
            foreach (var c in chs)
                s = s.Replace(c, newChar); 
            return s;
        }

        public static string RemoveChars(this string s, string oldChars)
        {
            var chs = oldChars.ToCharArray();
            foreach (var c in chs)
                s = s.Replace(c.ToString(), "");
            return s;
        }

        public static string RemoveCRLF(this string s)
        {
            return s.RemoveChars("\r\n"); 
        } 
         
        //public static string ToHexString(this string s)
        //{
        //    return s.ToBytes().ToHexString();
        //}

        //public static string ToHexString(this string s, Encoding encode)
        //{
        //    return s.ToBytes(encode).ToHexString();
        //}

        public static string HexToString(this string hexString)
        {
            return hexString.HexToString(Encoding.Default);
        }

        public static string HexToString(this string hexString, Encoding encode)
        {
            string strTemp = "";
            byte[] b = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length / 2; i++)
            {
                strTemp = hexString.Substring(i * 2, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            return encode.GetString(b);
        }

        public static byte[] HexToByte(this string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
       
        public static byte[] ToBytes(this string str)
        {
            return Encoding.Default.GetBytes(str);
        }

        public static byte[] ToBytes(this string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        public static int ToInteger(this string str, int def = 0, int min = int.MinValue, int max = int.MaxValue)
        {
            int ret = 0;
            if (!int.TryParse(str, out ret))
                ret = def;

            if (ret < min || ret > max)
                ret = def;

            return ret;
        }

        public static double ToDouble(this string str, double def = 0, double min = double.MinValue, double max = double.MaxValue)
        {
            double ret = 0;
            if (!double.TryParse(str, out ret))
                ret = def;

            if (ret < min || ret > max)
                ret = def;

            return ret;
        }


        public static List<string> SplitToList(this string str, string spliter, bool isTrim = true, bool removeEmpty = true)
        {
            List<string> list = new List<string>();
            List<string> tmp = str.Split(spliter.ToCharArray()).ToList();

            tmp.ForEach(s1 =>
            {
                string s = s1;

                if (isTrim)
                    s = s1.Trim();

                if (removeEmpty)
                {
                    if (!string.IsNullOrEmpty(s))
                        list.Add(s);
                }
                else
                    list.Add(s);
            });
            return list;
        }

        public static string ToAutoString(this byte[] bytes)
        {
            string ret = "";
             
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length); 
                ret = Marshal.PtrToStringAuto(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return ret;
        }

        public static string BeautifyJson(this string json)
        {
            var jd = JsonMapper.ToObject(json);
            JsonWriter jw = new JsonWriter();
            jw.PrettyPrint = true;
            jd.ToJson(jw);
            return jw.ToString().ChineseJson();
        }

        public static string ChineseJson(this string json)
        {
            Regex reg = new Regex(@"(?i)\\[uU]([0-9a-f]{4})");
            string ret = reg.Replace(json, delegate (Match m) { return ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString(); });
            return ret;
        }

        public static string CompressJson(this string json)
        {
            var jd = JsonMapper.ToObject(json);
            JsonWriter jw = new JsonWriter();
            jw.PrettyPrint = false;
            jd.ToJson(jw);
            return jw.ToString().ChineseJson();
        }

        public static T ToEnum<T>(this string str)
        {
            if (!Enum.IsDefined(typeof(T), str))
                throw new Exception(str + " not defined in type " + typeof(T).ToString());

            T ret = (T)Enum.Parse(typeof(T), str);

            return ret;
        }

 
    }
}
