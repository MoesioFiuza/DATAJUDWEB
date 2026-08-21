namespace DataWeb.Utils;

public static class CnjFormat
{
    public static string SomenteDigitos(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero)) return string.Empty;
        return new string(numero.Where(char.IsDigit).ToArray());
    }

    public static string ChaveCnj(string numero)
    {
        var digits = SomenteDigitos(numero);
        return digits.Length == 20 ? digits : (numero ?? string.Empty).Trim();
    }

    public static string FormatarNumero(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero)) return string.Empty;
        var limpo = numero.Replace(".", "").Replace("-", "").Trim();
        return limpo.PadLeft(20, '0');
    }

    public static string FormatarComMascara(string numero)
    {
        var digits = SomenteDigitos(numero);
        if (digits.Length != 20) return string.IsNullOrWhiteSpace(numero) ? string.Empty : numero.Trim();

        return $"{digits[..7]}-{digits[7..9]}.{digits[9..13]}.{digits[13]}.{digits[14..16]}.{digits[16..20]}";
    }

    public static string ObterRamoJustica(string numero)
    {
        var digits = FormatarNumero(numero);
        if (digits.Length != 20) return string.Empty;

        return digits[13] switch
        {
            '1' => "Justiça Militar da União",
            '2' => "Justiça Militar Estadual",
            '3' => "Justiça Militar",
            '4' => "Justiça Federal",
            '5' => "Justiça do Trabalho",
            '6' => "Justiça Eleitoral",
            '7' => "Justiça Militar",
            '8' => "Justiça Estadual",
            '9' => "Justiça Militar",
            _ => "Não identificado"
        };
    }

    public static string ObterGrauTratado(string grau)
    {
        if (string.IsNullOrWhiteSpace(grau)) return string.Empty;

        return grau.Trim().ToUpperInvariant() switch
        {
            "G1" => "1º grau",
            "G2" => "2º grau",
            "JE" => "Juizados Especiais",
            "TR" => "Turma Recursal",
            "SUP" => "Tribunal Superior",
            "TRU" => "Turma Regional de Uniformização",
            "TNU" => "Turma Nacional de Uniformização",
            "TEU" => "Turma Estadual de Uniformização",
            "CJF" => "Conselho da Justiça Federal",
            "CSJT" => "Conselho Superior da Justiça do Trabalho",
            _ => grau
        };
    }

    public static string ObterNivelSigiloTratado(string nivel)
    {
        if (string.IsNullOrWhiteSpace(nivel)) return string.Empty;

        return nivel.Trim() switch
        {
            "0" => "Público",
            "1" => "Segredo de justiça",
            "2" => "Sigilo mínimo",
            "3" => "Sigilo médio",
            "4" => "Sigilo intenso",
            "5" => "Sigilo absoluto",
            _ => nivel
        };
    }
}
