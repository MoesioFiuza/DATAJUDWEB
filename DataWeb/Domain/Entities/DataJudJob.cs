using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataWeb.Domain.Entities;

// Representa um job de processamento de processos judiciais no DataJud.
// Persistida no banco de dados MySQL.
[Table("DataJudJobs")]
public class DataJudJob
{
    // Identificador único do job
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // ID do usuário da intranet que criou o job.
    [Required]
    public int UserId { get; set; }

    // Nome do arquivo original enviado pelo usuário
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    // Tamanho do arquivo original em bytes
    public long OriginalFileSize { get; set; }

    // Status atual do job (pendente, processando, concluido, erro, cancelado)
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = DataJudJobStatus.Pendente;

    // Total de processos (CNJs) a serem consultados
    public int TotalProcessos { get; set; }

    // Quantidade de processos já processados
    public int ProcessosProcessados { get; set; }

    // Mensagem de erro caso o processamento falhe
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    // Caminho do arquivo resultado no servidor (após processamento)
    [MaxLength(500)]
    public string? ResultFilePath { get; set; }

    // Tamanho do arquivo resultado em bytes
    public long? ResultFileSize { get; set; }

    // Data/hora de criação do job (UTC)
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Data/hora de início do processamento (UTC)
    public DateTime? StartedAt { get; set; }

    // Data/hora de conclusão do processamento (UTC)
    public DateTime? CompletedAt { get; set; }

    // Percentual de progresso do processamento (0-100)
    [NotMapped]
    public int Progresso => TotalProcessos > 0
        ? (int)((double)ProcessosProcessados / TotalProcessos * 100)
        : 0;

    // Tempo total de processamento (se concluído)
    [NotMapped]
    public TimeSpan? TempoProcessamento => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    // Processos individuais consultados neste job (opcional, para detalhamento)
    public virtual ICollection<DataJudJobProcesso>? Processos { get; set; }
}

public static class DataJudJobStatus
{
    public const string Pendente = "pendente";
    public const string Processando = "processando";
    public const string Concluido = "concluido";
    public const string Erro = "erro";
    public const string Cancelado = "cancelado";
}
