## discord bot 

## creando el proyecto

```bash
dotnet new console -n AntiSpamBot
```

## nuget requeridos.

```bash
dotnet add package Discord.Net
```

## appsettings.json

```bash
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.Binder
```

## corriendo la aplicación

```bash
dotnet run
```

## notas, en caso de usar variables de entornos.

```
set DISCORD_BOT_TOKEN=TU_TOKEN_DE_DISCORD_AQUI
dotnet run

```PowerShell
$env:DISCORD_BOT_TOKEN="TU_TOKEN_DE_DISCORD_AQUI"
dotnet run
```

```
$env:DISCORD_BOT_TOKEN="mi token"
dotnet run
```

```
export DISCORD_BOT_TOKEN="TU_TOKEN_DE_DISCORD_AQUI"
dotnet run
```

