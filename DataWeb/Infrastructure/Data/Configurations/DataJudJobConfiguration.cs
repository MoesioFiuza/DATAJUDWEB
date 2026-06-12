using DataWeb.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataWeb.Infrastructure.Data.Configurations;

public class DataJudJobConfiguration : IEntityTypeConfiguration<DataJudJob>
{
    public void Configure(EntityTypeBuilder<DataJudJob> builder)
    {
        // Nome da tabela
        builder.ToTable("DataJudJobs");

        // Chave primária
        builder.HasKey(e => e.Id);

        // Configuração do ID
        builder.Property(e => e.Id)
            .HasMaxLength(36)
            .IsRequired();

        // UserId obrigatório (referencia usuário da intranet)
        builder.Property(e => e.UserId)
            .IsRequired();

        // Nome do arquivo original
        builder.Property(e => e.FileName)
            .HasMaxLength(255)
            .IsRequired();

        // Tamanho do arquivo
        builder.Property(e => e.OriginalFileSize)
            .IsRequired();

        // Status
        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .HasDefaultValue(DataJudJobStatus.Pendente)
            .IsRequired();

        // TotalProcessos
        builder.Property(e => e.TotalProcessos)
            .HasDefaultValue(0);

        // ProcessosProcessados
        builder.Property(e => e.ProcessosProcessados)
            .HasDefaultValue(0);

        // ErrorMessage
        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.JobKind)
            .HasMaxLength(20)
            .HasDefaultValue(DataJudJobKind.Xlsx)
            .IsRequired();

        builder.Property(e => e.InputCnjsJson)
            .HasColumnType("longtext");

        builder.Property(e => e.ResultJson)
            .HasColumnType("longtext");

        // Caminho do resultado
        builder.Property(e => e.ResultFilePath)
            .HasMaxLength(500);

        // ResultFileSize
        builder.Property(e => e.ResultFileSize);

        // Data de criação
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // StartedAt
        builder.Property(e => e.StartedAt);

        // CompletedAt
        builder.Property(e => e.CompletedAt);

        // Índice por UserId - para listar jobs do usuário
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_DataJudJobs_UserId");

        // Índice por Status - para buscar jobs pendentes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_DataJudJobs_Status");

        // Índice por CreatedAt - para ordenação
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_DataJudJobs_CreatedAt");

        // Índice composto UserId + CreatedAt - para listagem paginada do usuário
        builder.HasIndex(e => new { e.UserId, e.CreatedAt })
            .HasDatabaseName("IX_DataJudJobs_UserId_CreatedAt");

        builder.HasIndex(e => new { e.JobKind, e.Status })
            .HasDatabaseName("IX_DataJudJobs_JobKind_Status");

        // IGNORAR propriedades calculadas
        builder.Ignore(e => e.Progresso);
        builder.Ignore(e => e.TempoProcessamento);
    }
}
