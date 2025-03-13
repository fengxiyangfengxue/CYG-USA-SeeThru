using System;
using System.Collections.Generic;
using System.IO;
using Test._ScriptExtensions;
using Test.ScriptSettings;
using Test._StartupConfigs;
using UserHelpers.Helpers;

namespace Test.Definition
{
    public class TestConfig
    { 
        public TestConfig(ITestProject project)
        {
            Project = project;
            AuditSNList = new List<string>();
            CM = "Goertek";
            Product = "Barista";
            TestMode = Test_Mode.PRIME;

            string testmode = Project.Args.ArgsGetValue("-testmode"); 
            if (Enum.TryParse(testmode, true, out Test_Mode mode))
            {
                TestMode = mode;
            }  
        }

        public void LoadConfig(CommonSetting commonSetting, MESSetting mesSetting, BuildPhaseSetting buildSetting)
        {
            this.OperatorID = mesSetting.UserName;
            this.AuditSNList = commonSetting.AuditSN.SplitToList(",");
        }

        public List<string> AuditSNList { get; set; }
        public ITestProject Project { get; set; }
        public string CM { get; set; }
        public string OperatorID { get; set; }
        public string Product { get; set; }
        public Test_Mode TestMode { get; set; }
        public IStartupConfig StartupConfig { get; set; }
    }
}