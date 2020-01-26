﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 * 
 * All rights reserved.
 * 
 * EPPlus is an Open Source project provided under the 
 * GNU General Public License (GPL) as published by the 
 * Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 * 
 * EPPlus provides server-side generation of Excel 2007 spreadsheets.
 * See https://github.com/JanKallman/EPPlus for details.
 *
 * 
 * The GNU General Public License can be viewed at http://www.opensource.org/licenses/gpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 * 
 * The code for this project may be used and redistributed by any means PROVIDING it is 
 * not sold for profit without the author's written consent, and providing that this notice 
 * and the author's name and all copyright notices remain intact.
 * 
 * All code and executables are provided "as is" with no warranty either express or implied. 
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 *
 * Code change notes:
 * 
 * Author							Change						Date
 *******************************************************************************
 * Jan Källman		Added		10-SEP-2009
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using OfficeOpenXml;
using System.IO;
using System.Data.SqlClient;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Data.SQLite;
using OfficeOpenXml.Drawing.Chart.Style;

namespace EPPlusSamples.FXReportFromDatabase
{
    class FxReportFromDatabase
    {
        /// <summary>
        /// This sample creates a new workbook from a template file containing a chart and populates it with Exchange rates from 
        /// the database and set the three series on the chart.
        /// </summary>
        /// <param name="connectionString">Connectionstring to the db</param>
        /// <param name="template">the template</param>
        /// <param name="outputdir">output dir</param>
        /// <returns></returns>
        public static string Run(string connectionString)
        {
            FileInfo template = FileInputUtil.GetFileInfo("17-FXReportFromDatabase", "GraphTemplate.xlsx");

            using (ExcelPackage p = new ExcelPackage(template, true))
            {
                //Set up the headers
                ExcelWorksheet ws = p.Workbook.Worksheets[0];
                ws.Cells["A20"].Value = "Date";
                ws.Cells["B20"].Value = "EOD Rate";
                ws.Cells["B20:F20"].Merge = true;
                ws.Cells["G20"].Value = "Change";
                ws.Cells["G20:K20"].Merge = true;
                ws.Cells["B20:K20"].Style.HorizontalAlignment = ExcelHorizontalAlignment.CenterContinuous;
                using (ExcelRange row = ws.Cells["A20:G20"]) 
                {
                    row.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    row.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(23,55,93));
                    row.Style.Font.Color.SetColor(Color.White);
                    row.Style.Font.Bold = true;
                }
                ws.Cells["B21"].Value = "USD/SEK";
                ws.Cells["C21"].Value = "USD/EUR";
                ws.Cells["D21"].Value = "USD/INR";
                ws.Cells["E21"].Value = "USD/CNY";
                ws.Cells["F21"].Value = "USD/DKK";
                ws.Cells["G21"].Value = "USD/SEK";
                ws.Cells["H21"].Value = "USD/EUR";
                ws.Cells["I21"].Value = "USD/INR";
                ws.Cells["J21"].Value = "USD/CNY";
                ws.Cells["K21"].Value = "USD/DKK";
                using (ExcelRange row = ws.Cells["A21:K21"])
                {
                    row.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    row.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(184, 204, 228));
                    row.Style.Font.Color.SetColor(Color.Black);
                    row.Style.Font.Bold = true;
                }

                int startRow = 22;
                //Connect to the database and fill the data
                using (var sqlConn = new SQLiteConnection(connectionString))
                {
                    int row = startRow;
                    sqlConn.Open();
                    using (var sqlCmd = new SQLiteCommand("SELECT date, SUM(Case when CurrencyCodeTo = 'SEK' Then rate Else 0 END) AS [SEK], SUM(Case when CurrencyCodeTo = 'EUR' Then rate Else 0 END) AS [EUR], SUM(Case when CurrencyCodeTo = 'INR' Then rate Else 0 END) AS [INR], SUM(Case when CurrencyCodeTo = 'CNY' Then rate Else 0 END) AS [CNY], SUM(Case when CurrencyCodeTo = 'DKK' Then rate Else 0 END) AS [DKK]   FROM CurrencyRate where [CurrencyCodeFrom]='USD' AND CurrencyCodeTo in ('SEK', 'EUR', 'INR','CNY','DKK') GROUP BY date  ORDER BY date", sqlConn))
                    {
                        using (var sqlReader = sqlCmd.ExecuteReader())
                        {                            
                            // get the data and fill rows 22 onwards
                            while (sqlReader.Read())
                            {
                                ws.Cells[row, 1].Value = sqlReader[0];
                                ws.Cells[row, 2].Value = sqlReader[1];
                                ws.Cells[row, 3].Value = sqlReader[2];
                                ws.Cells[row, 4].Value = sqlReader[3];
                                ws.Cells[row, 5].Value = sqlReader[4];
                                ws.Cells[row, 6].Value = sqlReader[5];
                                row++;
                            }
                        }
                        //Set the numberformat
                        ws.Cells[startRow, 1, row - 1, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                        ws.Cells[startRow, 2, row - 1, 6].Style.Numberformat.Format = "#,##0.0000";
                        //Set the Formulas 
                        ws.Cells[startRow + 1, 7, row - 1, 11].Formula = $"B${startRow}/B{startRow+1}-1";
                        ws.Cells[startRow, 7, row - 1, 11].Style.Numberformat.Format = "0.00%";
                    }

                    //Set the series for the chart. The series must exist in the template or the program will crash.
                    var chart = ((ExcelLineChart)ws.Drawings["SampleChart"]); 
                    chart.Title.Text = "Exchange rate %";
                    chart.Series[0].Header = "USD/SEK";
                    chart.Series[0].XSeries = "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow+1, 1, row - 1, 1);
                    chart.Series[0].Series = "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 7, row - 1, 7);

                    chart.Series[1].Header = "USD/EUR";
                    chart.Series[1].XSeries = "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 1, row - 1, 1);
                    chart.Series[1].Series = "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 8, row - 1, 8);

                    chart.Series[2].Header = "USD/INR";
                    chart.Series[2].XSeries = "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 1, row - 1, 1);
                    chart.Series[2].Series = "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 9, row - 1, 9);

                    var serie = chart.Series.Add("'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 10, row - 1, 10),
                                        "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 1, row - 1, 1));
                    serie.Header = "USD/CNY";
                    serie.Marker.Style = eMarkerStyle.None;

                    serie = chart.Series.Add("'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 11, row - 1, 11),
                                        "'" + ws.Name + "'!" + ExcelRange.GetAddress(startRow + 1, 1, row - 1, 1));
                    serie.Header = "USD/DKK";
                    serie.Marker.Style = eMarkerStyle.None;

                    chart.Legend.Position = eLegendPosition.Bottom;

                    chart.StyleManager.SetChartStyle(ePresetChartStyle.LineChartStyle5);
                }
                
                //Get the documet as a byte array from the stream and save it to disk.  (This is useful in a webapplication) ... 
                var bin = p.GetAsByteArray();

                FileInfo file = FileOutputUtil.GetFileInfo("17-FxReportFromDatabase.xlsx");
                File.WriteAllBytes(file.FullName, bin);
                return file.FullName;
            }
        }
    }
}