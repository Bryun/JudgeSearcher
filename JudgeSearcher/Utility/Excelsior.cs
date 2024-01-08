using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeSearcher.Utility
{
    public static class Excelsior
    {
        /// <summary>
        /// WorkbookPart -> Workbook -> WorksheetPart -> Worksheet -> SheetData
        /// </summary>
        /// <param name="table"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<string> Save(DataTable table, string path, string name)
        {
            if (File.Exists(path))
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(path, true))
                {
                    WorkbookPart book = document.WorkbookPart;

                    string relationship = book.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name.Equals(name))?.Id;

                    if (relationship != null)
                    {
                        Worksheet sheet = ((WorksheetPart)book.GetPartById(relationship)).Worksheet;

                        SheetData data = sheet.GetFirstChild<SheetData>();

                        Tabulate(table, data);

                        //TableDefinitionPart definition = sheet.AddNewPart<TableDefinitionPart>();
                        //definition.Table = Definition(name, table);
                    }
                    else
                    {
                        WorksheetPart sheet = book.AddNewPart<WorksheetPart>();
                        sheet.Worksheet = new Worksheet(new SheetData());

                        Sheets sheets = book.Workbook.GetFirstChild<Sheets>();

                        sheets.Append(new Sheet()
                        {
                            Id = book.GetIdOfPart(sheet),
                            SheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1,
                            Name = name
                        });

                        SheetData data = sheet.Worksheet.GetFirstChild<SheetData>();

                        Tabulate(table, data);

                        TableDefinitionPart definition = sheet.AddNewPart<TableDefinitionPart>(name);
                        definition.Table = Definition(name, table);
                    }

                    book.Workbook.Save();
                }
            }
            else
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart book = document.AddWorkbookPart();
                    book.Workbook = new Workbook();

                    WorksheetPart sheet = book.AddNewPart<WorksheetPart>();
                    sheet.Worksheet = new Worksheet(new SheetData());

                    TableDefinitionPart definition = sheet.AddNewPart<TableDefinitionPart>(name);
                    definition.Table = Definition(name, table);

                    Sheets sheets = document.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

                    sheets.Append(new Sheet()
                    {
                        Id = document.WorkbookPart.GetIdOfPart(sheet),
                        SheetId = 1,
                        Name = name
                    });

                    SheetData data = sheet.Worksheet.GetFirstChild<SheetData>();

                    Tabulate(table, data);

                    book.Workbook.Save();
                }
            }

            return "Exported to Excel file successfully";
        }

        private static void Tabulate(DataTable table, SheetData data)
        {
            Row header = new Row();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var cell = new Cell()
                {
                    CellValue = new CellValue(table.Columns[i].ToString()),
                    DataType = CellValues.String
                };

                header.AppendChild(cell);
            }

            data.Append(header);

            for (int y = 0; y < table.Rows.Count; y++)
            {
                Row row = new Row();

                for (int x = 0; x < table.Columns.Count; x++)
                {
                    var cell = new Cell()
                    {
                        CellValue = new CellValue(table.Rows[y][x].ToString()),
                        DataType = CellValues.String
                    };

                    row.AppendChild(cell);
                }

                data.AppendChild(row);
            }
        }

        private static Table Definition(string circuit, DataTable data)
        {
            string coordinates = string.Format("A1:P{0}", data.Rows.Count);

            TableColumns columns = new TableColumns()
            {
                Count = Convert.ToUInt32(data.Columns.Count)
            };

            for (int i = 1; i == data.Columns.Count; i++)
            {
                columns.Append(new TableColumn()
                {
                    Id = Convert.ToUInt32(i),
                    Name = data.Columns[i - 1].ColumnName
                });
            }

            Table table = new Table()
            {
                Id = 1,
                Name = data.TableName,
                DisplayName = data.TableName,
                Reference = coordinates
            };

            table.Append(new AutoFilter()
            {
                Reference = coordinates
            });

            table.Append(columns);

            string style = string.Empty;

            switch (circuit)
            {
                case "First":
                case "Eighth":
                case "Fifteenth":
                    style = "White, Table Style Medium 1";
                    break;
                case "Second":
                case "Ninth":
                case "Sixteenth":
                    style = "Blue, Table Style Medium 2";
                    break;
                case "Third":
                case "Tenth":
                case "Seventeenth":
                    style = "Orange, Table Style Medium 3";
                    break;
                case "Fourth":
                case "Eleventh":
                case "Eighteenth":
                    style = "White, Table Style Medium 4";
                    break;
                case "Fifth":
                case "Twelveth":
                case "Nineteenth":
                    style = "Gold, Table Style Medium 5";
                    break;
                case "Sixth":
                case "Thirteenth":
                case "Twentieth":
                    style = "Blue, Table Style Medium 6";
                    break;
                case "Seventh":
                case "Fourteenth":
                default:
                    style = "Green, Table Style Medium 7";
                    break;
            }

            table.Append(new TableStyleInfo()
            {
                Name = style,
                ShowRowStripes = true,
                //ShowFirstColumn = true,
                //ShowLastColumn = true,
                //ShowColumnStripes = false
            });

            return table;
        }
    }
}
