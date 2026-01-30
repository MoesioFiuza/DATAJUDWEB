using DataWeb.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataWeb.Infrastructure.Data.Configurations;

public class DataJudJobProcessoConfiguration : IEntityTypeConfiguration<DataJudJobProcesso>
{
    public void Configure(EntityTypeBuilder<DataJudJobProcesso> builder)
    {
        // Nome da tabela
        builder.ToTable("DataJudJobProcessos");

        // Chave primária auto-incrementada
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // JobId - FK para DataJudJob
        builder.Property(e => e.JobId)
            .HasMaxLength(36)
            .IsRequired();

        // NumeroCNJ - número do processo
        builder.Property(e => e.NumeroCNJ)
            .HasMaxLength(25)
            .IsRequired();

        // Status do processo individual
        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .HasDefaultValue(DataJudProcessoStatus.Pendente)
            .IsRequired();

        // CodigoTribunal
        builder.Property(e => e.CodigoTribunal)
            .HasMaxLength(10);

        // NomeTribunal
        builder.Property(e => e.NomeTribunal)
            .HasMaxLength(200);

        // ErrorMessage
        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        // ProcessedAt
        builder.Property(e => e.ProcessedAt);

        // TotalMovimentacoes
        builder.Property(e => e.TotalMovimentacoes);

        // RELACIONAMENTO com DataJudJob
        builder.HasOne(e => e.Job)
            .WithMany(j => j.Processos)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Cascade); // Ao deletar job, deleta processos

        // Índice por JobId - para listar processos de um job
        builder.HasIndex(e => e.JobId)
            .HasDatabaseName("IX_DataJudJobProcessos_JobId");

        // Índice por NumeroCNJ - para buscar por número de processo
        builder.HasIndex(e => e.NumeroCNJ)
            .HasDatabaseName("IX_DataJudJobProcessos_NumeroCNJ");

        // Índice por Status - para filtrar por status
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_DataJudJobProcessos_Status");
    }
}
