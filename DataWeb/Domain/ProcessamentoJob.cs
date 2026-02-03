namespace DataWeb.Domain;

public enum JobStatus
{
    Pendente,
    Processando,
    Concluido,
    Erro
}

public class ProcessamentoJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? UsuarioId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public long TamanhoArquivo { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pendente;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    public string? CaminhoResultado { get; set; }
    public string? CaminhoArquivoTemp { get; set; }
    public string? Erro { get; set; }
    public int TotalProcessos { get; set; }
    public int ProcessosProcessados { get; set; }
}
