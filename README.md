# DataWeb

## Configuracao local (.env)

Este projeto usa a chave `DataJud__ApiKey` via variavel de ambiente.

1. Crie o arquivo `.env` na raiz do repo (ja existe um template).
2. Preencha a chave:

```env
DataJud__ApiKey=SEU_TOKEN_AQUI
```

## Como carregar o .env localmente

### PowerShell (sessao atual)

```powershell
Get-Content .env | ForEach-Object {
  if ($_ -match '^(#|\s*$)') { return }
  $name, $value = $_ -split '=', 2
  if ($name) { [Environment]::SetEnvironmentVariable($name, $value, 'Process') }
}
```

Depois, rode o projeto normalmente.

### Alternativa (User Secrets)

```powershell
dotnet user-secrets init
 dotnet user-secrets set "DataJud:ApiKey" "SEU_TOKEN_AQUI"
```

Observacao: o ASP.NET Core aceita `DataJud__ApiKey` (env var) e `DataJud:ApiKey` (user secrets).