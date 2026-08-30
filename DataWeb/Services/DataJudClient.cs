using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using DataWeb.Domain;
using DataWeb.Url;
using DataWeb.Utils;
using Microsoft.Extensions.Options;

namespace DataWeb.Services;

public class DataJudClient : IDataJudClient
{
    private readonly IHttpClientFactory _http;
    private readonly IOptions<DataJudOptions> _opt;
    private readonly ILogger<DataJudClient> _log;

    public DataJudClient(IHttpClientFactory http, IOptions<DataJudOptions> opt, ILogger<DataJudClient> log)
    {
        _http = http; _opt = opt; _log = log;
    }

    public async Task<IReadOnlyList<RespostaCnj>> ConsultarPorEstadoAsync(
        string estado,
        IEnumerable<string> cnjs,
        int paralelismo,
        CancellationToken ct,
        Action? onCnjConsultado = null)
    {
        var lista = cnjs.ToList();
        if (!UrlEndpoints.PorEstado.TryGetValue(estado, out var url) || string.IsNullOrWhiteSpace(url))
        {
            return lista
                .Select(cnj => RespostaCnj.Falha(cnj, $"Endpoint DataJud não configurado para {estado}"))
                .ToList();
        }

        url = url.Trim();
        var client = _http.CreateClient();
        var keyLen = _opt.Value.ApiKey?.Trim().Length ?? 0;
        _log.LogInformation("Usando ApiKey (len): {Len} para estado {Estado}", keyLen, estado);
        var respostas = new ConcurrentBag<RespostaCnj>();
        var sem = new SemaphoreSlim(Math.Max(1, paralelismo));
        var tasks = new List<Task>();
        foreach (var cnj in lista)
        {
            await sem.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var n = CnjFormat.FormatarNumero(cnj);
                    if (string.IsNullOrEmpty(n))
                    {
                        respostas.Add(RespostaCnj.Invalido(cnj, "CNJ vazio após normalização"));
                        return;
                    }

                    var payload = new
                    {
                        query = new
                        {
                            match = new Dictionary<string, string>
                            {
                                ["numeroProcesso"] = n
                            }
                        }
                    };

                    using var req = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = null
                        })
                    };

                    var apiKey = _opt.Value.ApiKey?.Trim();
                    req.Headers.Accept.Clear();
                    req.Headers.Accept.ParseAdd("application/json");
                    if (!string.IsNullOrWhiteSpace(apiKey))
                        req.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {apiKey}");

                    using var resp = await client.SendAsync(req, ct);
                    var content = await resp.Content.ReadAsStringAsync(ct);
                    var status = (int)resp.StatusCode;

                    _log.LogInformation("Estado {Estado} CNJ {CNJ} -> {Status}", estado, cnj, status);

                    if (!resp.IsSuccessStatusCode)
                    {
                        respostas.Add(RespostaCnj.Falha(
                            cnj,
                            $"HTTP {status} na consulta ao DataJud",
                            status,
                            content));
                        return;
                    }

                    respostas.Add(RespostaCnj.Ok(cnj, content, status));
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    respostas.Add(RespostaCnj.Falha(cnj, "Tempo esgotado na consulta ao DataJud"));
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Erro consultando {Estado} para {CNJ}", estado, cnj);
                    respostas.Add(RespostaCnj.Falha(cnj, ex.Message));
                }
                finally
                {
                    sem.Release();
                    onCnjConsultado?.Invoke();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
        return respostas.ToList();
    }
}
