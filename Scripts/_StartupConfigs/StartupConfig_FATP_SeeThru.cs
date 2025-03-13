using System.Collections.Generic;
using Test.Definition;

namespace Test._StartupConfigs
{
    public class StartupConfig_FATP_SeeThru: IStartupConfig
    {
        public StartupConfig_FATP_SeeThru(TestConfig config)
        {
            Factory = FACTORY_TYPE.GTK;
            LineType = LINE_TYPE.FATP;
            Station = TEST_STATION.FATP_SeeThru;
            IsPrintLimits = false;
            LimitFileNames = new List<string>()
            {
                $"./Limits/{Station}.json"
            };
            DllResolverFolder = @"";
            config.StartupConfig = this;
            TriggerMode = TestTriggerMode.ParallelIndividually;
            CustomeConfig = new StartupCustomConfig();
        }
        public FACTORY_TYPE Factory { get; set; }
        public LINE_TYPE LineType { get; set; }
        public TEST_STATION Station { get; set; }
        public TestTriggerMode TriggerMode { get; set; }
        public bool IsPrintLimits { get; set; }
        public List<string> LimitFileNames { get; set; }
        public string DllResolverFolder { get; set; }
        public StartupCustomConfig CustomeConfig { get; set; }
    }
}
