using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Test._ScriptHelpers
{
    /// <summary>
    /// 重试
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// 返回func的结果
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="validFunc"></param>
        /// <param name="context"></param>
        /// <param name="maxRetryTimes"></param>
        /// <param name="delay"></param>
        /// <param name="sleepTime"></param>
        /// <returns></returns>
        public static TResult TryInvoke<TResult>(Func<TResult> func, Func<TResult, bool> validFunc, Action<string> logger = null, int maxRetryTimes = 3, bool delay = false, int sleepTime = 1000)
        {
            var result = func();
            var time = 1;
            while (!validFunc(result) && time++ < maxRetryTimes)
            {
                logger.Invoke("time:" + time);
                try
                {
                    if (delay)
                        Thread.Sleep(sleepTime);
                    result = func();
                }
                catch (Exception ex)
                {
                    logger.Invoke(ex.ToString());
                }
            }
            return result;
        }

        public static string TryInvokeNoDevice(Func<string> func, out bool ispass, Action<string> logger = null, int maxRetryTimes = 3, bool delay = false, int sleepTime = 1000)
        {
            var result = func();
            var time = 1;
            Func<bool> act = () =>
            {
                return !result.Contains("not found") && !result.Contains("no devices") &&
                       !result.ToUpper().Contains("ERROR") && !result.Contains("fail");
            };
            while (!(ispass = act()) && time++ < maxRetryTimes)
            {
                logger.Invoke("time:" + time);
                try
                {
                    if (delay)
                        Thread.Sleep(sleepTime);
                    result = func();
                }
                catch (Exception ex)
                {
                    logger.Invoke(ex.ToString());
                }
            }
            return result;
        }

        public static void Invoke(Func<bool> func, Action action, TimeSpan time)
        {
            var result = func();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!result && sw.Elapsed < time)
            {
                Task.Delay(500);
                result = func();
            }
            sw.Stop();
            action();
        }

        /// <summary>
        /// 用于超时
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="validFunc"></param>
        /// <param name="context"></param>
        /// <param name="sleepTime"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static TResult InvokeTimeOut<TResult>(Func<TResult> func, Func<TResult, bool> validFunc, Action<string> logger = null, int sleepTime = 200, int timeOut = 500000)
        {
            var result = func();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!validFunc(result) && sw.ElapsedMilliseconds < timeOut)
            {
                try
                {
                    Thread.Sleep(sleepTime);
                    result = func();
                }
                catch (Exception ex)
                {
                    logger.Invoke(ex.ToString());
                }
            }
            sw.Stop();
            return result;
        }
    }
}
