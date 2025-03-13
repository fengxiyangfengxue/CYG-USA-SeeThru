using System;
using System.Runtime.InteropServices;

namespace Test._ScriptExtensions
{
    public static class ByteExtension
    {
        public static string ToHexString(this byte[] bytes)
        {
            string ret = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    ret += bytes[i].ToString("X2");
                }
            }
            return ret;
        }

        public static string ToHexString(this byte[] bytes, string spliter)
        {
            string ret = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (ret.Length > 0)
                        ret += spliter;

                    ret += bytes[i].ToString("X2");
                }
            }
            return ret;
        }

        public static string ToHexString(this byte[] bytes, string preFix, string spliter)
        {
            string ret = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (ret.Length > 0)
                        ret += spliter;

                    ret += preFix + bytes[i].ToString("X2");
                }
            }
            return ret;
        }

        public static string ToHexString(this byte[] bytes, int offset, int count)
        {
            string ret = "";
            if (bytes != null)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    ret += bytes[i].ToString("X2");
                }
            }
            return ret;
        }

        public static string ToHexString(this byte[] bytes, int offset, int count, string spliter)
        {
            string ret = "";
            if (bytes != null)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    if (ret.Length > 0)
                        ret += spliter;

                    ret += bytes[i].ToString("X2");
                }
            }
            return ret;
        }

        public static string ToHexString(this byte[] bytes, int offset, int count, string preFix, string spliter)
        {
            string ret = "";
            if (bytes != null)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    if (ret.Length > 0)
                        ret += spliter;

                    ret += preFix + bytes[i].ToString("X2");
                }
            }
            return ret;
        }

        public static T BytesToStruct<T>(byte[] bytes)
        {
            if (bytes == null) return default(T);
            if (bytes.Length <= 0) return default(T);
            int size = Marshal.SizeOf(typeof(T));
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return (T)Marshal.PtrToStructure(buffer, typeof(T));
            }
            catch (Exception ex)
            {
                throw new Exception("Error in BytesToStruct ! " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
