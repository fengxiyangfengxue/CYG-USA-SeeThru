using System;
using System.Collections.Generic;
using System.Linq;

namespace Test._Definitions
{

    public class StationLimits
    {
        public StationLimits()
        {
            LimitDict = new Dictionary<string, ItemLimit>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, ItemLimit> LimitDict { get; set; }



        public ItemLimit GetLimit(string limitName)
        {
            if (!string.IsNullOrEmpty(limitName) && !LimitDict.ContainsKey(limitName))
            {
                throw new Exception("limitName " + limitName + " not found!");
            }

            var limit = LimitDict[string.IsNullOrEmpty(limitName) ? "NO_Limit" : limitName];

            if (!limit.IsEnabled)
                limit = LimitDict["NO_Limit"];

            return limit;
        }

        public string ToLog()
        {
            string log = String.Empty;
            LimitDict.ToList().ForEach(l =>
            {
                log = log + l.Key + " : " + l.Value.ToLog() + Environment.NewLine;
            });

            return log;
        }

        public string GetHashedString()
        {
            var limits = LimitDict.Select(a =>
            {
                string str = a.Key;
                if (a.Value.LCL.HasValue)
                {
                    str += a.Value.LCL.Value;
                }
                if (a.Value.UCL.HasValue)
                {
                    str += a.Value.UCL.Value;
                }
                if (!string.IsNullOrEmpty(a.Value.CheckString))
                {
                    str += a.Value.CheckString;
                }
                if (!string.IsNullOrEmpty(a.Value.Unit))
                {
                    str += a.Value.Unit;
                }
                return str;
            });
            return string.Join("", limits);
        }
    }

}
