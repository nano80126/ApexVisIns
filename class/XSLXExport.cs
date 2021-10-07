using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace ApexVisIns
{
    public class XSLXExport
    {
        private XLWorkbook Workbook;
        private IXLWorksheet sheet;

        public XSLXExport(Type t)
        {
            CreateWorkbook(t);
        }

        private void CreateWorkbook(Type t)
        {
            Workbook = new();

            sheet = Workbook.Worksheets.Add("Record");

            int colIdx = 1;

            foreach (PropertyInfo prop in t.GetProperties())
            {
                DescriptionAttribute des = prop.GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute;

                sheet.Cell(1, colIdx).Value = des != null ? des.Description : prop.Name;

                #region Header Cell
                _ = sheet.Cell(1, colIdx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                _ = sheet.Cell(1, colIdx).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                _ = sheet.Cell(1, colIdx).Style.Font.SetFontSize(14);
                _ = sheet.Cell(1, colIdx).Style.Font.SetBold();
                #endregion

                #region Column
                sheet.Column(colIdx).Width = 14;
                #endregion

                colIdx++;
            }
        }

        public XLWorkbook Export<T>(List<T> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("No Data Exists.");
            }

            //// 建立 Excel 物件
            //XLWorkbook workbook = new();
            ////// 加入工作表
            //IXLWorksheet sheet = workbook.Worksheets.Add("Report");
            //// 起始 Col 位置
            //int colIdx = 1;
            //foreach (PropertyInfo item in typeof(T).GetProperties())
            //{
            //    DescriptionAttribute des = item.GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute;

            //    // des != null 則使用 Description, 否則使用 Name
            //    sheet.Cell(1, colIdx).Value = des != null ? des.Description : item.Name;

            //    #region Cell
            //    _ = sheet.Cell(1, colIdx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            //    _ = sheet.Cell(1, colIdx).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            //    _ = sheet.Cell(1, colIdx).Style.Font.SetFontSize(14);
            //    _ = sheet.Cell(1, colIdx).Style.Font.SetBold();
            //    #endregion

            //    #region Column
            //    sheet.Column(colIdx).Width = 14;
            //    #endregion

            //    colIdx++;
            //}

            for (int i = 0; i < list.Count; i++)
            {
                PropertyInfo[] info = list[i].GetType().GetProperties();

                for (int j = 0; j < info.Length; j++)
                {
                    //sheet.Cell(i + 2, j + 1).Value = string.Concat("'", info[j].GetValue(list[i]).ToString());
                    sheet.Cell(i + 2, j + 1).Value = info[j].GetValue(list[i]).ToString();
                    _ = sheet.Cell(i + 2, j + 1).Style.Font.SetFontSize(12);
                }
            }
            return Workbook;
        }
    }
}
