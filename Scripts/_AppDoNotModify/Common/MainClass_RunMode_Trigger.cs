using System.Linq;
using System.Threading;
using UserHelpers.Helpers;
using System.Windows.Media;
using System.Data;
using Test._Definitions;

namespace Test
{
    public partial class MainClass
    {
        bool _isTriggered = false;
        public bool IsReadyToTrigger = true;

        public void TriggerSlot()
        {
            _isTriggered = true;
            if (!Project.IsDebug)
            {
                while (_isTriggered) //等待_isTriggered变为False，App_BeforeTesting里面会在检测到true后把它设置为false
                {
                    Thread.Sleep(5);
                }
            }
        }

        public int FirstOneReady(ITestItem item)
        {

            if (Project.RunningProjects.IndexOf(Project) == 0)//第一个工程
            {
                item.AddLog("all trigger start!");
                Project.RunningProjects.ForEach(p =>
                {
                    if (p != Project) //除第一个Project
                    {
                        item.AddLog("trigger project [{0}] ", p.ProjectIndex);
                        p.GetInstance<MainClass>().TriggerSlot();
                        item.AddLog("trigger project [{0}] done!", p.ProjectIndex);
                    }
                });
                item.AddLog("all trigger done!");
            }
            else
            {
                item.AddLog("skip!");
            }

            return 0;
        }
         
        public int Is_FirstOne(ITestItem item)
        {
            bool isFirst = (Project.RunningProjects.IndexOf(Project) == 0) || Project.IsDebug;

            if (isFirst)
                item.AddLog("I am the first one!");
            else
                item.AddLog("I am not the first one!");

            return isFirst ? 0 : 1;
        }

        public int Is_LastOne(ITestItem item)
        {
            bool isLast = (Project.RunningProjects.IndexOf(Project) == Project.RunningProjects.Count - 1) || Project.IsDebug;
             
            if (isLast)
                item.AddLog("I am the last one!");
            else
                item.AddLog("I am not the last one!");

            return isLast ? 0 : 1;
        }

        void RunMode_ParallelIndividually(ITimeLogger logger)
        {
            Project.SerialNumber = string.Empty;
            Test_Trigger_Invoke(logger);
            IsTestFinished = false;
        }

        void RunMode_ParallelFixtureReady(ITimeLogger logger)
        {
            IsReadyToTrigger = true;
            if (Project.RunningProjects.IndexOf(Project) == 0) //第一个工程
            {
                //等待所有工程都测试完成，并且脚本也要是完成状态 
                while (Project.RunningProjects.Any(p => p.IsTesting || p.GetInstance<MainClass>().IsTestFinished == false || p.GetInstance<MainClass>().IsReadyToTrigger == false))
                {
                    Thread.Sleep(10);
                }

                Project.RunningProjects.ForEach(p => p.SerialNumber = string.Empty);
                Test_Trigger_Invoke(logger);
                Project.RunningProjects.ForEach(p =>
                {
                    p.ClearResult();
                    p.ResetProgressBar();
                    p.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);
                });

                IsTestFinished = false; //Project 0先设置false，其它Project会由TriggerTest设置
                IsReadyToTrigger = false;
                _isTriggered = false;

                //全部触发
                logger.AddLog("all trigger start!");
                Project.RunningProjects.ForEach(p =>
                {
                    if (p != Project) //除第一个Project
                    {
                        logger.AddLog("trigger project [{0}] ", p.ProjectIndex);
                        p.GetInstance<MainClass>().TriggerSlot();
                        logger.AddLog("trigger project [{0}] done!", p.ProjectIndex);
                    }
                });
                logger.AddLog("all trigger done!");
            }
            else
            {
                //等待触发测试
                while (true)
                {
                    if (_isTriggered) //triggered, break
                        break;
                    Thread.Sleep(10);
                }
                IsTestFinished = false; //一定放_isTriggered reset之前
                _isTriggered = false; //reset
                IsReadyToTrigger = false;
            }
        }

        void RunMode_ParallelFirstOneReady(ITimeLogger logger)
        {
            IsReadyToTrigger = true;
            if (Project.RunningProjects.IndexOf(Project) == 0) //第一个工程
            {
                //等待所有工程都测试完成，并且脚本也要是完成状态 
                while (Project.RunningProjects.Any(p => p.IsTesting || p.GetInstance<MainClass>().IsTestFinished == false || p.GetInstance<MainClass>().IsReadyToTrigger == false))
                {
                    Thread.Sleep(10);
                }

                Project.RunningProjects.ForEach(p => p.SerialNumber = string.Empty);
                Test_Trigger_Invoke(logger);
                Project.RunningProjects.ForEach(p =>
                {
                    p.ClearResult();
                    p.ResetProgressBar();
                    p.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);
                });

                IsTestFinished = false; //Project 0先设置false，其它Project会由TriggerTest设置
                IsReadyToTrigger = false;
                _isTriggered = false;
            }
            else
            {
                //等待触发测试
                while (true)
                {
                    if (_isTriggered) //triggered, break
                        break;
                    Thread.Sleep(10);
                }
                IsTestFinished = false; //一定放_isTriggered reset之前
                _isTriggered = false; //reset
                IsReadyToTrigger = false;

            }
        }

        void RunMode_Sequential(ITimeLogger logger)
        {
            IsReadyToTrigger = true;
            if (Project.RunningProjects.IndexOf(Project) == 0) //第一个工程
            {
                //等待所有工程都测试完成，并且脚本也要是完成状态 
                while (Project.RunningProjects.Any(p => p.IsTesting || p.GetInstance<MainClass>().IsTestFinished == false || p.GetInstance<MainClass>().IsReadyToTrigger == false))
                {
                    Thread.Sleep(10);
                }

                Project.RunningProjects.ForEach(p => p.SerialNumber = string.Empty);
                Test_Trigger_Invoke(logger);
                Project.RunningProjects.ForEach(p =>
                {
                    p.ClearResult();
                    p.ResetProgressBar();
                    p.SideBar.TopBar.Add(ConstKeys.Bar_TestStatus, "Ready...", 14, 14, Colors.Black, Colors.Green);
                });

                IsTestFinished = false; //Project 0先设置false，其它Project会由TriggerTest设置
                IsReadyToTrigger = false;
                _isTriggered = false;

                //全部触发
                logger.AddLog("all trigger start!");
                Project.RunningProjects.ForEach(p =>
                {
                    if (p != Project) //除第一个Project
                    {
                        logger.AddLog("trigger project [{0}] ", p.ProjectIndex);
                        p.GetInstance<MainClass>().TriggerSlot();
                        logger.AddLog("trigger project [{0}] done!", p.ProjectIndex);
                    }
                });
                logger.AddLog("all trigger done!");

            }
            else
            {
                //等待触发测试
                while (true)
                {
                    if (_isTriggered) //triggered, break
                        break;
                    Thread.Sleep(10);
                }
                IsTestFinished = false; //一定放_isTriggered reset之前
                _isTriggered = false; //reset
                IsReadyToTrigger = false;

                while (true)
                {
                    if (Project.RunningProjects.Where(p => p.ProjectIndex < Project.ProjectIndex)
                        .All(p => !p.IsTesting && p.GetInstance<MainClass>().IsTestFinished))
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        void RunMode_Error(ITimeLogger logger)
        {
            string msg = "unknown run mode " + _Config.StartupConfig.TriggerMode.ToString();
            logger.AddLog(msg);
            while (true)
            {
                UIMessageBox.Show(Project.AppWindow, msg, "Run mode error", UIMessageBoxButton.OK, 14, Colors.Red);
            }
        }
    }
}

