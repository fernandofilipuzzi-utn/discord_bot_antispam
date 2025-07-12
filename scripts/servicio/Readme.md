

# Configuración e instalación script systemd



## instalación del script
```bash
sudo nano /etc/systemd/system/bot.service
```

## ver la ruta donde esta dotnet
```bash
which  dotnet
```

### generar el publish, ver ruta para el script
```bash
dotnet publish -c Release -r linux-arm -o ./publish
```

##  recarga la configuración del systemd
```bash
sudo systemctl daemon-reload
```

## habilita el servicio
```bash
sudo systemctl enable antispambot.service
```

## inicia el servicio
```bash
sudo systemctl start antispambot.service
```

## muestra log en tiempo real
```bash
sudo systemctl status antispambot.service
```
