namespace DataWeb.Services;

public class ProcessamentoOptions
{
    public int MaxCnjsApi { get; set; } = 500;
    public int Paralelismo { get; set; } = 20;
    public int MaxConcorrencia { get; set; } = 2;
}
