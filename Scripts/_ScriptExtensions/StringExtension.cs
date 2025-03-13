using LitJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Test._ScriptExtensions
{
    public static class StringExtension_GTK
    {
        /// <summary>
        /// 从json数据总获取数据
        /// </summary>
        /// <param name="json">元数据</param>
        /// <param name="args">key:key,每个key对应的一级多层用:隔开</param>
        /// <returns></returns>
        public static string StringParse(this string json, string args)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(args))
            {
                return null; // 检查输入参数是否有效
            }

            try
            {
                var jd = JsonMapper.ToObject(json);
                var keys = args.Split(':');
                for (int i = 0; i < keys.Length; i++)
                {
                    if (jd.ContainsKey(keys[i]) && jd[keys[i]] != null)
                        jd = jd[keys[i]];
                }
                return jd.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static string GetSubString(string mainString, string startString, string endString)
        {
            string result = string.Empty;
            try
            {
                int startIndex = mainString.IndexOf(startString);
                int endIndex = mainString.IndexOf(endString, startIndex + startString.Length);
                if (startIndex != -1 && endIndex != -1)
                {
                    endIndex += endString.Length;
                    result = mainString.Substring(startIndex, endIndex - startIndex);
                }
            }
            catch
            {
                result = string.Empty;
            }
            return result;
        }

        public static string StringParseForArray(this string json, string args)
        {
            try
            {
                var jd = JsonMapper.ToObject(json);
                var keys = args.Split(':');
                for (int i = 0; i < keys.Length; i++)
                {
                    if (jd.ContainsKey(keys[i]) && jd[keys[i]] != null)
                        jd = jd[keys[i]][1];
                }
                return jd.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static JsonData JsonParse(this string json, string args)
        {
            try
            {
                var jd = JsonMapper.ToObject(json);
                var keys = args.Split(':');
                for (int i = 0; i < keys.Length; i++)
                {
                    if (jd.ContainsKey(keys[i]) && jd[keys[i]] != null)
                        jd = jd[keys[i]];
                }
                return jd;
            }
            catch
            {
                return null;
            }
        }

        public static List<double> ListParse(this string json, string args)
        {
            try
            {
                var jd = JsonMapper.ToObject(json);
                var keys = args.Split(':');
                for (int i = 0; i < keys.Length; i++)
                {
                    if (jd.ContainsKey(keys[i]) && jd[keys[i]] != null)
                        jd = jd[keys[i]];
                }
                Newtonsoft.Json.Linq.JArray ja = (Newtonsoft.Json.Linq.JArray)Newtonsoft.Json.JsonConvert.DeserializeObject(jd.ToJson());
                List<double> list = ja.ToObject<List<double>>();

                return list;
            }
            catch
            {
                return null;
            }
        }




        /// <summary>
        /// 判断字符串是否包含特定关系表达式
        /// </summary>
        /// <param name="data">要匹配的字符串</param>
        /// <param name="pattern">匹配关系表达式</param>
        /// <param name="length">匹配长度</param>
        /// <returns>匹配结果</returns>
        public static bool IsMatch(this string data, string pattern, int? length)
        {
            try
            {
                bool retLength = false;
                bool retPattern = false;
                if (string.IsNullOrWhiteSpace(data))
                {
                    return false;
                }

                if (length == null)
                    retLength = true;
                else
                {
                    retLength = data.Length != length;
                }

                if (string.IsNullOrEmpty(pattern))
                    retPattern = true;
                else
                {
                    Regex regex = new Regex(pattern);
                    retPattern = regex.IsMatch(data);
                }
                return retLength && retPattern;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string FormatTestNameData(this string str)
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

        private static readonly Dictionary<string, string> units = new Dictionary<string, string>
        {
            { "℃","C"},
        };
        public static string FormatTestUnitData(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            var item = units.FirstOrDefault(a => a.Key.Equals(str));
            if (string.IsNullOrEmpty(item.Key))
            {
                return str;
            }
            return item.Value;
        }

        public static string FormatTestValueData(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            str = Regex.Replace(str, @"[\r\n,]+", "").Replace(@",", "").Replace(@"\r", "").Replace(@"\n", "").Replace(Environment.NewLine, "");
            return str;
        }


        /// <summary>
        /// 判断字符串是否包含特定关系表达式
        /// </summary>
        /// <param name="data">要匹配的字符串</param>
        /// <param name="pattern">匹配关系表达式</param>
        /// <returns>匹配结果</returns>
        public static bool IsMatch(string data, string pattern)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                Regex regex = new Regex(pattern);
                if (!regex.IsMatch(data))
                    return false;
            }
            return true;
        }


        public static List<int> MatchStr(string rcv, string pattern)
        {
            MatchCollection MatchCaptouch = Regex.Matches(rcv, pattern);
            List<int> Captouch = new List<int>();
            List<int> Touch = new List<int>();

            for (int i = 0; i < MatchCaptouch.Count; i++)
            {
                Captouch.Add(int.Parse(MatchCaptouch[i].Groups[4].ToString()));
                Touch.Add(int.Parse(MatchCaptouch[i].Groups[1].ToString()));
            }
            List<int> result = new List<int>();
            result.Add((int)Captouch.Average());
            result.Add(Captouch.Min());
            result.Add(Captouch.Max());
            result.Add(Captouch.First());
            result.Add(Captouch.Last());

            result.Add((int)Touch.Average());
            result.Add(Touch.Min());
            result.Add(Touch.Max());
            result.Add(Touch.First());
            result.Add(Touch.Last());

            return result;

        }

        public static List<int> MatchCurlStr(string rcv, string pattern)
        {
            MatchCollection MatchCaptouch = Regex.Matches(rcv, pattern);
            List<int> Captouch = new List<int>();
            List<int> Touch = new List<int>();

            for (int i = 0; i < MatchCaptouch.Count; i++)
            {
                Captouch.Add(int.Parse(MatchCaptouch[i].Groups[2].ToString()));
                Touch.Add(int.Parse(MatchCaptouch[i].Groups[1].ToString()));
            }
            List<int> result = new List<int>();
            result.Add((int)Captouch.Average());
            result.Add(Captouch.Min());
            result.Add(Captouch.Max());
            result.Add(Captouch.First());
            result.Add(Captouch.Last());

            result.Add((int)Touch.Average());
            result.Add(Touch.Min());
            result.Add(Touch.Max());
            result.Add(Touch.First());
            result.Add(Touch.Last());

            return result;
        }


        public static List<int> DataStr(List<int> param)
        {
            List<int> result = new List<int>();
            result.Add((int)param.Average());
            result.Add(param.Min());
            result.Add(param.Max());
            result.Add(param.First());
            result.Add(param.Last());


            return result;
        }


    }
}
