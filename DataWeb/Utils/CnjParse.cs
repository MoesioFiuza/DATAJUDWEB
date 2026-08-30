using System.Text.RegularExpressions;

namespace DataWeb.Utils;

public static class CnjParse
{
    static readonly Regex CnjCodigoRegex = new(@"\.(\d\.\d{2})\.", RegexOptions.Compiled);

    public static bool TryObterCodigo(string cnj, out string codigo)
    {
        var m = CnjCodigoRegex.Match(cnj ?? string.Empty);
        if (m.Success)
        {
            codigo = m.Groups[1].Value;
            return true;
        }

        var digits = CnjFormat.SomenteDigitos(cnj ?? string.Empty);
        if (digits.Length == 20)
        {
            codigo = $"{digits[13]}.{digits[14]}{digits[15]}";
            return true;
        }

        codigo = "";
        return false;
    }
}
