namespace DataWeb.Utils;

public static class CnjFormat
{
    public static string FormatarNumero(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero)) return string.Empty;
        var limpo = numero.Replace(".", "").Replace("-", "").Trim();
        return limpo.PadLeft(20, '0');
    }
}