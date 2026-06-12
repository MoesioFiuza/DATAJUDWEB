using DataWeb.Models;

namespace DataWeb.Services;

public static class ProcessarCnjsRequestValidator
{
    public static (IResult? Error, List<string> Cnjs) Validate(ProcessarCnjsRequest request, int maxCnjs)
    {
        var cnjs = request.Cnjs?
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .ToList() ?? [];

        if (cnjs.Count == 0)
        {
            return (Results.Problem(
                title: "Requisição inválida",
                detail: "Informe ao menos um CNJ no campo 'cnjs'.",
                statusCode: 400), cnjs);
        }

        if (cnjs.Count > maxCnjs)
        {
            return (Results.Problem(
                title: "Limite excedido",
                detail: $"Máximo de {maxCnjs} CNJs por requisição. Enviados: {cnjs.Count}.",
                statusCode: 400), cnjs);
        }

        return (null, cnjs);
    }
}
