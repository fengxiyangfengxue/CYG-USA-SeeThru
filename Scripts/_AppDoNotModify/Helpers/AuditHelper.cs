using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Test.Definition;
using Test._ScriptExtensions;
using UserHelpers.Helpers;
using Test.ScriptSettings;
using Test.StationsScripts.Shared;

namespace Test._ScriptHelpers
{
    class AuditHelper
    {
        List<Tuple<string, string>> _auditRecord = new List<Tuple<string, string>>();

        public AuditHelper()
        {

        }
          
        public int GetAuditTimes()
        {
            string key = DateTime.Now.ToString("yyyy-MM-dd");
            return _auditRecord.Where(r => r.Item1.Equals(key)).Select(r => r.Item2).Distinct().Count();
        }

        public void AddRecord(string sn)
        {
            _auditRecord.Add(new Tuple<string, string>(DateTime.Now.ToString("yyyy-MM-dd"), sn));
        }

        public void CheckAudioResult(ITestProject project, TEST_STATION station, int passCount)
        {

            try
            {
                DateTime dt = DateTime.Now;
                string key = dt.ToString("yyyy-MM-dd");

                var snList = _auditRecord.Where(r => r.Item1.Equals(key)).Select(r => r.Item2).Distinct().ToList();

                if (snList.Count >= passCount)
                {
                    string fileName = Path.Combine(MainClass.CaesarConfigPath, station.ToString() + "\\AuditRecord\\slot_" + (project.ProjectIndex + 1).ToString() + "_last.txt");
                    FileInfo fi = new FileInfo(fileName);
                    if (!fi.Directory.Exists)
                        fi.Directory.Create();

                    string log = Convert.ToBase64String(Encoding.Default.GetBytes(dt.ToString("yyyy-MM-dd HH:mm:ss")));
                    File.WriteAllText(fi.FullName, log);

                    fileName = Path.Combine(MainClass.CaesarConfigPath, station.ToString() + "\\AuditRecord\\slot_" + (project.ProjectIndex + 1).ToString() + "_history.txt");
                    fi = new FileInfo(fileName);
                    if (!fi.Directory.Exists)
                        fi.Directory.Create();

                    File.AppendAllText(fi.FullName, dt.ToString("yyyy-MM-dd HH:mm:ss") + "," + snList.CombineToString(",") + Environment.NewLine);

                    _auditRecord.Clear();
                }
            }
            catch (Exception ex)
            {
                UIMessageBox.Show(project, ex.ToString(), "UpdateAuditRecord", UIMessageBoxButton.OK, 14, Colors.Red);
            }
        }
        public bool Check_AuditSerialnumber(Action<string> logger, ITestProject project, TestContext _context, List<string> audioSNList, TEST_STATION station, string sn)
        {
            bool result = true;
           
            logger.AddLog("Audit SN = " + audioSNList.CombineToString(","));

            if (audioSNList.Any(s => s.Equals(sn, StringComparison.OrdinalIgnoreCase)))
            {
                logger.AddLog("Doing Audit!");
                _context.IsAudit = true;
                _context.ScriptMode = Script_Mode.Audit;

                //Stinson PCBA Line switch to offline when doing audit
                logger.AddLog("_isAudit = true");
                logger.AddLog("_scriptMode = " + _context.ScriptMode.ToString());
                project.PathDictionary["ScriptMode"] = _context.ScriptMode.ToString();
            }
            else
            {

                int ret = CheckAuditRecord(logger.AddLog, project, station);
                logger.AddLog("ret = " + ret);

                if (ret == 1)
                {
                    logger.AddLog("Audit check failed! test continue...");
                }
                else if (ret == 2)
                {
                    result = false;
                    _context.IsAuditCheckFail = true;
                    logger.AddLog("Audit check failed!");
                    UIMessageBox.Show(project, "Do Audit!", "Do Audit", UIMessageBoxButton.OK, 14, Colors.Red);
                    goto ReturnAndExit;
                }
                else
                    logger.AddLog("Audit check passed!");
            }

        ReturnAndExit:
            return result;
        }

        /// <summary>
        /// 0 = pass
        /// 1 = fail, continue test
        /// 2 = fail, stop test
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        int CheckAuditRecord(Action<string> logger, ITestProject Project, TEST_STATION station)
        {
            int result = 2;
            Action<string> addLog = (s) =>
            {
                if (logger != null)
                    logger(s);
            };
            try
            {

                string fileName = Path.Combine(MainClass.CaesarConfigPath, station.ToString() + "\\AuditRecord\\slot_" + (Project.ProjectIndex + 1).ToString() + "_last.txt");
                FileInfo fi = new FileInfo(fileName);

                if (!fi.Exists)
                {
                    addLog.Invoke(fi.FullName + " not found!");
                    goto ReturnAndExit;
                }

                string log = File.ReadAllText(fileName).RemoveCRLF();
                log = Encoding.Default.GetString(Convert.FromBase64String(log));

                DateTime dt = DateTime.Parse(log);
                addLog.Invoke("audit timeout = " + dt.ToString("yyyy-MM-dd HH:mm:ss"));

                DateTime now = DateTime.Now;
                addLog.Invoke("dt = " + dt.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                addLog.Invoke("now = " + now.Date.ToString("yyyy-MM-dd HH:mm:ss"));

                if (now < dt.Date || now > dt.Date.AddDays(1).AddHours(10))
                {
                    result = 2;
                    addLog.Invoke("audit timeout invalid!");
                    goto ReturnAndExit;
                }
                else if (now >= dt.Date.AddDays(1) && now <= dt.Date.AddDays(1).AddHours(10))
                {
                    result = 1;
                    addLog.Invoke("audit timeout invalid!");
                    goto ReturnAndExit;
                }
                addLog.Invoke("audit timeout valid!");

                result = 0;
            }
            catch (Exception ex)
            {
                result = 2;
                addLog.Invoke(ex.ToString());
            }

        ReturnAndExit:
            return result;
        }




    }
}
