using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataWeb.Infrastructure.Repositories;

// Implementação do repositório de Jobs usando Entity Framework Core com MySQL.
// Esta classe abstrai todas as operações de banco de dados para Jobs.
public class JobRepository : IJobRepository
{
    private readonly DataWebDbContext _context;

    public JobRepository(DataWebDbContext context)
    {
        _context = context;
    }

    public async Task<DataJudJob> CreateAsync(DataJudJob job)
    {
        // Garante que o ID seja gerado se não foi informado
        if (string.IsNullOrEmpty(job.Id))
        {
            job.Id = Guid.NewGuid().ToString();
        }

        // Define data de criação
        job.CreatedAt = DateTime.UtcNow;

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return job;
    }

    public async Task<DataJudJob?> GetByIdAsync(string id)
    {
        return await _context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<DataJudJob> UpdateAsync(DataJudJob job)
    {
        // Verificar se já existe uma entidade com o mesmo Id sendo rastreada
        var existingEntry = _context.ChangeTracker
            .Entries<DataJudJob>()
            .FirstOrDefault(e => e.Entity.Id == job.Id);

        if (existingEntry != null)
        {
            // Se já existe, atualizar os valores da entidade rastreada
            existingEntry.CurrentValues.SetValues(job);
        }
        else
        {
            // Se não existe, anexar e marcar como modificado
            _context.Jobs.Attach(job);
            _context.Entry(job).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
        return job;
    }

    public async Task DeleteAsync(string id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job != null)
        {
            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<DataJudJob>> GetByUserIdAsync(
        int userId,
        int page = 1,
        int pageSize = 20)
    {
        return await _context.Jobs
            .AsNoTracking()
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId)
    {
        return await _context.Jobs
            .CountAsync(j => j.UserId == userId);
    }

    public async Task<IEnumerable<DataJudJob>> GetPendingJobsAsync()
    {
        return await _context.Jobs
            .AsNoTracking()
            .Where(j => j.Status == DataJudJobStatus.Pendente)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataJudJob>> GetByStatusAsync(string status)
    {
        return await _context.Jobs
            .AsNoTracking()
            .Where(j => j.Status == status)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<JobStatistics> GetUserStatisticsAsync(int userId)
    {
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.UserId == userId)
            .ToListAsync();

        return new JobStatistics
        {
            TotalJobs = jobs.Count,
            JobsPendentes = jobs.Count(j => j.Status == DataJudJobStatus.Pendente),
            JobsProcessando = jobs.Count(j => j.Status == DataJudJobStatus.Processando),
            JobsConcluidos = jobs.Count(j => j.Status == DataJudJobStatus.Concluido),
            JobsComErro = jobs.Count(j => j.Status == DataJudJobStatus.Erro),
            TotalProcessosConsultados = jobs.Sum(j => j.ProcessosProcessados),
            UltimoJob = jobs.MaxBy(j => j.CreatedAt)?.CreatedAt
        };
    }
}