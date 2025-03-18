using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace CodeToObject 
{ 
    public class RunArgs
    {
        public RunArgs()
        {
            ArgValues = new List<Tuple<Type, object>>();
        }
        public Guid ItemGUID { get; set; }
        public List<Tuple<Type, object>> ArgValues { get; set; }
    }

    public class CodeToObjectClass
    {
        List<RunArgs> RunArgsList = new List<RunArgs>();

        public List<Tuple<Type, object>> GetObject(Guid guid)
        {
            List<Tuple<Type, object>> ret = new List<Tuple<Type, object>>();
            RunArgs arg = RunArgsList.FirstOrDefault(a => a.ItemGUID.Equals(guid));
            if(arg != null)
            {
                arg.ArgValues.ForEach(v => ret.Add(v));
            }
            return ret;
        }

        Type GetArgType<T>(T obj) { return typeof(T); }

        public void CreateObjectList()
        {
            RunArgs arg = new RunArgs();
              
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("f3c0af4a-268b-4e8f-8b55-702edfd7fe9d");
            string _var_0_0 = "C:\\adb_tool\\platform-tools";
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_0_0), _var_0_0));
            RunArgsList.Add(arg);
  
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("1275af4c-5f9e-40d9-9581-a22fbdfd9387");
            int _var_1_0 = 7;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_1_0), _var_1_0));
            string _var_1_1 = "curl http://172.18.193.172:8088/parameterdata/NODUT/24/dstcalstation/camera";
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_1_1), _var_1_1));
            RunArgsList.Add(arg);
  
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("5147c08f-ae8f-42f2-941f-239ebdfd4a73");
            int _var_2_0 = 10000;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_2_0), _var_2_0));
            RunArgsList.Add(arg);
  
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("e41df7c3-b4c8-4016-ae0f-5f367e773da7");
            bool _var_3_0 = false;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_3_0), _var_3_0));
            bool _var_3_1 = false;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_3_1), _var_3_1));
            RunArgsList.Add(arg);
  
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("2d48108c-5355-44c6-9ce2-739de9a722a3");
            int _var_4_0 = 10000;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_4_0), _var_4_0));
            RunArgsList.Add(arg);
  
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("ee029248-f106-40e1-8b1e-2beef29f93b4");
            int _var_5_0 = 10000;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_5_0), _var_5_0));
            RunArgsList.Add(arg);
  
            arg = new RunArgs();
            arg.ItemGUID = Guid.Parse("6dbc27e5-66d2-4186-a7ef-b15eb8542662");
            bool _var_6_0 = false;
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_6_0), _var_6_0));
            List<string> _var_6_1 = {"channel1","channel2","channel3"};
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_6_1), _var_6_1));
            List<int> _var_6_2 = {2500,2500,2500};
            arg.ArgValues.Add(new Tuple<Type, object>(GetArgType(_var_6_2), _var_6_2));
            RunArgsList.Add(arg);
   
        }
    }
}   
