using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SetSupply.Helper;

/// <summary>
/// Đọc / ghi bảng đơn giản (dòng 1 = tiêu đề) cho Import / Export: hỗ trợ .xlsx (ClosedXML, không cần cài Excel) và .csv (UTF-8).
/// </summary>
public static class SpreadsheetHelper
{
    public const string FileFilter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv";

    private static readonly string[] DateFormats =
    {
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "yyyy/MM/dd",
        "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "M/d/yyyy", "M/d/yyyy h:mm:ss tt"
    };

    /// <summary>
    /// Ghi bảng ra file. Giá trị DateTime ghi dạng ngày dd/MM/yyyy.
    /// </summary>
    public static void Write(string path, string sheetName, IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
    {
        if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(FormatValue(v)))));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); // BOM để Excel đọc đúng tiếng Việt
            return;
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (int c = 0; c < headers.Count; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        var headerRange = sheet.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4F96");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int r = 2;
        foreach (var row in rows)
        {
            for (int c = 0; c < row.Length; c++)
            {
                var cell = sheet.Cell(r, c + 1);
                switch (row[c])
                {
                    case null:
                        break;
                    case DateTime date:
                        cell.Value = date;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                        break;
                    case int number:
                        cell.Value = number;
                        break;
                    default:
                        cell.Value = row[c]!.ToString();
                        break;
                }
            }
            r++;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        foreach (var column in sheet.ColumnsUsed())
            column.Width = Math.Max(column.Width, 14);

        workbook.SaveAs(path);
    }

    /// <summary>
    /// Đọc file thành các dòng chuỗi (bỏ dòng tiêu đề, bỏ dòng trống). Ngày trong Excel trả về dạng yyyy-MM-dd.
    /// Mỗi phần tử: (số dòng trong file, các ô).
    /// </summary>
    public static List<(int RowNumber, string[] Cells)> Read(string path, int columnCount)
    {
        var result = new List<(int, string[])>();

        if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return result;
            char delimiter = lines[0].Count(ch => ch == ';') > lines[0].Count(ch => ch == ',') ? ';' : ',';

            for (int i = 1; i < lines.Length; i++)
            {
                var cells = ParseCsvLine(lines[i], delimiter);
                if (cells.All(string.IsNullOrWhiteSpace)) continue;
                result.Add((i + 1, Normalize(cells, columnCount)));
            }
            return result;
        }

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int r = 2; r <= lastRow; r++)
        {
            var cells = new string[columnCount];
            for (int c = 0; c < columnCount; c++)
            {
                var cell = sheet.Cell(r, c + 1);
                cells[c] = cell.DataType == XLDataType.DateTime
                    ? cell.GetDateTime().ToString("yyyy-MM-dd")
                    : cell.GetFormattedString().Trim();
            }
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            result.Add((r, cells));
        }
        return result;
    }

    public static bool TryParseDate(string text, out DateTime date)
    {
        text = text.Trim();
        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            date = date.Date;
            return true;
        }
        // Số serial ngày của Excel (VD 46306)
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 80000)
        {
            date = DateTime.FromOADate(serial).Date;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Số nguyên không âm; ô trống = 0. Chấp nhận dấu phân cách hàng nghìn "1,060" / "1.060".
    /// </summary>
    public static bool TryParseQuantity(string text, out int value)
    {
        text = text.Trim().Replace(",", "").Replace(".", "").Replace(" ", "");
        if (text.Length == 0)
        {
            value = 0;
            return true;
        }
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0;
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTime date => date.ToString("dd/MM/yyyy"),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', ';', '"', '\n', '\r' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string[] ParseCsvLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (inQuotes)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (ch == '"') inQuotes = false;
                else current.Append(ch);
            }
            else if (ch == '"') inQuotes = true;
            else if (ch == delimiter) { cells.Add(current.ToString()); current.Clear(); }
            else current.Append(ch);
        }
        cells.Add(current.ToString());
        return cells.Select(c => c.Trim()).ToArray();
    }

    private static string[] Normalize(string[] cells, int columnCount)
    {
        var result = new string[columnCount];
        for (int i = 0; i < columnCount; i++)
            result[i] = i < cells.Length ? cells[i] : string.Empty;
        return result;
    }
}
