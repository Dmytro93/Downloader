using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader
{
    public class XlsxConverter
    {

        static private string BasicWorkSheetsName(ExcelPackage package, string worksheetsname)//ПОЛНОЦЕННЫЙ(вроде) МЕТОД ДЛЯ ИМЕНОВАНИЯ ЛИСТОВ С ОГЛЯДКОЙ НА ИМЕНА СУЩЕСТВУЮЩИХ(неповторение названий)
        {
            ExcelWorksheets excelWorksheets = package.Workbook.Worksheets;
            var worksheetsnames = excelWorksheets.Select(x => x.Name);//Regex.Match(x, $@"({worksheetsname})( \((\d+)\))?") - УТОЧНЯТЬ!!!
            var namesFirstpart = worksheetsnames.Select(x => Regex.Match(x, $@"({worksheetsname})( \((\d+)\))?").Groups[1].Value).Where(x1 => x1 != "").ToList();
            var namesDigits = worksheetsnames.Select(x => Regex.Match(x, $@"({worksheetsname})( \((\d+)\))?").Groups[3].Value).Where(x1 => !string.IsNullOrWhiteSpace(x1))
                .Select(x2 => Convert.ToInt32(x2)).ToList();
            if (namesFirstpart == null || namesFirstpart.Count == 0 || namesDigits.Count == namesFirstpart.Count)
            {
                return worksheetsname;
            }
            else if (namesDigits == null || namesDigits.Count == 0)
            {
                return worksheetsname = $"{worksheetsname} (1)";
            }
            else
            {
                for (int i = 1; i < 256; i++)
                {
                    if (!namesDigits.Contains(i))
                    {
                        return worksheetsname = $"{worksheetsname} ({i})";
                    }
                }
            }
            return worksheetsname;
        }
        static private string WorkSheetsName(ExcelPackage package)
        {
            string worksheetsname = "Test";
            return BasicWorkSheetsName(package, worksheetsname);
        }
        static private string WorkSheetsName(ExcelPackage package, string defaultworksheetsname)
        {
            return BasicWorkSheetsName(package, defaultworksheetsname);
        }
        static public async Task ConvertToXLSX(string csvfilename, string xlsxfilename)
        {
            await Task.Run(() =>
            {
                string worksheetsname;
                bool firstRowIsHeader = true;
                var format = new ExcelTextFormat
                {
                    Delimiter = ';',
                    EOL = "\n"
                };
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (ExcelPackage package = new ExcelPackage(new FileInfo(xlsxfilename)))
                {
                    //Console.WriteLine("Введите желаемое имя Листа Excel");
                    //string defaultworksheetsname = Console.ReadLine();
                    string defaultworksheetsname = "";
                    if (string.IsNullOrEmpty(defaultworksheetsname))
                        worksheetsname = WorkSheetsName(package);
                    else
                        worksheetsname = WorkSheetsName(package, defaultworksheetsname);
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(worksheetsname);
                    worksheet.Cells["A1"].LoadFromText(new FileInfo(csvfilename), format, OfficeOpenXml.Table.TableStyles.Medium27, firstRowIsHeader);
                    package.Save();
                }
            });
            //File.Delete(csvfilename);
        }
    }
}
