using DataWeb.Domain.Entities;

namespace DataWeb.Infrastructure.Repositories;

public interface IJobRepository
{
    Task<DataJudJob> CreateAsync(DataJudJob job);
    Task<DataJudJob?> GetByIdAsync(string id);
    Task<DataJudJob> UpdateAsync(DataJudJob job);
    Task DeleteAsync(string id);
    Task<IEnumerable<DataJudJob>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20);
    Task<int> CountByUserIdAsync(int userId);
    Task<IEnumerable<DataJudJob>> GetPendingJobsAsync();
    Task<IEnumerable<DataJudJob>> GetByStatusAsync(string status);
    Task<JobStatistics> GetUserStatisticsAsync(int userId);
}

public class JobStatistics
{
    public int TotalJobs { get; set; }
    public int JobsPendentes { get; set; }
    public int JobsProcessando { get; set; }
    public int JobsConcluidos { get; set; }
    public int JobsComErro { get; set; }
    public int TotalProcessosConsultados { get; set; }
    public DateTime? UltimoJob { get; set; }
}