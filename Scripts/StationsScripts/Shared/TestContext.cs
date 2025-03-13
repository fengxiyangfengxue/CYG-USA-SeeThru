using GTKWebServices;
using MetaHelpers.ScriptHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test._App;
using Test._ScriptHelpers;
using Test.Definition;
using Test.ModbusTCP;
using Test.StationsScripts.Shared;
using Test.Modules.SerialMotion;
using Test.Modules.motion_control;
using System.Diagnostics;

namespace Test.StationsScripts.Shared
{

    public class TestContext
    {

        string _adbSN = string.Empty;

        public TestContext()
        {
            Reset();

            //set once only
            IsMesLoggedIn = false;
            IsMESCheckedLineAndStation = false;
            TempSerialNumber = string.Empty;
            OperatorID = string.Empty;
            MESClient = null;
            PLCClient = null;
            PLCClientR = null;
            PLCClientData = null;
        }


        public string ADBSN
        {
            get => _adbSN;
            set
            {
                _adbSN = value;
                ADBCaller.ADBSerialNumber = _adbSN;
            }
        }
         
        public static Test_Mode SelectedTestMode { get; set; } = Test_Mode.IDLE;
        public string ADBComPort { get; set; }
        public string BootloaderSN { get; set; }
        public string TempSerialNumber { get; set; }  //do not clear it 
        public string OperatorID { get; set; }
        public int TestCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public List<string> Locations { get; set; }
        public string DUT_Config { get; set; }
        public string SlotId { get; set; }
        public bool IsAudit { get; set; }
        public bool IsAuditCheckFail { get; set; }
        public bool IsAuditPass { get; set; }
        public bool IsAutoLoss { get; set; }
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public Script_Mode ScriptMode { get; set; }
        public string TSRID { get; set; }

        public string SlotFolder { get; set; }
        public string TmpFolder { get; set; }
        public string BackupFolder { get; set; }

        public GTKTestInterface MESClient { get; set; } //do not clear it 
        public bool IsMESCheckedLineAndStation { get; set; }  //do not clear it
        public bool IsMesLoggedIn { get; set; } //do not clear it
        public bool IsCheckRoutePass { get; set; }

        public ADBCallerHelper ADBCaller { get; set; } = new ADBCallerHelper();

        public ResultData FirstFailData { get; set; }
        public List<ResultData> AllFailData { get; set; } 

        public List<string> QDF_ZipFiles { get; set; }
        public ConcurrentDictionary<string, object> Variables { get; set; } = new ConcurrentDictionary<string, object>();


        public ModbusTcpClient PLCClient { get; set; }      //do not clear it 
        public ModbusTcpClient PLCClientR { get; set; }     //do not clear it 
        public ModbusTcpClient PLCClientData { get; set; }  //do not clear it 

        public HardwareControl MotionCotrol { get; set; }   //do not clear it 
        

        public DateTime SeeThruStartTime { get; set; }

        //public Process SeeThruExtCamProce { get; set; }

        public MotionPath Motion_Path { get; set; }

        public void Reset()
        {
            Variables.Clear();
            ADBSN = string.Empty;
            ADBComPort = string.Empty;
            ScriptMode = Script_Mode.Test;
            Locations = new List<string>();
            IsCheckRoutePass = false;
            FirstFailData = new ResultData();
            AllFailData = new List<ResultData>();
            QDF_ZipFiles = new List<string>();
            ADBCaller = new ADBCallerHelper(); 
        }

        public void ClearUp()
        {
            TempSerialNumber = string.Empty;
        }
    }
}
