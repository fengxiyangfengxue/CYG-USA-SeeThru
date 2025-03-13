
using LitJson;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Test.Definition;
using Test._Definitions;
using Test._StartupConfigs;

namespace Test
{
    public partial class MainClass
    {
        StationLimits _Limits = null;
        TestConfig _Config = null;
        void Load_Limits()
        {
            _Limits = new StationLimits();

            _Config.StartupConfig.LimitFileNames.ForEach(f =>
            {
                JsonData jd = JsonMapper.ToObject(File.ReadAllText(f));

                if (jd != null)
                {
                    jd.Keys.ToList().ForEach(k =>
                    {
                        var json = jd[k].ToJson();
                        var d = JsonMapper.ToObject<ItemLimit>(json);
                        _Limits.LimitDict[k] = d;
                    });
                }
            });

            _Limits.LimitDict["NO_Limit"] = new ItemLimit() { };
        }

        TestConfig LoadTestConfig()
        {
            //Test.StartupConfigs
            string startupClassFullName = typeof(IStartupConfig).Namespace + "." + typeof(IStartupConfig).Name.Substring(1) + "_" + Project.ProjectName.Trim();

            Assembly assembly = Assembly.GetAssembly(typeof(IStartupConfig));
            //Assembly assembly = Assembly.Load("Test.StationConfigs");

            if (assembly.GetType(startupClassFullName) == null)
                throw new Exception("type " + startupClassFullName + " not found!");

            TestConfig config = new TestConfig(Project);
            config.StartupConfig = (IStartupConfig)assembly.CreateInstance(startupClassFullName, false, BindingFlags.Default, null, new object[] { config }, null, null);
             
            return config;
        }

    }
}
