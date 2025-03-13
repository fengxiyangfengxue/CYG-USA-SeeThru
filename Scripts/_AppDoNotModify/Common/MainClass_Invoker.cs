using System;
using System.Linq;
using System.Reflection;
using Test.Definition;
using Test._Definitions;
using UserHelpers.Helpers;

namespace Test
{
    public partial class MainClass
    {

        int MainClassConstructor_Invoke(int startLevel = 0, int endLevel = 1000)
        {

            int invokedCount = 0;
            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.GetCustomAttributes().Count() > 0 &&
                            f.GetCustomAttributes().Any(a => a.GetType() == typeof(MainClassConstructorAttribute)) &&
                            f.GetParameters().Count() == 0 && f.ReturnType == typeof(int))
                .OrderBy(m => ((MainClassConstructorAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(MainClassConstructorAttribute))).Level)
                                    .ToList();


            foreach (var method in methodNames)
            {
                try
                {
                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(MainClassConstructorAttribute) &&
                                                    (((MainClassConstructorAttribute)a).Level >= startLevel && ((MainClassConstructorAttribute)a).Level <= endLevel) &&
                                                    (((MainClassConstructorAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((MainClassConstructorAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;
                    var result = (int)method.Invoke(this, new object[] { });
                    invokedCount++;
                }
                catch (Exception ex)
                {

                }
            }

            return invokedCount;
        }

        int App_BeforeTesting_Invoke(ITimeLogger logger, int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 1
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(BeforeTestingAttribute)))
                                    //排序(执行顺序的优先级)
                                    .OrderBy(m => ((BeforeTestingAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(BeforeTestingAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var parameters = method.GetParameters();
                    if (parameters[0].ParameterType != typeof(ITimeLogger))
                        continue;

                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(BeforeTestingAttribute) &&
                                                    (((BeforeTestingAttribute)a).Level >= startLevel && ((BeforeTestingAttribute)a).Level <= endLevel) &&
                                                    (((BeforeTestingAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((BeforeTestingAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    logger.AddLog("Invoke " + method.Name);
                    var result = (int)method.Invoke(this, new object[] { logger });
                    invokedCount++;
                    logger.AddLog("Return Code = " + result);
                }
                catch (Exception ex)
                {
                    logger.AddLog(ex.ToString());
                }
            }

            logger.AddLog("invokedCount = " + invokedCount);
            return invokedCount;
        }

        int Test_Trigger_Invoke(ITimeLogger logger, int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(f => f.GetParameters().Count() == 1
                                    && f.ReturnType == typeof(int)
                                    && f.GetCustomAttributes().Count() > 0
                                    && f.GetCustomAttributes().Any(a => a.GetType() == typeof(TriggerAttribute)))
                        .OrderBy(m => ((TriggerAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(TriggerAttribute))).Level)
                        .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var parameters = method.GetParameters();
                    if (parameters[0].ParameterType != typeof(ITimeLogger))
                        continue;
                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(TriggerAttribute) &&
                                                    (((TriggerAttribute)a).Level >= startLevel && ((TriggerAttribute)a).Level <= endLevel) &&
                                                    (((TriggerAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((TriggerAttribute)a).Station == TEST_STATION.ANY_STATION)).ToList();
                    if (attributes.Count == 0)
                        continue;

                    logger.AddLog("Invoke " + method.Name);
                    var result = (int)method.Invoke(this, new object[] { logger });
                    invokedCount++;
                    logger.AddLog("Return Code = " + result);
                }
                catch (Exception ex)
                {
                    logger.AddLog(ex.ToString());
                }
            }

            logger.AddLog("invokedCount = " + invokedCount);
            return invokedCount;
        }

        int Script_Initialize_Invoke(ITestItem item, int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 1
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(ScriptInitializeAttribute)))
                                    .OrderBy(m => ((ScriptInitializeAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(ScriptInitializeAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                //try
                //{
                var parameters = method.GetParameters();
                if (parameters[0].ParameterType != typeof(ITestItem))
                    continue;

                var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(ScriptInitializeAttribute) &&
                                                (((ScriptInitializeAttribute)a).Level >= startLevel && ((ScriptInitializeAttribute)a).Level <= endLevel) &&
                                                (((ScriptInitializeAttribute)a).Station == _Config.StartupConfig.Station ||
                                                ((ScriptInitializeAttribute)a).Station == TEST_STATION.ANY_STATION))
                                        .ToList();

                if (attributes.Count == 0)
                    continue;
                int level = ((ScriptInitializeAttribute)attributes.FirstOrDefault()).Level;
                item.AddLog("Level " + level);
                item.AddLog("Invoke " + method.Name);
                var result = (int)method.Invoke(this, new object[] { item });
                invokedCount++;
                item.AddLog("Return Code = " + result);
                //}
                //catch (Exception ex)
                //{
                //    item.AddLog(ex.ToString());
                //}
            }

            item.AddLog("invokedCount = " + invokedCount);
            return invokedCount;
        }

        int App_AfterScript_Invoke(ITimeLogger logger, int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 1
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(AfterScriptAttribute)))
                                    .OrderBy(m => ((AfterScriptAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(AfterScriptAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var parameters = method.GetParameters();
                    if (parameters[0].ParameterType != typeof(ITimeLogger))
                        continue;

                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(AfterScriptAttribute) &&
                                                    (((AfterScriptAttribute)a).Level >= startLevel && ((AfterScriptAttribute)a).Level <= endLevel) &&
                                                    (((AfterScriptAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((AfterScriptAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    logger.AddLog("Invoke " + method.Name);
                    var result = (int)method.Invoke(this, new object[] { logger });
                    invokedCount++;
                    logger.AddLog("Return Code = " + result);
                }
                catch (Exception ex)
                {
                    logger.AddLog(ex.ToString());
                }
            }

            logger.AddLog("invokedCount = " + invokedCount);

            return invokedCount;
        }

        int App_BeforeSavingLog_Invoke(ITimeLogger logger, int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 1
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(BeforeSavingLogAttribute)))
                                    .OrderBy(m => ((BeforeSavingLogAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(BeforeSavingLogAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var parameters = method.GetParameters();
                    if (parameters[0].ParameterType != typeof(ITimeLogger))
                        continue;

                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(BeforeSavingLogAttribute) &&
                                                    (((BeforeSavingLogAttribute)a).Level >= startLevel && ((BeforeSavingLogAttribute)a).Level <= endLevel) &&
                                                    (((BeforeSavingLogAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((BeforeSavingLogAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    logger.AddLog("Invoke " + method.Name);
                    var result = (int)method.Invoke(this, new object[] { logger });
                    invokedCount++;
                    logger.AddLog("Return Code = " + result);
                }
                catch (Exception ex)
                {
                    logger.AddLog(ex.ToString());
                }
            }

            logger.AddLog("invokedCount = " + invokedCount);

            return invokedCount;
        }

        int App_LogFilter_Invoke(ILogInformation logInfo, ITimeLogger logger, int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 2
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(LogFilterAttribute)))
                                    .OrderBy(m => ((LogFilterAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(LogFilterAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var parameters = method.GetParameters();
                    if (parameters[0].ParameterType != typeof(ILogInformation))
                        continue;
                    if (parameters[1].ParameterType != typeof(ITimeLogger))
                        continue;

                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(LogFilterAttribute) &&
                                                    (((LogFilterAttribute)a).Level >= startLevel && ((LogFilterAttribute)a).Level <= endLevel) &&
                                                    (((LogFilterAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((LogFilterAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    logger.AddLog("Invoke " + method.Name);
                    var result = (int)method.Invoke(this, new object[] { logInfo, logger });
                    invokedCount++;
                    logger.AddLog("Return Code = " + result);
                }
                catch (Exception ex)
                {
                    logger.AddLog(ex.ToString());
                }
            }

            logger.AddLog("invokedCount = " + invokedCount);

            return invokedCount;
        }

        int App_AfterSavingLog_Invoke(int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 0
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(AfterSavingLogAttribute)))
                                    //排序(执行顺序的优先级)
                                    .OrderBy(m => ((AfterSavingLogAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(AfterSavingLogAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    //var parameters = method.GetParameters();
                    //if (parameters[0].ParameterType != typeof(ITimeLogger))
                    //    continue;

                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(AfterSavingLogAttribute) &&
                                                    (((AfterSavingLogAttribute)a).Level >= startLevel && ((AfterSavingLogAttribute)a).Level <= endLevel) &&
                                                    (((AfterSavingLogAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((AfterSavingLogAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    var result = (int)method.Invoke(this, new object[] { });
                    invokedCount++;
                }
                catch(Exception ex)
                {

                }
            }

            return invokedCount;
        }

        int App_BeforeShowingResult_Invoke(int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 0
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(BeforeShowingResultAttribute)))
                                    //排序(执行顺序的优先级)
                                    .OrderBy(m => ((BeforeShowingResultAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(BeforeShowingResultAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(BeforeShowingResultAttribute) &&
                                                    (((BeforeShowingResultAttribute)a).Level >= startLevel && ((BeforeShowingResultAttribute)a).Level <= endLevel) &&
                                                    (((BeforeShowingResultAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((BeforeShowingResultAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    var result = (int)method.Invoke(this, new object[] { });
                    invokedCount++;
                }
                catch
                {
                }
            }

            return invokedCount;
        }

        int App_AfterTesting_Invoke(int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 0
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(AfterTestingAttribute)))
                                    //排序(执行顺序的优先级)
                                    .OrderBy(m => ((AfterTestingAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(AfterTestingAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    //var parameters = method.GetParameters();
                    //if (parameters[0].ParameterType != typeof(ITimeLogger))
                    //    continue;

                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(AfterTestingAttribute) &&
                                                    (((AfterTestingAttribute)a).Level >= startLevel && ((AfterTestingAttribute)a).Level <= endLevel) &&
                                                    (((AfterTestingAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((AfterTestingAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    var result = (int)method.Invoke(this, new object[] { });
                    invokedCount++;
                }
                catch
                {
                }
            }

            return invokedCount;
        }

        int App_AfterClosed_Invoke(int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 0
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(AfterClosedAttribute)))
                                    //排序(执行顺序的优先级)
                                    .OrderBy(m => ((AfterClosedAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(AfterClosedAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(AfterClosedAttribute) &&
                                                    (((AfterClosedAttribute)a).Level >= startLevel && ((AfterClosedAttribute)a).Level <= endLevel) &&
                                                    (((AfterClosedAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((AfterClosedAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    var result = (int)method.Invoke(this, new object[] { });
                    invokedCount++;
                }
                catch
                {
                }
            }

            return invokedCount;
        }

        int Script_Dispose_Invoke(int startLevel = 0, int endLevel = 1000)
        {
            int invokedCount = 0;

            var methodNames = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                    .Where(f => f.GetParameters().Count() == 0
                                                && f.ReturnType == typeof(int)
                                                && f.GetCustomAttributes().Count() > 0
                                                && f.GetCustomAttributes().Any(a => a.GetType() == typeof(MainClassDisposeAttribute)))
                                    .OrderBy(m => ((MainClassDisposeAttribute)m.GetCustomAttributes().FirstOrDefault(a => a.GetType() == typeof(MainClassDisposeAttribute))).Level)
                                    .ToList();

            foreach (var method in methodNames)
            {
                try
                {
                    var attributes = method.GetCustomAttributes().Where(a => a.GetType() == typeof(MainClassDisposeAttribute) &&
                                                    (((MainClassDisposeAttribute)a).Level >= startLevel && ((MainClassDisposeAttribute)a).Level <= endLevel) &&
                                                    (((MainClassDisposeAttribute)a).Station == _Config.StartupConfig.Station ||
                                                    ((MainClassDisposeAttribute)a).Station == TEST_STATION.ANY_STATION))
                                            .ToList();

                    if (attributes.Count == 0)
                        continue;

                    var result = (int)method.Invoke(this, new object[] { });
                    invokedCount++;

                }
                catch { }
            }

            return invokedCount;
        }
    }
}