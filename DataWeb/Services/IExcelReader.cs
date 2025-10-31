namespace DataWeb.Services;

public interface IExcelReader
{
    List<string> LerCnjs(Stream xlsxStream, int coluna = 1, int linhaInicial = 1);
}