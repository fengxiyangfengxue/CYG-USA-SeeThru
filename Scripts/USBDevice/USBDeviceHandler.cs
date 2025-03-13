using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks; 
using UserHelpers.Helpers;

namespace Test.USBDevice
{
    class USBDeviceHandler
    {

        //internal static Guid GUID_DEVINTERFACE_NET = new Guid("36fc9e60-c465-11cf-8056-444553540000".ToUpper());

        //ports
        static Guid GUID_DEVINTERFACE_PORTS = new Guid("4d36e978-e325-11ce-bfc1-08002be10318".ToUpper());
        static Guid GUID_BootLoader = new Guid("3f966bd9-fa04-4ec5-991c-d326973b5128".ToUpper());

        public static string ADB_ComPort_DeviceName = "Qualcomm HS-USB Android DIAG 901D";
        public static string ADB_9008_ComPort_DeviceName = "Qualcomm HS-USB QDLoader 9008";
        public static string BootLoader_USB_DeviceName = "Oculus Bootloader Interface";

        public static string ADB_Get_SerialNumber(ITestItem item, string name, string locationInfo)
        {
            string sn = string.Empty;
            Guid classGuid = GUID_DEVINTERFACE_PORTS;
            IntPtr hwndParent = IntPtr.Zero;
            Int32 flags = USBDeviceManagement.DIGCF_ALLCLASSES | USBDeviceManagement.DIGCF_PRESENT;
            IntPtr pDevInfoSet = IntPtr.Zero;
            IntPtr pNewDevInfoSet = IntPtr.Zero;
            try
            {
                pNewDevInfoSet = USBDeviceManagement.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, hwndParent, flags);//, pDevInfoSet, strMachineName, IntPtr.Zero);
                if (pNewDevInfoSet == IntPtr.Zero)
                {
                    item.AddLog("Failed to get device information list");
                    return "";
                }

                Int32 iRet;
                Int32 iMemberIndex = 0;
                do
                {
                    USBDeviceManagement.SP_DEVINFO_DATA devInfoData = new USBDeviceManagement.SP_DEVINFO_DATA();
                    devInfoData.ClassGuid = Guid.Empty;
                    devInfoData.DevInst = 0;
                    devInfoData.Reserved = UIntPtr.Zero;
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    iRet = USBDeviceManagement.SetupDiEnumDeviceInfo(pNewDevInfoSet, iMemberIndex, ref devInfoData);
                    if (iRet == 0)
                    {
                        Int32 iLastError = USBDeviceManagement.GetLastError();
                        if (iLastError == USBDeviceManagement.ERROR_NO_MORE_FILES)
                        {
                            //item.AddLog("No more devices in list");
                            break;
                        }
                        else
                        {
                            iMemberIndex++;
                            continue;
                        }
                    }

                    string friendlyName = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_FRIENDLYNAME);
                    string localtion = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_LOCATION_INFORMATION);
                    string instanceId = GetDeviceInstanceId(pNewDevInfoSet, devInfoData);
 
                    if (friendlyName.Contains(name) && localtion.Contains(locationInfo))
                    {
                        item.AddLog("SPDRP_FRIENDLYNAME = " + friendlyName);
                        item.AddLog("InstanceId = " + instanceId);
                        item.AddLog("SPDRP_LOCATION_INFORMATION = " + localtion);

                        string hwid = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_HARDWAREID);
                        item.AddLog("SPDRP_HARDWAREID = " + hwid);

                        string compatibleId = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_COMPATIBLEIDS);
                        item.AddLog("SPDRP_COMPATIBLEIDS = " + compatibleId);

