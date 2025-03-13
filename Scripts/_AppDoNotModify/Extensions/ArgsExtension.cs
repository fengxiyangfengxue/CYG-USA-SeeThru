using System;
using System.Collections.Generic;
using System.Linq;
using UserHelpers.Helpers;

namespace Test._ScriptExtensions
{
    public static class ScriptExtension
    {
        public static string[] ArgsJoin(this string[] args, string parameter)
        {
            var argsList = args.ToList();
            string tmp = argsList.FirstOrDefault(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            if (tmp == null)
                argsList.Add(parameter);
            return argsList.ToArray();
        }

        public static string[] ArgsJoin(this string[] args, string parameter, string value)
        {
            var argsList = args.ArgsRemove(parameter, 2).ToList();
            string tmp = argsList.FirstOrDefault(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            if (tmp == null)
            {
                argsList.Add(parameter);
                argsList.Add(value);
            }
            else
            {
                int index = argsList.IndexOf(tmp);
                if (index == argsList.Count - 1)
                    argsList.Add(value);
                else
                    argsList[index + 1] = value;
            }
            return argsList.ToArray();
        }


        public static bool ArgsExists(this string[] args, string parameter)
        {
            var argsList = args.ToList();
            string tmp = argsList.FirstOrDefault(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            return tmp != null;
        }

        public static string[] ArgsRemove(this string[] args, string parameter)
        {
            var argsList = args.ToList();
            argsList.RemoveAll(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            return argsList.ToArray();
        }

        public static string[] ArgsRemove(this string[] args, string parameter, int count)
        {
            var argsList = args.ToList();
            string tmp = argsList.FirstOrDefault(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            if (tmp == null || count <= 0)
                goto ReturnAndExit;

            while (true)
            {
                int index = argsList.IndexOf(tmp);
                if (index == -1)
                    break;

                for (int i = 0; i < count; i++)
                {
                    if (argsList.Count > index)
                        argsList.RemoveAt(index);
                }
            }

        ReturnAndExit:
            return argsList.ToArray();
        }

        public static string ArgsGetValue(this string[] args, string parameter)
        {
            var argsList = args.ToList();
            string tmp = argsList.FirstOrDefault(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            if (tmp == null)
                return string.Empty;

            int index = argsList.IndexOf(tmp);
            if (index == argsList.Count - 1)
                return string.Empty;

            return argsList[index + 1];
        }

        public static string[] ArgsGetValues(this string[] args, string parameter, int count)
        {
            List<string> ret = new List<string>();
            var argsList = args.ToList();
            string tmp = argsList.FirstOrDefault(p => p.Equals(parameter, StringComparison.OrdinalIgnoreCase));
            if (tmp == null || count <= 0)
                goto ReturnAndExit;

            int index = argsList.IndexOf(tmp);
            if (index == argsList.Count - 1)
                goto ReturnAndExit;

            index++;
            for (int i = 0; i < count; i++)
            {
                if (argsList.Count > index + i)
                    ret.Add(argsList[index + i]);
            }

        ReturnAndExit:
            return ret.ToArray();
        }
         
    }
}
