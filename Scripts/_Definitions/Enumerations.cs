
using System.ComponentModel;

namespace Test.Definition
{

    public enum TestTriggerMode
    {
        //单独/并行
        ParallelIndividually,
        /// <summary>
        /// 由第一个触发其他拼版执行(SMT_SWDL)
        /// </summary>
        ParallelFixtureReady,
        ParallelFirstOneReady,
        /// <summary>
        /// 顺序执行
        /// </summary>
        Sequential,
    }

    public enum FACTORY_TYPE
    {
        GTK,
        HQ
    }

    public enum LINE_TYPE
    {
        NO_NAME,
        SMT,
        FATP
    }

    public enum TEST_STATION
    {
        NONE,
        ANY_STATION,
        PCBA_RF,
        Example,
        FATP_Audio,
        FATP_Camera,
        FATP_Button,
        FATP_SuperCal,
        FATP_SuperCal_StageCal,
        FATP_SeeThru,
    }

    public enum Script_Mode
    {
        Test,
        AutoLoss,
        Audit
    }

    public enum AssemblyEnum
    {
        FATP,
        SMT
    }

    public enum WorkflowEnum
    {
        Headset = 0,
        Controller = 1,
        CR = 2
    }

    public enum Test_Mode
    {
        IDLE = 0,
        PRIME = 1,
        FA = 2,
        REWORK = 3,
        GRR = 4,
        REL = 5
    }
}
