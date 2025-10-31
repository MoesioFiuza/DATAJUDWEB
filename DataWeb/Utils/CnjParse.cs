using System.Text.RegularExpressions;

namespace DataWeb.Utils;

public static class CnjParse
{
    static readonly Regex CnjCodigoRegex = new(@"\.(\d\.\d{2})\.", RegexOptions.Compiled);

    public static bool TryObterCodigo(string cnj, out string codigo)
    {
        var m = CnjCodigoRegex.Match(cnj);
        if (m.Success)
        {
            codigo = m.Groups[1].Value; 
            return true;
        }
        codigo = "";
        return false;
    }
}