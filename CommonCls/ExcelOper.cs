using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;
using System.Collections;
//using Microsoft.Office.Core;
using System.Reflection;
//using Excel = Microsoft.Office.Interop.Excel;
 
///using Microsoft.Office.Interop.Excel;

namespace CommonCls
{
   public struct DataBaseInfo
    {
        public string DataBaseUserName;
        public string Servername;
        public int AuthenticationType;
        public string DataBaseName;
        public string UserName;
        public string Password;
        public string OnLineServer;
        public int OnLineServerPort;
        public int iBit;
    }

    public class ExcelOper
    {

        //若指定值為 No，代表 Excel 档中的工作表第一行就是資料了，沒有栏位名称

        string strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + System.Environment.CurrentDirectory + "\\DBINFO.xls;Extended Properties='Excel 8.0;HDR=Yes;IMEX=2;'";

        string strPath = Environment.CurrentDirectory + "\\DBINFO.xls";
    //    Excel.Application excelApp = new Excel.Application();
        DataBaseInfo dbi = new DataBaseInfo();
        public ExcelOper()
        {
         //   excelApp.Visible = false;
        }
   /*     public void ReadDataBaseInfo(ArrayList al)
        {
            try
            {
                // 打开工作簿
                Workbook workbook = excelApp.Workbooks.Open(strPath);

                // 获取第一个工作表
                Worksheet worksheet = workbook.Sheets[1];
                // 获取数据范围
                Range range = worksheet.UsedRange;

                // 获取行和列的数量
                int rowCount = range.Rows.Count;
                int colCount = range.Columns.Count;
                for (int i = 2; i <= rowCount; i++)
                {
                    dbi.DataBaseUserName = (string)(range.Cells[i, 1] as Range).Value2;
                    dbi.DataBaseName = (string)(range.Cells[i, 4] as Range).Value2;
                    dbi.OnLineServer = (string)(range.Cells[i, 7] as Range).Value2;
                    al.Add(dbi);
                }
                workbook.Close();
            }
            catch
            {

            }
        }

        //读取一条数据库记录
        public void ReadCertainDataBaseInfo(ref DataBaseInfo dbi, string strDatabasename)
        {
            Workbook workbook;
            try
            {
                // 打开工作簿
                workbook = excelApp.Workbooks.Open(strPath);

                // 获取第一个工作表
                Worksheet worksheet = workbook.Sheets[1];
                // 获取数据范围
                Range range = worksheet.UsedRange;

                // 获取行和列的数量
                int rowCount = range.Rows.Count;
                int colCount = range.Columns.Count;
                for (int i = 2; i <= rowCount; i++)
                {
                    dbi.DataBaseUserName = (string)(range.Cells[i, 1] as Range).Value2;
                    if (dbi.DataBaseUserName == strDatabasename)
                    {
                        dbi.Servername = (string)(range.Cells[i, 2] as Range).Value2;
                        dbi.AuthenticationType = (int)(range.Cells[i, 3] as Range).Value2;
                        dbi.DataBaseName = (string)(range.Cells[i, 4] as Range).Value2;
                        dbi.UserName = (string)(range.Cells[i, 5] as Range).Value2;
                        dbi.Password = (string)(range.Cells[i, 6] as Range).Value2;
                        dbi.OnLineServer = (string)(range.Cells[i, 7] as Range).Value2;
                        dbi.OnLineServerPort = (int)(range.Cells[i, 8] as Range).Value2;
                        dbi.iBit = (int)(range.Cells[i, 9] as Range).Value2;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            catch
            {

            }
        }


        public int DelCertainDataBaseInfo(int index)
        {
            // 打开工作簿
            Workbook workbook = excelApp.Workbooks.Open(strPath);
            // 获取第一个工作表
            Worksheet worksheet = workbook.Sheets[1];
            try
            {
                worksheet.Rows[index].Delete();
                // 保存删除后的文件
                workbook.Save();
                workbook.Close();
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 1;
            }
        }

        //编辑一条数据库信息
        public int EditCertainDataBaseInfo(DataBaseInfo dbi, string strDatabasename)
        {
            try
            {
                // 打开工作簿
                Workbook workbook = excelApp.Workbooks.Open(strPath);

                // 获取第一个工作表
                Worksheet worksheet = workbook.Sheets[1];
                // 获取数据范围
                Range range = worksheet.UsedRange;

                // 获取行和列的数量
                int rowCount = range.Rows.Count;

                for (int i = 2; i <= rowCount; i++)
                {
                    dbi.DataBaseUserName = (string)(range.Cells[i, 1] as Range).Value2;
                    if (dbi.DataBaseUserName == strDatabasename)
                    {
                        (range.Cells[i, 2] as Range).Value2 = dbi.Servername;
                        (range.Cells[i, 3] as Range).Value2 = dbi.AuthenticationType;
                        (range.Cells[i, 4] as Range).Value2 = dbi.DataBaseName;
                        (range.Cells[i, 5] as Range).Value2 = dbi.UserName;
                        (range.Cells[i, 6] as Range).Value2 = dbi.Password;
                        (range.Cells[i, 7] as Range).Value2 = dbi.OnLineServer;
                        (range.Cells[i, 8] as Range).Value2 = dbi.OnLineServerPort;
                        (range.Cells[i, 9] as Range).Value2 = dbi.iBit;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                // 保存修改后的文件
                workbook.Save();
                workbook.Close();
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 1;
            }

        }

        public int AddDataBaseInfo(DataBaseInfo dbi)
        {
            try
            {
                // 打开工作簿
                Workbook workbook = excelApp.Workbooks.Open(strPath);

                // 获取第一个工作表
                Worksheet worksheet = workbook.Sheets[1];
                // 获取数据范围
                Range range = worksheet.UsedRange;

                // 获取行和列的数量
                int i = range.Rows.Count + 1;


                (range.Cells[i, 1] as Range).Value2 = dbi.DataBaseUserName;

                (range.Cells[i, 2] as Range).Value2 = dbi.Servername;
                (range.Cells[i, 3] as Range).Value2 = dbi.AuthenticationType;
                (range.Cells[i, 4] as Range).Value2 = dbi.DataBaseName;
                (range.Cells[i, 5] as Range).Value2 = dbi.UserName;
                (range.Cells[i, 6] as Range).Value2 = dbi.Password;
                (range.Cells[i, 7] as Range).Value2 = dbi.OnLineServer;
                (range.Cells[i, 8] as Range).Value2 = dbi.OnLineServerPort;
                (range.Cells[i, 9] as Range).Value2 = dbi.iBit;

                // 保存修改后的文件
                workbook.Save();
                workbook.Close();
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 1;
            }

        }
*/

        public void CloseExcelApp()
        {
            try
            {

             //   excelApp.Quit();
            }
            catch
            {

            }
        }

    }
}
