namespace DataWeb.Domain;

/// <summary>
/// Resultado bruto da consulta de um CNJ no DataJud (antes do parse).
/// </summary>
public sealed class RespostaCnj
{
    public required string Cnj { get; init; }
    public string? Json { get; init; }
    public string? Erro { get; init; }
    public int? HttpStatus { get; init; }

    /// <summary>
    /// ok = JSON recebido; erro = falha na consulta; invalido = nem chegou a consultar.
    /// </summary>
    public string StatusPrevio { get; init; } = "ok";

    public static RespostaCnj Ok(string cnj, string json, int httpStatus) => new()
    {
        Cnj = cnj,
        Json = json,
        HttpStatus = httpStatus,
        StatusPrevio = "ok"
    };

    public static RespostaCnj Falha(string cnj, string erro, int? httpStatus = null, string? json = null) => new()
    {
        Cnj = cnj,
        Json = json,
        Erro = erro,
        HttpStatus = httpStatus,
        StatusPrevio = DataWeb.Domain.Entities.DataJudProcessoStatus.Erro
    };

    public static RespostaCnj Invalido(string cnj, string motivo) => new()
    {
        Cnj = cnj,
        Erro = motivo,
        StatusPrevio = DataWeb.Domain.Entities.DataJudProcessoStatus.Invalido
    };
}
