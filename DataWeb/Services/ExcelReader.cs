using ClosedXML.Excel;

namespace DataWeb.Services;

public class ExcelReader : IExcelReader
{
    public List<string> LerCnjs(Stream xlsxStream, int coluna = 1, int linhaInicial = 2)
    {
        var cnjs = new List<string>();
        using var wb = new XLWorkbook(xlsxStream);
        var ws = wb.Worksheets.First();

        foreach (var row in ws.RowsUsed().Skip(linhaInicial - 1))
        {
            var cnj = row.Cell(coluna).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(cnj))
                cnjs.Add(cnj);
        }

        return cnjs;
    }
}