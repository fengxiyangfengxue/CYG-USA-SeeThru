using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Test._App;
using UserHelpers.Helpers;
using System.Windows.Media; 
using System.Data;

using Test.Definition;
using Test._ScriptExtensions;
using Test._Definitions;
using Test._ScriptHelpers;

namespace Test
{
    public partial class MainClass
    {
        AuditHelper _auditHelper = new AuditHelper();

        [ScriptInitialize(TEST_STATION.ANY_STATION, level: 1)]
        public int ScriptInitialize_Audit(ITestItem item)
        {
            _Context.IsAudit = false;
            _Context.IsAuditCheckFail = false;
            return 0;
        }


        [AfterScript(TEST_STATION.ANY_STATION, level: 1)]
        public int AfterScript_Audit(ITimeLogger logger)
        {
            bool result = false;
            try
            {
                if (_Context.IsAudit && !Project.HasFailed)
                {
                    _auditHelper.AddRecord(Project.SerialNumber); 
                    _auditHelper.CheckAudioResult(Project, _Station, _commonSetting.AuditSNCount);
                }
                result = true;
            } 
            catch (Exception ex)
            {
                logger.AddLog(ex.ToString());
                UIMessageBox.Show(Project, ex.ToString());
            }
            finally
            {
                if (!result)
                {
                    UIMessageBox.Show(Project, _Station.ToString() + "AfterScript failed!", "AfterScript fail", UIMessageBoxButton.OK, 14, Colors.Red);
                }
            }
             
            return 0;
        }

        public int Audit_Check(ITestItem item)
        {
            bool result = false;

            try
            {
                item.AddLog("SN = " + Project.SerialNumber);

                if (!Project.IsOnLine)
                {
                    item.AddLog("offline skip audit");
                    result = true;
                    goto ReturnAndExit;
                }

                result = _auditHelper.Check_AuditSerialnumber(item.AddLog, Project, _Context, _Config.AuditSNList, _Station, Project.SerialNumber);
            }
            catch (Exception ex)
            {
                item.AddLog(ex.ToString());
            }

        ReturnAndExit:

            ResultData data = new ResultData(item.Title, CreateErrorCode(result, item.Title), result ? ConstKeys.PASS : ConstKeys.FAIL);
            AddResult(item, data);
            return result ? 0 : 1;
        }

    }
}

