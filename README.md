## discord bot 
bot para eliminar mensajes no deseados siguiendo un patron de archivos adjuntos determinado.


## creando el proyecto

```bash
dotnet new console -n AntiSpamBot
cd AntiSpamBot
```

## nuget requeridos.

```bash
dotnet add package Discord.Net
```

## appsettings.json

```bash
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Binder --version 8.0.0
```

para revisar las versiones existentes
```
(Invoke-RestMethod https://api.nuget.org/v3-flatcontainer/microsoft.extensions.configuration.binder/index.json).versions
(Invoke-RestMethod https://api.nuget.org/v3-flatcontainer/Microsoft.Extensions.Configuration.Binder/index.json).versions
(Invoke-RestMethod https://api.nuget.org/v3-flatcontainer/Discord.Net/index.json).versions
```

```
dotnet nuget locals all --clear
dotnet restore
```

## crear y editar el ./AntiSpamBot/appsettins.jon
```json
{
  "Discord": {
    "Token": "TOKEN"
  }
}
```

## corriendo la aplicación

```bash
dotnet run
```

## notas, en caso de usar variables de entornos.

```
set DISCORD_BOT_TOKEN=TOKEN
dotnet run

```PowerShell
$env:DISCORD_BOT_TOKEN="TOKEN"
dotnet run
```

```
$env:DISCORD_BOT_TOKEN="TOKEN"
dotnet run
```

```
export DISCORD_BOT_TOKEN="TOKEN"
dotnet run
```

