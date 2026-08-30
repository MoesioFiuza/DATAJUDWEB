using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataWeb.Domain.Entities;

// Representa um processo individual dentro de um job.
// Para rastrear quais processos foram encontrados, não encontrados ou deram erro.
[Table("DataJudJobProcessos")]
public class DataJudJobProcesso
{
    // Identificador único auto-incrementado
    [Key]
    public long Id { get; set; }

    // ID do job pai (relacionamento)
    [Required]
    [MaxLength(36)]
    public string JobId { get; set; } = string.Empty;

    // Número do processo no formato CNJ (ex: 0000001-23.2024.8.26.0100)
    [Required]
    [MaxLength(25)]
    public string NumeroCNJ { get; set; } = string.Empty;

    // Status do processamento individual deste processo
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = DataJudProcessoStatus.Pendente;

    // Código do tribunal extraído do CNJ
    [MaxLength(10)]
    public string? CodigoTribunal { get; set; }

    // Nome do tribunal (ex: TJSP, TRF3, etc.)
    [MaxLength(200)]
    public string? NomeTribunal { get; set; }

    // Mensagem de erro específica deste processo (se houver)
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    // Data/hora do processamento deste processo específico
    public DateTime? ProcessedAt { get; set; }

    // Quantidade de movimentações encontradas para este processo
    public int? TotalMovimentacoes { get; set; }

    // Job pai ao qual este processo pertence
    [ForeignKey(nameof(JobId))]
    public virtual DataJudJob? Job { get; set; }
}

public static class DataJudProcessoStatus
{
    public const string Pendente = "pendente";
    public const string Encontrado = "encontrado";
    public const string NaoEncontrado = "nao_encontrado";
    public const string Erro = "erro";
    public const string Invalido = "invalido";
}