                        string desc = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC);
                        item.AddLog("SPDRP_DEVICEDESC = " + desc);

                        string parentId = string.Empty;
                        if (GetParentId(item, instanceId, out parentId))
                        {
                            item.AddLog("parentId = " + parentId);

                            if (parentId.IndexOf("\\") >= 0)
                                parentId = parentId.Substring(parentId.LastIndexOf('\\') + 1).Trim();
                            if (parentId.IndexOf("&") >= 0)
                                parentId = parentId.Substring(parentId.LastIndexOf('&') + 1).Trim();

                            sn = parentId;

                            item.AddLog("SN found = " + sn);
                            break;
                        }
                        else
                        {
                            item.AddLog("Get parentId failed!");
                        }
                        break;
                    }
                     
                    iMemberIndex++;
                } while (true);
            }
            finally
            {
                USBDeviceManagement.SetupDiDestroyDeviceInfoList(pNewDevInfoSet);
            }

            return sn.ToLower();
        }

        public static string ADB_Get_ComPort(ITestItem item, string name, string locationInfo)
        {
            string comPort = string.Empty;
            Guid classGuid = GUID_DEVINTERFACE_PORTS;
            IntPtr hwndParent = IntPtr.Zero;
            Int32 flags = USBDeviceManagement.DIGCF_ALLCLASSES | USBDeviceManagement.DIGCF_PRESENT;
            IntPtr pDevInfoSet = IntPtr.Zero;
            IntPtr pNewDevInfoSet = IntPtr.Zero;
            try
            {
                pNewDevInfoSet = USBDeviceManagement.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, hwndParent, flags);//, pDevInfoSet, strMachineName, IntPtr.Zero);
                if (pNewDevInfoSet == IntPtr.Zero)
                {
                    item.AddLog("Failed to get device information list");
                    return "";
                }

                Int32 iRet;
                Int32 iMemberIndex = 0;
                do
                {
                    USBDeviceManagement.SP_DEVINFO_DATA devInfoData = new USBDeviceManagement.SP_DEVINFO_DATA();
                    devInfoData.ClassGuid = Guid.Empty;
                    devInfoData.DevInst = 0;
                    devInfoData.Reserved = UIntPtr.Zero;
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    iRet = USBDeviceManagement.SetupDiEnumDeviceInfo(pNewDevInfoSet, iMemberIndex, ref devInfoData);
                    if (iRet == 0)
                    {
                        Int32 iLastError = USBDeviceManagement.GetLastError();
                        if (iLastError == USBDeviceManagement.ERROR_NO_MORE_FILES)
                        {
                            //item.AddLog("No more devices in list");
                            break;
                        }
                        else
                        {
                            iMemberIndex++;
                            continue;
                        }
                    }

                    string friendlyName = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_FRIENDLYNAME);
                    string localtion = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_LOCATION_INFORMATION);
                    string instanceId = GetDeviceInstanceId(pNewDevInfoSet, devInfoData);

                    if (friendlyName.Contains(name) && localtion.Contains(locationInfo) && friendlyName.Contains("(COM"))
                    {
                        item.AddLog("SPDRP_FRIENDLYNAME = " + friendlyName);
                        item.AddLog("InstanceId = " + instanceId);
                        item.AddLog("SPDRP_LOCATION_INFORMATION = " + localtion);

                        friendlyName = friendlyName.Substring(friendlyName.LastIndexOf("(") + 1);
                        if (friendlyName.IndexOf(")") > 0)
                        {
                            comPort = friendlyName.Substring(0, friendlyName.IndexOf(")"));
                            item.AddLog("ComPort found = " + comPort);
                            break;
                        }
                    }

                    iMemberIndex++;
                } while (true);
            }
            finally
            {
                USBDeviceManagement.SetupDiDestroyDeviceInfoList(pNewDevInfoSet);
            }

            return comPort.ToUpper();
        }

        public static string BootLoader_Get_SerialNumber(ITestItem item, string name, string locationInfo)
        {
            string sn = string.Empty;
            Guid classGuid = GUID_BootLoader;
            IntPtr hwndParent = IntPtr.Zero;
            Int32 flags = USBDeviceManagement.DIGCF_ALLCLASSES | USBDeviceManagement.DIGCF_PRESENT;
            IntPtr pDevInfoSet = IntPtr.Zero;
            IntPtr pNewDevInfoSet = IntPtr.Zero;
            try
            {
                pNewDevInfoSet = USBDeviceManagement.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, hwndParent, flags);//, pDevInfoSet, strMachineName, IntPtr.Zero);
                if (pNewDevInfoSet == IntPtr.Zero)
                {
                    item.AddLog("Failed to get device information list");
                    return "";
                }

                Int32 iRet;
                Int32 iMemberIndex = 0;
                do
                {
                    USBDeviceManagement.SP_DEVINFO_DATA devInfoData = new USBDeviceManagement.SP_DEVINFO_DATA();
                    devInfoData.ClassGuid = Guid.Empty;
                    devInfoData.DevInst = 0;
                    devInfoData.Reserved = UIntPtr.Zero;
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    iRet = USBDeviceManagement.SetupDiEnumDeviceInfo(pNewDevInfoSet, iMemberIndex, ref devInfoData);
                    if (iRet == 0)
                    {
                        Int32 iLastError = USBDeviceManagement.GetLastError();
                        if (iLastError == USBDeviceManagement.ERROR_NO_MORE_FILES)
                        {
                            //item.AddLog("No more devices in list");
                            break;
                        }
                        else
                        {
                            iMemberIndex++;
                            continue;
                        }
                    }

                    string descName = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC);
                    string localtion = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_LOCATION_INFORMATION);
                    string instanceId = GetDeviceInstanceId(pNewDevInfoSet, devInfoData);

                    if (descName.Contains(name) && localtion.Contains(locationInfo))
                    {
                        item.AddLog("SPDRP_DEVICEDESC = " + descName);
                        item.AddLog("InstanceId = " + instanceId);
                        item.AddLog("SPDRP_LOCATION_INFORMATION = " + localtion);

                        string hwid = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_HARDWAREID);
                        item.AddLog("SPDRP_HARDWAREID = " + hwid);

                        string compatibleId = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_COMPATIBLEIDS);
                        item.AddLog("SPDRP_COMPATIBLEIDS = " + compatibleId);

                        string desc = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC);
                        item.AddLog("SPDRP_DEVICEDESC = " + desc);
                         
                        if (instanceId.IndexOf("\\") >= 0)
                            instanceId = instanceId.Substring(instanceId.LastIndexOf('\\') + 1).Trim();
                        if (instanceId.IndexOf("&") >= 0)
                            instanceId = instanceId.Substring(instanceId.LastIndexOf('&') + 1).Trim();

                        sn = instanceId;

                        item.AddLog("SN found = " + sn);
                        break;

                    }


                    iMemberIndex++;
                } while (true);
            }
            finally
            {
                USBDeviceManagement.SetupDiDestroyDeviceInfoList(pNewDevInfoSet);
            }

            return sn.ToLower();
        }

        public static bool BootLoader_DetectDevice(ITestItem item, string name, string locationInfo)
        {
            bool isFound = false;
            Guid classGuid = GUID_BootLoader;
            IntPtr hwndParent = IntPtr.Zero;
            Int32 flags = USBDeviceManagement.DIGCF_ALLCLASSES | USBDeviceManagement.DIGCF_PRESENT;
            IntPtr pDevInfoSet = IntPtr.Zero;
            IntPtr pNewDevInfoSet = IntPtr.Zero;
            try
            {
                pNewDevInfoSet = USBDeviceManagement.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, hwndParent, flags);//, pDevInfoSet, strMachineName, IntPtr.Zero);
                if (pNewDevInfoSet == IntPtr.Zero)
                {
                    item.AddLog("Failed to get device information list");
                    goto ReturnAndExit;
                }

                Int32 iRet;
                Int32 iMemberIndex = 0;
                do
                {
                    USBDeviceManagement.SP_DEVINFO_DATA devInfoData = new USBDeviceManagement.SP_DEVINFO_DATA();
                    devInfoData.ClassGuid = Guid.Empty;
                    devInfoData.DevInst = 0;
                    devInfoData.Reserved = UIntPtr.Zero;
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    iRet = USBDeviceManagement.SetupDiEnumDeviceInfo(pNewDevInfoSet, iMemberIndex, ref devInfoData);
                    if (iRet == 0)
                    {
                        Int32 iLastError = USBDeviceManagement.GetLastError();
                        if (iLastError == USBDeviceManagement.ERROR_NO_MORE_FILES)
                        {
                            //item.AddLog("No more devices in list");
                            break;
                        }
                        else
                        {
                            iMemberIndex++;
                            continue;
                        }
                    }

                    string descName = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC);
                    string localtion = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_LOCATION_INFORMATION);
                    string instanceId = GetDeviceInstanceId(pNewDevInfoSet, devInfoData);

                    if (descName.Contains(name) && localtion.Contains(locationInfo))
                    {
                        item.AddLog("SPDRP_DEVICEDESC = " + descName);
                        item.AddLog("InstanceId = " + instanceId);
                        item.AddLog("SPDRP_LOCATION_INFORMATION = " + localtion);

                        isFound = true;
                        break; 
                    }

                    iMemberIndex++;
                } while (true);
            }
            finally
            {
                USBDeviceManagement.SetupDiDestroyDeviceInfoList(pNewDevInfoSet);
            }

        ReturnAndExit:
            item.AddLog(isFound ? "Device found!" : "Device nt found");
            return isFound;
        }
         
        public static bool ADB_DetectDevice(ITestItem item, string name, string locationInfo)
        {
            bool isFound = false;
            Guid classGuid = GUID_DEVINTERFACE_PORTS;
            IntPtr hwndParent = IntPtr.Zero;
            Int32 flags = USBDeviceManagement.DIGCF_ALLCLASSES | USBDeviceManagement.DIGCF_PRESENT;
            IntPtr pDevInfoSet = IntPtr.Zero;
            IntPtr pNewDevInfoSet = IntPtr.Zero;
            try
            {
                pNewDevInfoSet = USBDeviceManagement.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, hwndParent, flags);//, pDevInfoSet, strMachineName, IntPtr.Zero);
                if (pNewDevInfoSet == IntPtr.Zero)
                {
                    item.AddLog("Failed to get device information list");
                    goto ReturnAndExit;
                }

                Int32 iRet;
                Int32 iMemberIndex = 0;
                do
                {
                    USBDeviceManagement.SP_DEVINFO_DATA devInfoData = new USBDeviceManagement.SP_DEVINFO_DATA();
                    devInfoData.ClassGuid = Guid.Empty;
                    devInfoData.DevInst = 0;
                    devInfoData.Reserved = UIntPtr.Zero;
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    iRet = USBDeviceManagement.SetupDiEnumDeviceInfo(pNewDevInfoSet, iMemberIndex, ref devInfoData);
                    if (iRet == 0)
                    {
                        Int32 iLastError = USBDeviceManagement.GetLastError();
                        if (iLastError == USBDeviceManagement.ERROR_NO_MORE_FILES)
                        {
                            //item.AddLog("No more devices in list");
                            break;
                        }
                        else
                        {
                            iMemberIndex++;
                            continue;
                        }
                    }

                    string friendlyName = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_FRIENDLYNAME);
                    string localtion = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_LOCATION_INFORMATION);
                    string instanceId = GetDeviceInstanceId(pNewDevInfoSet, devInfoData);

                    if (friendlyName.Contains(name) && localtion.Contains(locationInfo) && friendlyName.Contains("(COM"))
                    {
                        item.AddLog("SPDRP_FRIENDLYNAME = " + friendlyName);
                        item.AddLog("InstanceId = " + instanceId);
                        item.AddLog("SPDRP_LOCATION_INFORMATION = " + localtion);

                        isFound = true;
                        break;

                    }

                    iMemberIndex++;
                } while (true);
            }
            finally
            {
                USBDeviceManagement.SetupDiDestroyDeviceInfoList(pNewDevInfoSet);
            }

        ReturnAndExit:
            item.AddLog(isFound ? "Device found!" : "Device nt found");
            return isFound;
        }

        public static void ADB_ListDevices(ITestItem item, string name)
        {
            Guid classGuid = GUID_DEVINTERFACE_PORTS;
            IntPtr hwndParent = IntPtr.Zero;
            Int32 flags = USBDeviceManagement.DIGCF_ALLCLASSES | USBDeviceManagement.DIGCF_PRESENT;
            IntPtr pDevInfoSet = IntPtr.Zero;
            IntPtr pNewDevInfoSet = IntPtr.Zero;
            try
            {
                pNewDevInfoSet = USBDeviceManagement.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, hwndParent, flags);
                if (pNewDevInfoSet == IntPtr.Zero)
                {
                    item.AddLog("Failed to get device information list");
                    return;
                }

                Int32 iRet;
                Int32 iMemberIndex = 0;
                do
                {
                    USBDeviceManagement.SP_DEVINFO_DATA devInfoData = new USBDeviceManagement.SP_DEVINFO_DATA();
                    devInfoData.ClassGuid = Guid.Empty;
                    devInfoData.DevInst = 0;
                    devInfoData.Reserved = UIntPtr.Zero;
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    iRet = USBDeviceManagement.SetupDiEnumDeviceInfo(pNewDevInfoSet, iMemberIndex, ref devInfoData);
                    if (iRet == 0)
                    {
                        Int32 iLastError = USBDeviceManagement.GetLastError();
                        if (iLastError == USBDeviceManagement.ERROR_NO_MORE_FILES)
                        {
                            //item.AddLog("No more devices in list");
                            break;
                        }
                        else
                        {
                            iMemberIndex++;
                            continue;
                        }
                    }

                    string friendlyName = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_FRIENDLYNAME);
                    string localtion = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_LOCATION_INFORMATION);
                    string instanceId = GetDeviceInstanceId(pNewDevInfoSet, devInfoData);
                    //item.AddLog("SPDRP_FRIENDLYNAME = " + friendlyName);

                    if (friendlyName.Contains(name))
                    {
                        item.AddLog("SPDRP_FRIENDLYNAME = " + friendlyName);
                        item.AddLog("InstanceId = " + instanceId);
                        item.AddLog("SPDRP_LOCATION_INFORMATION = " + localtion);

                        string hwid = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_HARDWAREID);
                        item.AddLog("SPDRP_HARDWAREID = " + hwid);

                        string compatibleId = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_COMPATIBLEIDS);
                        item.AddLog("SPDRP_COMPATIBLEIDS = " + compatibleId);

                        string desc = GetDevicePropertyString(pNewDevInfoSet, devInfoData, SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC);
                        item.AddLog("SPDRP_DEVICEDESC = " + desc);

                        string parentId = string.Empty;
                        if (GetParentId(item, instanceId, out parentId))
                        {
                            item.AddLog("parentId = " + parentId);

                            if (parentId.IndexOf("\\") >= 0)
                                parentId = parentId.Substring(parentId.LastIndexOf('\\') + 1).Trim();
                            if (parentId.IndexOf("&") >= 0)
                                parentId = parentId.Substring(parentId.LastIndexOf('&') + 1).Trim();

                            item.AddLog("SN = " + parentId);
                        }
                        else
                        {
                            item.AddLog("Get parentId failed!");
                        }
                    }

                    iMemberIndex++;
                } while (true);
            }
            finally
            {
                USBDeviceManagement.SetupDiDestroyDeviceInfoList(pNewDevInfoSet);
            }

        }

        static bool GetParentId(ITestItem item, string driver, out string resultDeviceID)
        {
            resultDeviceID = "";
            try
            {
                int ulFlags = 0;
                int pdnDevInst = 0;
                int pulLen = 0;
                if (USBDeviceManagement.CM_Locate_DevNodeA(ref pdnDevInst, driver, ulFlags) != 0)
                {
                    return false;
                }

                uint devInst;
                if (USBDeviceManagement.CM_Get_Parent(out devInst, (uint)pdnDevInst, ulFlags) != 0)
                {
                    return false;
                }
                if (USBDeviceManagement.CM_Get_Device_ID_Size(out pulLen, devInst, ulFlags) != 0)
                {
                    return false;
                }
                char[] buffer = new char[256];
                if (USBDeviceManagement.CM_Get_Device_ID(devInst, buffer, pulLen, 0) != 0)
                {
                    return false;
                }
                resultDeviceID = new string(buffer);
                return true;
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
                return false;
            }
        }

        static string GetDeviceInstanceId(IntPtr DeviceInfoSet, USBDeviceManagement.SP_DEVINFO_DATA DeviceInfoData)
        {
            StringBuilder strId = new StringBuilder(0);
            Int32 iRequiredSize = 0;
            Int32 iSize = 0;
            Int32 iRet = USBDeviceManagement.SetupDiGetDeviceInstanceId(DeviceInfoSet, ref DeviceInfoData, strId, iSize, ref iRequiredSize);
            strId = new StringBuilder(iRequiredSize);
            iSize = iRequiredSize;
            iRet = USBDeviceManagement.SetupDiGetDeviceInstanceId(DeviceInfoSet, ref DeviceInfoData, strId, iSize, ref iRequiredSize);
            if (iRet == 1)
            {
                return strId.ToString();
            }

            return string.Empty;
        }

        static string GetDevicePropertyString(IntPtr DeviceInfoSet, USBDeviceManagement.SP_DEVINFO_DATA DeviceInfoData, SetupDiGetDeviceRegistryPropertyEnum property)
        {
            byte[] ptrBuf = GetDeviceProperty(DeviceInfoSet, DeviceInfoData, property);
            return ToAutoString(ptrBuf);
        }

        static Guid GetDevicePropertyGuid(IntPtr DeviceInfoSet, USBDeviceManagement.SP_DEVINFO_DATA DeviceInfoData, SetupDiGetDeviceRegistryPropertyEnum property)
        {
            byte[] ptrBuf = GetDeviceProperty(DeviceInfoSet, DeviceInfoData, property);
            return new Guid(ptrBuf);
        }

        static byte[] GetDeviceProperty(IntPtr DeviceInfoSet, USBDeviceManagement.SP_DEVINFO_DATA DeviceInfoData, SetupDiGetDeviceRegistryPropertyEnum property)
        {
            StringBuilder strId = new StringBuilder(0);
            byte[] ptrBuf = null;
            UInt32 RegType;
            UInt32 iRequiredSize = 0;
            UInt32 iSize = 0;
            bool iRet = USBDeviceManagement.SetupDiGetDeviceRegistryProperty(DeviceInfoSet, ref DeviceInfoData,
                (uint)property, out RegType, ptrBuf, iSize, out iRequiredSize);
            ptrBuf = new byte[iRequiredSize];
            iSize = iRequiredSize;
            iRet = USBDeviceManagement.SetupDiGetDeviceRegistryProperty(DeviceInfoSet, ref DeviceInfoData,
                (uint)property, out RegType, ptrBuf, iSize, out iRequiredSize);
            if (iRet)
            {
                return ptrBuf;
            }

            return new byte[0];
        }

        static string ToAutoString(byte[] bytes)
        {
            string str = "";

            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length); 
                str = Marshal.PtrToStringAuto(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return str;
        }

        //public ReturnInfo GetMTKPortDeviceID(int dutId, int port, string comKeyWord, out string deviceID)
        //{
        //    ReturnInfo returnInfo = new ReturnInfo(false);
        //    deviceID = "";
        //    LogHelper.Log($"START: DUT{dutId}: GetMTKPortDeviceID", LogLevel.Info, LogDisplay.Both);
        //    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PnPEntity"))
        //    {
        //        try
        //        {
        //            var hardInfos = searcher.Get();
        //            foreach (ManagementObject hardInfo in hardInfos)
        //            {
        //                var args = new object[] { new string[] { "DEVPKEY_Device_FriendlyName", "DEVPKEY_Device_Parent" }, null };
        //                hardInfo.InvokeMethod("GetDeviceProperties", args);

        //                var mbos = (ManagementBaseObject[])args[1];

        //                var name = mbos[0].Properties.OfType<PropertyData>().FirstOrDefault(p => p.Name == "Data")?.Value;
        //                if (name != null)
        //                {
        //                    if (!name.ToString().Contains(comKeyWord))
        //                        continue;
        //                    int portnum = int.Parse(Regex.Match(name.ToString(), @"(?<=\(COM)\d+?(?=\))").Value);
        //                    if (portnum != port)
        //                        continue;
        //                    var parent = mbos[1].Properties.OfType<PropertyData>().FirstOrDefault(p => p.Name == "Data")?.Value;
        //                    if (parent == null)
        //                        continue;
        //                    string deviceid = Regex.Match(parent.ToString(), @"(?<=PID_[A-Fa-f0-9\\]{5})\S+").Value;
        //                    if (string.IsNullOrWhiteSpace(deviceid))
        //                    {
        //                        LogHelper.Log($"ERROR: DUT{dutId}: GetMTKPortDeviceID error,device ID is empty or NULL,or regex error.Full device ID:{parent}", LogLevel.Error, LogDisplay.Both);
        //                        returnInfo.isOk = false;
        //                        return returnInfo;
        //                    }
        //                    deviceID = deviceid;
        //                    LogHelper.Log($"FINISH: DUT{dutId}: GetMTKPortDeviceID device ID :{deviceID}", LogLevel.Info, LogDisplay.Both);
        //                    returnInfo.isOk = true;
        //                    return returnInfo;
        //                }
        //            }
        //            LogHelper.Log($"ERROR: DUT{dutId}:GetMTKPortDeviceID can't find device ID.", LogLevel.Error, LogDisplay.Both);
        //            returnInfo.isOk = false;
        //            return returnInfo;
        //        }
        //        catch (Exception ex)
        //        {
        //            var errMsg = $"ERROR: DUT: {ex.Message} {ex.StackTrace}";
        //            LogHelper.Log(errMsg, LogLevel.Error, LogDisplay.Both);
        //            returnInfo.isOk = false;
        //            returnInfo.msg = errMsg;
        //            return returnInfo;
        //        }
        //        finally
        //        {
        //            searcher.Dispose();
        //        }
        //    }
        //}
    }
}
