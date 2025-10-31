using DataWeb.Domain;

namespace DataWeb.Exporters;

public interface IExcelExporter
{
    byte[] GerarExcel(List<LinhaProcesso> linhas);
}