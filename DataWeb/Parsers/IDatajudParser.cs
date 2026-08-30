using DataWeb.Domain;

namespace DataWeb.Parsers;

public interface IDatajudParser
{
    List<LinhaProcesso> ExtrairLinhas(IEnumerable<RespostaCnj> respostas);
}
