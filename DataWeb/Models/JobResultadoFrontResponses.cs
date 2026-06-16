namespace DataWeb.Models;

public record JobResultadoResumoResponse(
    string JobId,
    int TotalCnjsEnviados,
    int TotalProcessos,
    int TotalMovimentacoes,
    JobResultadoLinks Links);

public record JobResultadoLinks(
    string Processos,
    string Excel);

public record PaginatedResponse<T>(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<T> Items);

public record ProcessoListaItem(
    string NumeroProcesso,
    string Tribunal,
    string Grau,
    string RamoJustica,
    string NivelSigilo,
    string ClasseNome,
    string OrgaoNome,
    string DataAjuizamento,
    string Assuntos,
    int TotalMovimentacoes,
    string? MovimentacaoMaisRecente,
    string? DataMovimentacaoMaisRecente);

public record ProcessoDetalheResponse(
    string NumeroProcesso,
    string IdDatajud,
    string Tribunal,
    string Grau,
    string RamoJustica,
    string NivelSigilo,
    string ClasseCodigo,
    string ClasseNome,
    string SistemaNome,
    string FormatoNome,
    string OrgaoCodigo,
    string OrgaoNome,
    string OrgaoMunicipioIbge,
    string DataAjuizamento,
    string Assuntos,
    int TotalMovimentacoes,
    string MovimentacoesUrl);

public record MovimentacaoItem(
    int Ordem,
    string DataHora,
    string Codigo,
    string Nome,
    string OrgaoCodigo,
    string OrgaoNome,
    string Complementos,
    bool EhMaisRecente);
