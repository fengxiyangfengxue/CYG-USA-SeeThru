using System.Collections.Generic;
using Test.Definition;

namespace Test._StartupConfigs
{
    public interface IStartupConfig
    {
        FACTORY_TYPE Factory { get; set; }
        LINE_TYPE LineType { get; set; }
        TEST_STATION Station { get; set; }
        TestTriggerMode TriggerMode { get; set; }
        bool IsPrintLimits { get; set; }
        List<string> LimitFileNames { get; set; }
        string DllResolverFolder { get; set; }
        StartupCustomConfig CustomeConfig { get; set; }
    }
}
