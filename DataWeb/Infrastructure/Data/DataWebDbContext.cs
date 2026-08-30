using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DataWeb.Infrastructure.Data;

public class DataWebDbContext : DbContext
{
    public DataWebDbContext(DbContextOptions<DataWebDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataJudJob> Jobs { get; set; }

    public DbSet<DataJudJobProcesso> JobProcessos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configurações das entidades
        modelBuilder.ApplyConfiguration(new DataJudJobConfiguration());
        modelBuilder.ApplyConfiguration(new DataJudJobProcessoConfiguration());
    }
}
