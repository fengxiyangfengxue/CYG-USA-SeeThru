using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test._Definitions;
using Test._ScriptHelpers;
using Test.StationsScripts.FATP_SuperCal;
using UserHelpers.Helpers;
using Test._App;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetaHelpers.ScriptHelpers;
using Test.ModbusTCP;
using Test._ScriptExtensions;
using System.Diagnostics;
using System.Threading;
using NModbus;
using System.Management;
using Test.Modules.SerialMotion;

namespace Test
{
    public partial class MainClass
    {

        /// <summary>
        /// 倍福PLC使大小轴回原点，并且检测回原点的情况。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="precision">精准度范围</param>
        /// <param name="delay">延时时间</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// 


        
        public int BfMotionHome(ITestItem item, double precision, int delay = 0, int timeout = 100000)
        {
            bool result = false;
            try
            {
  
                item.AddLog($"{BfPLC.ToString()}");
                BfPLC.Home(precision, delay, timeout);
                
                result = true;
                item.AddLog($"使大小轴回零点，并且检测回零状态为：{result}");

            }
            catch (Exception ex)
            {
                item.AddLog($"使倍福大小轴回零点失败，报错信息为：{ex}");
            }

            ReturnAndExit:
            ResultData resultData =
                new ResultData(item.Title, result?"":CreateErrorCode(item.Title).Name, result ? "PASS" : "FAIL");
            AddResult(item, resultData);
            return result ? 0 : 1;

        }


        
        public int motion_ab_1p(ITestItem item, string motionName,int timeout = 100000, bool isCheck = true, bool waitAnyway = false)
        {
            bool result = false;
            try
            {
                //MotionPath motionPath = new MotionPath();
                var listDic = _Context.Motion_Path.GetData(motionName);

                BfPLC.MoveAb(new List<MovingData> { listDic.First() }, 0.02, timeout, isCheck, waitAnyway); 
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"将电机移动到{motionName}的第一个点位出错 ： {e}");
               
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public int motion_ab(ITestItem item, string motionName,double precision =0.2, int timeout = 100000, bool isCheck = true, bool waitAnyway = false)
        {
            bool result = false;
            try
            {
                //MotionPath motionPath = new MotionPath();

                BfPLC.MoveAb(_Context.Motion_Path.GetData(motionName), precision, timeout, isCheck, waitAnyway);
                result = true;
            }
            catch (Exception e)
            {
                item.AddLog($"将电机移动到{motionName}点位出错 ： {e}");
               
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }

        public int MotionClose(ITestItem item)
        {
            bool result = false;
            try
            {
                BfPLC.CloseBf();
                result = true;
            }
            catch (Exception re)
            {

                item.AddLog($"MotionClose-->error {re}");
            }
            ReturnAndExit:
            ResultData data = new ResultData(item.Title, result ? "" : CreateErrorCode(item.Title).Name, result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;

        }
        
    }
}
