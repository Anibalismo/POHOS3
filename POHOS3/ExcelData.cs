using Excel = Microsoft.Office.Interop.Excel;
using System.Data;
using System.Reflection;
using System.IO;


namespace POHOS3
{
    class ExcelData
    {
        public DataView Data
        {
            get
            {
                Excel.Application excelApp = new Excel.Application();
                Excel.Workbook workbook;
                Excel.Worksheet worksheet;
                Excel.Range range;
                workbook = excelApp.Workbooks.Open(Directory.GetParent( Directory.GetCurrentDirectory() ).Parent.FullName + "\\Prueba.xlsx");
                worksheet = (Excel.Worksheet)workbook.Sheets["Hoja1"];

                int column = 0;
                int row = 0;

                range = worksheet.UsedRange;
                DataTable dt = new DataTable();

                for (column = 1; column <= range.Columns.Count; column++)
                {
                    //dr[column - 1] = (range.Cells[row, column] as Excel.Range).Value2.ToString();
                    dt.Columns.Add((range.Cells[1, column] as Excel.Range).Value2.ToString());
                }
                
                for (row = 2; row <= range.Rows.Count; row++)
                {
                    DataRow dr = dt.NewRow();
                    for (column = 1; column <= range.Columns.Count; column++)
                    {
                        dr[column - 1] = (range.Cells[row, column] as Excel.Range).Value2.ToString();
                    }
                    dt.Rows.Add(dr);
                    dt.AcceptChanges();
                }
                workbook.Close(true, Missing.Value, Missing.Value);
                excelApp.Quit();
                return dt.DefaultView;
            }
        }
    }
}
