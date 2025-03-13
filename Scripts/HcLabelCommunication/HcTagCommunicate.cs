using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagAccessCS;
//using InoTagComCSharp;
using System.Collections.Concurrent;
using System.Threading;

namespace Test.HcLabelCommunication
{
    class HCTagCommunicate : IDisposable
    {
        public TagAccessClass plcTag = null;
        public string plcIp = string.Empty;
        public IntPtr plcHandle = IntPtr.Zero;
        public ConcurrentDictionary<string, IntPtr> HandleDic = new ConcurrentDictionary<string, IntPtr>();
        public bool connectState = false;
        public TagAccessClass.TAResult TAResult = TagAccessClass.TAResult.ERR_NOERROR;

        //public HCTagCommunicate()
        //{
        //    plcTag = new TagAccessClass();

        //}

        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //释放托管资源 ：.NET运行时管理的资源，比如普通的C#对象。 *这些资源会由垃圾回收器（GC）自动清理
            }
            else
            {
                // 释放非托管资源： 如文件句柄，PLC句柄、数据库链接，（操作系统层面的资源）  *GC不会自动清理这些资源，需要手动清理
                if (plcTag != null && !HandleDic.IsEmpty)
                {
                    foreach (var key in HandleDic.Keys)
                    {
                        try
                        {
                            plcTag.ReleaseHandle(HandleDic[key]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"释放句柄[{key}]失败：{ex.Message}");
                        }
                    }
                    HandleDic.Clear();
                    plcTag = null;

                }
            }
        }

        public HCTagCommunicate(string ip)
        {
            plcIp = ip;
            plcTag = new TagAccessClass();
            ConnectPlc();
        }

        public void ConnectPlc()
        {
            try
            {
                var result = plcTag.Connect2PlcDevice(plcIp);
                connectState = result == TagAccessClass.TAResult.ERR_NOERROR;
                //if (result == TagAccessClass.TAResult.ERR_NOERROR) 
                //    connectState = true;
                //else
                //    connectState = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接PLC时出错-->  {ex}");
                connectState = false;
            }

        }

        public void DisConnectplc()
        {

            if (plcTag != null && connectState)
            {
                try
                {
                    plcTag.DisconnectFromPLC();
                    connectState = false;
                    Dispose(false);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"断开连接时发生错误：{ex.Message}");
                }

            }
            else
            {
                Console.WriteLine("PLC未连接或已断开，无需再次断开连接。");
            }

        }

        public bool CommState => plcTag != null && connectState;

        public object CommandRead(string readType, string tagName)
        {
            try
            {


                //  定义一个枚举类型，注明 需要的data数据，当给出的参数不符合其中的类型时，进行报错提示。
                if (!Enum.TryParse(readType, true, out TagTypeEnum tagType))
                {
                    throw new Exception($"type只能填写BOOLEAN,INT,DINT,DOUBLE,REAL,STRING,BYTE,WSTRING,请检查是否出错！！");
                }

                // 简化写法
                IntPtr handleName = GetOrCreateHandle(tagName);

                switch (readType)
                {

                    case "boolean":

                        //plcHandle = plcTag.CreateTagHandle(tagName, out TagAccessClass.TAResult res);
                        //if (!HandleDic.TryGetValue(tagName,out plcHandle))
                        //{
                        //    plcHandle = plcTag.CreateTagHandle(tagName, out TAResult);
                        //    HandleDic[tagName] = plcHandle;
                        //}
                        //IntPtr handleName = HandleDic[tagName];


                        var value_bool = false;
                        value_bool = (bool)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_BOOL, out TAResult);
                        Console.WriteLine($"TAResult-->{TAResult}");
                        return value_bool;

                    case "string":

                        string str;
                        return str = (string)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_STRING, out TAResult);

                    case "int":
                        Int16 valu = 0;
                        return valu = (Int16)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_INT, out TAResult);

                    case "dint":

                        Int32 val_Int32 = 0;
                        return val_Int32 = (Int32)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_DINT, out TAResult);

                    case "double":
                        double double_value = 0.0;
                        return double_value = (double)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_LREAL, out TAResult);

                    case "real":
                        
                        
                        float real_value = (float)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_REAL, out TAResult);
                        
                        return real_value;

                    case "byte":

                        var val_Byte = 0;
                        return val_Byte = (byte)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_BYTE, out TAResult);

                    default:
                        throw new ArgumentException("给出的类型没有合适的匹配项，请检查！");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"读取出错-->{ex}");
            }

        }

        /// <summary>
        /// 在限定时间内读取值，超时为fasle
        /// </summary>
        /// <param name="readType">读取的类型</param>
        /// <param name="tagName">标签名</param>
        /// <param name="tagetValue">想要读取到的值</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="waitTime">每次刷新间隔的时间默认为100ms</param>
        /// <returns>bool</returns>
        public bool CommandReadWait(string readType, string tagName, object tagetValue, int timeout, int waitTime = 100)
        {
            try
            {
                if (!Enum.TryParse(readType, true, out TagTypeEnum tagType))
                {
                    throw new Exception($"type只能填写BOOLEAN,INT,DINT,DOUBLE,REAL,STRING,BYTE,WSTRING,请检查是否出错！！");
                }

                IntPtr handleName = GetOrCreateHandle(tagName);
                Console.WriteLine($"handleName-->{handleName}");

                DateTime startTime = DateTime.Now;
                switch (readType)
                {
                    case "boolean":
                        while ((DateTime.Now - startTime).TotalSeconds < timeout)
                        {
                            bool res = (bool)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_BOOL, out TAResult);
                            if (res == Convert.ToBoolean(tagetValue))
                            {
                                return true;
                            }

                            Thread.Sleep(waitTime);
                        }
                        return false;

                    case "int":
                        while ((DateTime.Now - startTime).TotalSeconds < timeout)
                        {
                            Int16 res = (Int16)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_BOOL, out TAResult);
                            if (res == Convert.ToInt16(tagetValue))
                            {
                                return true;
                            }

                            Thread.Sleep(waitTime);
                        }
                        return false;

                    case "dint":
                        while ((DateTime.Now - startTime).TotalSeconds < timeout)
                        {
                            Int32 res = (Int32)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_BOOL, out TAResult);
                            if (res == Convert.ToInt32(tagetValue))
                            {
                                return true;
                            }

                            Thread.Sleep(waitTime);
                        }
                        return false;

                    case "string":
                        while ((DateTime.Now - startTime).TotalSeconds < timeout)
                        {
                            string res = (string)plcTag.ReadTag(handleName, TagAccessClass.TagTypeClass.TC_BOOL, out TAResult);
                            if (res == Convert.ToString(tagetValue))
                            {
                                return true;
                            }

                            Thread.Sleep(waitTime);

                        }
                        return false;

                    default:
                        throw new ArgumentException("给出的类型没有合适的匹配项，请检查！");
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"等待读取到固定的值时出错：{ex}");
            }
        }

        /// <summary>
        /// 根据标签给plc写入值
        /// </summary>
        /// <param name="writeType"> 写入的值的类型</param>
        /// <param name="tagName"> 标签名</param>
        /// <param name="obj">要写入的值</param>
        /// <returns>写入的结果</returns>
        public object CommandWrite(string writeType, string tagName, object obj)
        {
            try
            {


                //  定义一个枚举类型，注明 需要的data数据，当给出的参数不符合其中的类型时，进行报错提示。
                if (!Enum.TryParse(writeType, true, out TagTypeEnum tagType))
                {
                    throw new Exception($"type只能填写BOOLEAN,INT,DINT,DOUBLE,REAL,STRING,BYTE,WSTRING,请检查是否出错！！");
                }

                IntPtr handleName = GetOrCreateHandle(tagName);

                Console.WriteLine($"handleName-->{handleName}");
                bool writeResult = false;
                switch (writeType)
                {
                    case "boolean":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_BOOL, out TAResult);
                        Console.WriteLine($"TAResult-->{TAResult}");
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "int":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_INT, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "dint":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_DINT, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "double":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_LREAL, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "real":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_REAL, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "string":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_STRING, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "wstring":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_WSTRING, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;

                    case "byte":
                        plcTag.WriteTag(handleName, obj, TagAccessClass.TagTypeClass.TC_BYTE, out TAResult);
                        writeResult = TagAccessClass.TAResult.ERR_NOERROR == TAResult;
                        break;




                    default:
                        throw new ArgumentException("给出的类型没有合适的匹配项，请检查！");

                }
                return writeResult;
            }
            catch (Exception ex)
            {
                throw new Exception($"写入出错-->{ex}");
            }
        }

        private IntPtr GetOrCreateHandle(string tagName)
        {
            if (plcIp == null || !connectState)
            {
                throw new InvalidOperationException("PLC未链接或者IP地址为空");
            }

            //return plcTag.CreateTagHandle(tagName, out TAResult);
            return HandleDic.GetOrAdd(tagName, key => plcTag.CreateTagHandle(tagName, out TAResult));


        }

        ~HCTagCommunicate()
        {
            //实现了IDisposable接口的析构函数
            Dispose(false);


            //if (plcTag != null && !HandleDic.IsEmpty)
            //{

            //    foreach (var key in HandleDic.Keys)
            //    {


            //        try
            //        {
            //            plcTag.ReleaseHandle(HandleDic[key]);
            //        }
            //        catch (Exception ex)
            //        {
            //            // 记录释放失败的异常信息，便于后续排查
            //            Console.WriteLine($"释放句柄[{key}]失败：{ex.Message}");
            //        }
            //    }
            //    HandleDic.Clear();

            //}
        }
    }


    public enum TagTypeEnum
    {
        BOOLEAN,
        INT,
        DINT,
        DOUBLE,
        REAL,
        STRING,
        BYTE,
        WSTRING,

    }

}


