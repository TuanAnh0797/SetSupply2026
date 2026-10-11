using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SetSupply.Service;

/// <summary>
/// Kết quả đọc file kế hoạch: ngày -> (model, ca của app) -> số lượng.
/// </summary>
public sealed class PlanFileResult
{
    public string SheetName { get; init; } = string.Empty;
    public Dictionary<DateTime, Dictionary<(string Model, int Shift), int>> ByDate { get; } = new();
    public HashSet<string> Models { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Warnings { get; } = new();
    public int PlanRowCount { get; set; }
}

/// <summary>
/// Đọc file kế hoạch sản xuất (định dạng sheet "Daily revise"):
/// - Dòng tiêu đề có ô "Model" ở cột A, cột "Production Shifts" (Plan / Result), và mỗi ngày 1 khối cột
///   "Manual | Total | S.1 | S.2 | S.3". Ngày của khối nằm ở các dòng phía trên tiêu đề (giá trị ngày).
/// - Chỉ lấy dòng "Plan" của model bắt đầu bằng "NR-". Số lượng lấy ở S.1 / S.2 / S.3.
/// Ô có công thức lấy giá trị đã lưu sẵn trong file (không tính lại công thức => nhanh, không phụ thuộc file liên kết).
/// </summary>
public static class PlanFileParser
{
    public const string ModelPrefix = "NR-";
    private const int MaxHeaderSearchRows = 100;

    /// <param name="shiftMap">shiftMap[0..2] = ca của app cho cột S.1 / S.2 / S.3 (0 = bỏ qua).</param>
    /// <param name="sheetName">Tên sheet cần đọc (VD "Daily revise (3S)"), không phân biệt hoa/thường, bỏ khoảng trắng thừa.
    /// Để trống => tự dò sheet đầu tiên có đúng cấu trúc.</param>
    public static PlanFileResult Parse(Stream stream, IReadOnlyList<int> shiftMap, string? sheetName = DefaultSheetName)
    {
        using var workbook = new XLWorkbook(stream);
        var sheetNames = string.Join(", ", workbook.Worksheets.Select(s => $"\"{s.Name}\""));

        if (!string.IsNullOrWhiteSpace(sheetName))
        {
            var wanted = Normalize(sheetName);
            var sheet = workbook.Worksheets.FirstOrDefault(s => Normalize(s.Name) == wanted)
                ?? throw new InvalidDataException($"Không có sheet \"{sheetName.Trim()}\" trong file. Các sheet trong file: {sheetNames}.");

            var header = FindHeader(sheet)
                ?? throw new InvalidDataException($"Sheet \"{sheet.Name}\" không có dòng tiêu đề gồm cột \"Model\", \"Production Shifts\" và \"S.1 | S.2 | S.3\".");
            return ParseSheet(sheet, header.Row, header.ModelCol, header.TypeCol, shiftMap);
        }

        foreach (var sheet in workbook.Worksheets)
        {
            var header = FindHeader(sheet);
            if (header == null) continue;
            return ParseSheet(sheet, header.Value.Row, header.Value.ModelCol, header.Value.TypeCol, shiftMap);
        }

        throw new InvalidDataException($"Không sheet nào có dòng tiêu đề gồm cột \"Model\", \"Production Shifts\" và \"S.1 | S.2 | S.3\". Các sheet trong file: {sheetNames}.");
    }

    public const string DefaultSheetName = "Daily revise (3S)";

