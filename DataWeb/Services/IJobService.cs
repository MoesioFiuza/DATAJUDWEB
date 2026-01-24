using DataWeb.Domain;

namespace DataWeb.Services;

public interface IJobService
{
    Task<string> CriarJobAsync(string? usuarioId, string nomeArquivo, long tamanhoArquivo, Stream arquivoStream);
    Task<ProcessamentoJob?> ObterJobAsync(string jobId);
    Task<IEnumerable<ProcessamentoJob>> ListarJobsPorUsuarioAsync(string? usuarioId);
    Task<bool> MarcarJobComoProcessandoAsync(string jobId);
    Task<bool> MarcarJobComoConcluidoAsync(string jobId, string caminhoResultado, int totalProcessos);
    Task<bool> MarcarJobComoErroAsync(string jobId, string erro);
    Task AtualizarProgressoAsync(string jobId, int processados, int total);
    
    byte[]? ObterArquivoTemporario(string jobId);
    void RemoverArquivoTemporario(string jobId);
}