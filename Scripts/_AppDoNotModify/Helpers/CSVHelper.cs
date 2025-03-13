using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test._ScriptExtensions;
using UserHelpers.Helpers;

namespace Test._ScriptHelpers
{
    public class CSVHelper
    {
 
        public static bool LoadCSV(ITestItem item, string fileName, ref DataTable dt)
        {
            bool result = false;
            var fi = new FileInfo(fileName);
            item.AddLog("load " + fi.FullName);
            dt.Reset(); 
            List<string> lines = new List<string>();

            int retryTimes = 5;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    lines = File.ReadAllLines(fi.FullName).Where(l => l.Length > 0).ToList();
                    break;
                }
                catch (Exception ex)
                {
                    if (i == retryTimes - 1)
                        throw ex;

                    if (fi.Exists)
                    {
                        item.AddLog("read csv failed!, retry...");
                        item.Sleep(1000);
                    }
                }
            }


            if (lines.Count == 0)
            {
                item.AddLog("rows error!");
                goto ReturnAndExit;
            }

            //create columns
            List<DataColumn> columns = new List<DataColumn>();
            lines[0].SplitToList(",", true, false).ForEach(t =>
            {
                if (!columns.Any(c => c.ColumnName.Equals(t))) //不存在才添加
                    columns.Add(new DataColumn(t));
            });
            dt.Columns.AddRange(columns.ToArray());

            for (int i = 1; i < lines.Count; i++)
            {
                var arr = lines[i].Split(',');
                DataRow row = dt.NewRow();
                for (int j = 0; j < arr.Count(); j++)
                {
                    row[j] = arr[j];
                    //item.AddLog(dt.Columns[j].ColumnName + " = " + arr[j]);
                }
                dt.Rows.Add(row);
            }

            item.AddLog("csv data loaded!");

            result = true;

        ReturnAndExit:
            return result;

        }

        public static string ToCSV(DataTable dt)
        {
            StringBuilder csv = new StringBuilder();

            try
            {
                //title
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    csv.Append(dt.Columns[i].ColumnName);
                    csv.Append(i == dt.Columns.Count - 1 ? "" : ",");
                }
                csv.Append(Environment.NewLine);


                //body
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        csv.Append(dt.Rows[i][j]);
                        csv.Append(j == dt.Columns.Count - 1 ? "" : ",");
                    }
                    csv.Append(Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return csv.ToString();
        }

    }
}
