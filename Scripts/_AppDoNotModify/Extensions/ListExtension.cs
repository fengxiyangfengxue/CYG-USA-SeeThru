using System.Collections.Generic;
using System.Text;

namespace Test._ScriptExtensions
{
    public static class ListExtension
    {
        public static string CombineToString<T>(this List<T> list, string spliter = "", string preFix = "")
        {
            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                string s = list[i] == null ? string.Empty : list[i].ToString();
                ret.Append((i == 0 ? string.Empty : spliter) + preFix + s);
            }

            return ret.ToString();
        }


    }
}
