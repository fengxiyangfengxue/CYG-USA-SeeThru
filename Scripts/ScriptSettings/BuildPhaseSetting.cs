using System.Xml.Serialization;
using Test.Definition;

namespace Test.ScriptSettings
{
    public class BuildPhaseSetting
    {
        public BuildPhaseSetting()
        {
            BuildPhase = "MP";
            WorkflowComponent = WorkflowEnum.Headset;
            FactoryId = "wef102";
            StationSequencing = "1"; 
            Version = "V1.0";
            Stage = "EVT1.0"; 
            Desc = "config information";
        }

        public string BuildPhase { get; set; } 

        public WorkflowEnum WorkflowComponent { get; set; } 

        public string FactoryId{ get; set; } 

        public string StationSequencing { get; set; } 

        public AssemblyEnum AssemblyPhase { get; set; } = AssemblyEnum.FATP;

        public string Version { get; set; }

        public string Stage { get; set; }
        
        [XmlAttribute("description")]
        public string Desc { get; set; } 
    }
}