    /// <summary>
    /// So sánh tên sheet: bỏ khoảng trắng thừa, không phân biệt hoa/thường.
    /// </summary>
    private static string Normalize(string name) =>
        string.Join(" ", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static (int Row, int ModelCol, int TypeCol)? FindHeader(IXLWorksheet sheet)
    {
        var used = sheet.RangeUsed();
        if (used == null) return null;
        int lastRow = Math.Min(used.LastRow().RowNumber(), MaxHeaderSearchRows);
        int lastCol = used.LastColumn().ColumnNumber();

        for (int r = 1; r <= lastRow; r++)
        {
            int modelCol = 0, typeCol = 0;
            bool hasShift = false;
            for (int c = 1; c <= lastCol; c++)
            {
                var text = Text(sheet.Cell(r, c));
                if (modelCol == 0 && text.Equals("Model", StringComparison.OrdinalIgnoreCase)) modelCol = c;
                else if (typeCol == 0 && text.StartsWith("Production Shift", StringComparison.OrdinalIgnoreCase)) typeCol = c;
                else if (text.Equals("S.1", StringComparison.OrdinalIgnoreCase)) hasShift = true;
            }
            if (modelCol > 0 && typeCol > 0 && hasShift)
                return (r, modelCol, typeCol);
        }
        return null;
    }

    private static PlanFileResult ParseSheet(IXLWorksheet sheet, int headerRow, int modelCol, int typeCol, IReadOnlyList<int> shiftMap)
    {
        var result = new PlanFileResult { SheetName = sheet.Name };
        var used = sheet.RangeUsed()!;
        int lastRow = used.LastRow().RowNumber();
        int lastCol = used.LastColumn().ColumnNumber();

        // Các khối ngày: cột S.1 (S.2, S.3 liền sau) + ngày tìm ở các dòng phía trên
        var blocks = new List<(DateTime Date, int Col)>();
        for (int c = 1; c <= lastCol - 2; c++)
        {
            if (!Text(sheet.Cell(headerRow, c)).Equals("S.1", StringComparison.OrdinalIgnoreCase)) continue;
            if (!Text(sheet.Cell(headerRow, c + 1)).Equals("S.2", StringComparison.OrdinalIgnoreCase) ||
                !Text(sheet.Cell(headerRow, c + 2)).Equals("S.3", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"Cột {sheet.Cell(headerRow, c).Address.ColumnLetter}: sau S.1 không phải S.2, S.3 => bỏ qua");
                continue;
            }

            DateTime? date = null;
            for (int r = headerRow - 1; r >= 1 && date == null; r--)
                date = AsDate(sheet.Cell(r, c));

            if (date == null)
            {
                result.Warnings.Add($"Cột {sheet.Cell(headerRow, c).Address.ColumnLetter}: không tìm thấy ngày => bỏ qua");
                continue;
            }
            if (blocks.Any(b => b.Date == date.Value))
            {
                result.Warnings.Add($"Ngày {date:dd/MM/yyyy} xuất hiện nhiều lần (cột {sheet.Cell(headerRow, c).Address.ColumnLetter}) => chỉ lấy khối đầu tiên");
                continue;
            }
            blocks.Add((date.Value, c));
        }

        if (blocks.Count == 0)
            throw new InvalidDataException("Không tìm thấy khối ngày nào (cột S.1 | S.2 | S.3 có ngày ở phía trên).");

        // Các khối là các ngày liên tiếp. Ô ngày bị gõ sai (VD 14/12/2023 giữa tháng 10/2026) => sửa theo thứ tự
        for (int i = 1; i < blocks.Count; i++)
        {
            var expected = blocks[i - 1].Date.AddDays(1);
            bool nextConfirms = i + 1 >= blocks.Count || blocks[i + 1].Date == expected.AddDays(1);
            if (blocks[i].Date != expected && nextConfirms && !blocks.Any(b => b.Date == expected))
            {
                result.Warnings.Add($"Cột {sheet.Cell(headerRow, blocks[i].Col).Address.ColumnLetter}: ngày ghi trong file là " +
                                    $"{blocks[i].Date:dd/MM/yyyy}, không liên tiếp => dùng {expected:dd/MM/yyyy}");
                blocks[i] = (expected, blocks[i].Col);
            }
        }

        foreach (var block in blocks)
            result.ByDate[block.Date] = new Dictionary<(string, int), int>();

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var model = Text(sheet.Cell(r, modelCol));
            if (!model.StartsWith(ModelPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (!Text(sheet.Cell(r, typeCol)).Equals("Plan", StringComparison.OrdinalIgnoreCase)) continue;

            result.PlanRowCount++;
            result.Models.Add(model);

            foreach (var (date, col) in blocks)
            {
                for (int i = 0; i < 3; i++)
                {
                    int shift = i < shiftMap.Count ? shiftMap[i] : 0;
                    if (shift <= 0) continue;

                    int qty = AsInt(sheet.Cell(r, col + i));
                    if (qty == 0) continue;
                    if (qty < 0)
                    {
                        // VD S.3 = Manual - S.1 - S.2 bị âm khi S.1 nhập lớn hơn Manual. Vẫn cộng để tổng ngày khớp file.
                        result.Warnings.Add($"Dòng {r} ({model}) ngày {date:dd/MM}: {(i == 0 ? "S.1" : i == 1 ? "S.2" : "S.3")} = {qty} (âm)");
                    }

                    var day = result.ByDate[date];
                    var key = (model, shift);
                    day[key] = day.TryGetValue(key, out var existing) ? existing + qty : qty; // model lặp nhiều dòng => cộng dồn
                }
            }
        }

        if (result.PlanRowCount == 0)
            result.Warnings.Add($"Không có dòng \"Plan\" nào của model bắt đầu bằng \"{ModelPrefix}\".");

        return result;
    }

    // ===== Đọc ô (công thức => giá trị đã lưu) =====

    private static XLCellValue Value(IXLCell cell)
    {
        try { return cell.HasFormula ? cell.CachedValue : cell.Value; }
        catch { return Blank.Value; }
    }

    private static string Text(IXLCell cell)
    {
        var v = Value(cell);
        if (v.IsBlank) return string.Empty;
        if (v.IsText) return v.GetText().Trim();
        if (v.IsNumber) return v.GetNumber().ToString(CultureInfo.InvariantCulture);
        return v.ToString().Trim();
    }

    private static DateTime? AsDate(IXLCell cell)
    {
        var v = Value(cell);
        if (v.IsDateTime) return v.GetDateTime().Date;
        if (v.IsNumber)
        {
            var n = v.GetNumber();
            if (n >= 36526 && n <= 73051) return DateTime.FromOADate(n).Date; // năm 2000 .. 2100
        }
        return null;
    }

    private static int AsInt(IXLCell cell)
    {
        var v = Value(cell);
        if (v.IsNumber) return (int)Math.Round(v.GetNumber());
        if (v.IsText && double.TryParse(v.GetText().Replace(",", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return (int)Math.Round(d);
        return 0;
    }
}
