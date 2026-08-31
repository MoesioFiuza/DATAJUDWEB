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

    public static string FormatarData(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
        var s = valor.Trim();

        if (DateTimeOffset.TryParse(s, out var dto))
            return dto.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");

        var digitos = SomenteDigitos(s);
        if (digitos.Length >= 14
            && DateTime.TryParseExact(
                digitos[..14],
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal,
                out var compacto))
        {
            return compacto.ToString("dd/MM/yyyy HH:mm:ss");
        }

        if (digitos.Length == 8
            && DateTime.TryParseExact(
                digitos,
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var soData))
        {
            return soData.ToString("dd/MM/yyyy");
        }

        return s;
    }

    public static string FormatarComplemento(string? descricao, string? nome, string? valor)
    {
        var rotulo = TraduzirChaveComplemento(descricao);
        var texto = (nome ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(texto))
            texto = (valor ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(rotulo) && string.IsNullOrEmpty(texto))
            return string.Empty;
        if (string.IsNullOrEmpty(rotulo))
            return texto;
        if (string.IsNullOrEmpty(texto))
            return rotulo;
        return $"{rotulo}: {texto}";
    }

    public static string TraduzirChaveComplemento(string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave)) return string.Empty;

        return chave.Trim().ToLowerInvariant() switch
        {
            "tipo_de_documento" => "Tipo de documento",
            "tipo_de_conclusao" => "Tipo de conclusão",
            "motivo_da_remessa" => "Motivo da remessa",
            "resultado" => "Resultado",
            "situacao_da_audiencia" => "Situação da audiência",
            "tipo_de_peca" => "Tipo de peça",
            "tipo_de_peticao" => "Tipo de petição",
            _ => HumanizarChave(chave.Trim())
        };
    }

    private static string HumanizarChave(string chave)
    {
        var partes = chave.Replace('-', '_').Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return chave;
        return string.Join(' ', partes.Select(p =>
            p.Length == 1
                ? p.ToUpperInvariant()
                : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }
}
