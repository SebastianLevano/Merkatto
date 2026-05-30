# Merkatto Desktop — Runbook de publicación

## Flujo completo para publicar una versión nueva

```
[Código listo] → publish-win.ps1 → [releases/] → GitHub Release → [clientes se actualizan solos]
```

---

## 1. Prerequisitos (instalar una vez)

```powershell
# En Windows (máquina de build) o WSL
dotnet tool install -g vpk
node --version  # >= 20
```

Para publicar a GitHub Releases, necesitás tener el `gh` CLI:
```powershell
winget install GitHub.cli
gh auth login
```

---

## 2. Configurar el feed de updates

En `backend/src/Merkatto.Desktop/appsettings.json`, el campo `Updates.FeedUrl` determina
de dónde la app chequea updates. Opciones:

| Feed | FeedUrl | Costo |
|---|---|---|
| GitHub Releases (recomendado) | `https://github.com/TU_USUARIO/merkatto/releases/` | Gratis |
| Backblaze B2 | `https://f001.backblazeb2.com/file/tu-bucket/` | Desde $0.006/GB |
| Self-hosted | `https://tu-dominio.com/merkatto-releases/` | Tu VPS |

Para GitHub Releases, el repo puede ser privado si usás un token:
```
FeedUrl = "https://github.com/TU_USUARIO/merkatto/releases/"
```
*(Velopack usa la API pública de GitHub Releases, no requiere autenticación para repos públicos.)*

---

## 3. Publicar una versión nueva

### En Windows (producción)

```powershell
# Desde la raíz del repo
.\desktop\publish-win.ps1 `
    -Version 1.0.1 `
    -FeedUrl "https://github.com/TU_USUARIO/merkatto/releases/"
```

Esto genera en `desktop/releases/`:
- `MerkattoSetup.exe` — instalador para nuevos clientes
- `Merkatto-1.0.1-win-full.nupkg` — paquete full para el feed
- `Merkatto-1.0.1-win-delta.nupkg` — paquete delta (descarga mínima para clients que ya tienen 1.0.0)
- `RELEASES-win` — índice del feed

### Subir al feed (GitHub Releases)

```powershell
# Crear el release en GitHub
gh release create v1.0.1 `
    --title "v1.0.1" `
    --notes "Qué cambió en esta versión" `
    desktop/releases/*

# Velopack descarga automáticamente de la API de GitHub Releases
```

---

## 4. Cómo viven los updates en el cliente

1. La app arranca → `VelopackApp.Build().Run()` verifica si hay una actualización pendiente (Velopack hook)
2. En background, `UpdateManager.CheckForUpdatesAsync()` consulta el feed
3. Si hay update → `DownloadUpdatesAsync()` lo baja silenciosamente
4. El banner "Actualización descargada" aparece en la UI
5. Cuando el usuario cierra la app → `ApplyUpdatesAndRestart()` instala la nueva versión y reinicia

El cliente **nunca necesita hacer nada manualmente**. La actualización se aplica sola al reiniciar.

---

## 5. Primer instalador para un cliente nuevo

1. En `desktop/releases/`, tomá el `MerkattoSetup.exe`
2. Copiarlo a una USB o compartirlo por Drive
3. El cliente corre `MerkattoSetup.exe` → instala en `%LocalAppData%\Merkatto\`
4. Al abrir por primera vez, aparece el **wizard de primer arranque** (Paso 3)
5. El cliente completa su email y contraseña temporal
6. Listo

### client.json (opcional, para pre-rellenar el wizard)

Podés dejar un `client.json` junto al `.exe` del instalador para pre-rellenar:

```json
{
  "businessName": "Bodega Martita",
  "adminEmail": "martita@gmail.com"
}
```

Copiarlo a la carpeta de instalación (`%LocalAppData%\Merkatto\app-1.0.0\`) después de instalar.

---

## 6. Backup pre-migración automático

Antes de cada migración de DB (futura), la app copia el `.db` a `%ProgramData%\Merkatto\backups\`.
Si una migración falla, podés restaurar copiando el backup de vuelta.

Backups manuales: **Configuración → Exportar respaldo** (llama a `POST /api/v1/setup/backup`).

---

## 7. Rollback

Velopack guarda la versión anterior en `%LocalAppData%\Merkatto\`. Para hacer rollback:

```powershell
# En la PC del cliente (como admin)
cd %LocalAppData%\Merkatto
.\Update.exe --processStartAndWait=Merkatto.Desktop.exe --forceLatest
```

O manualmente: renombrar la carpeta `app-1.0.1` a `app-1.0.1.bak` y `app-1.0.0` de vuelta a la activa.

---

## 8. Versioning

| Campo | Dónde | Qué es |
|---|---|---|
| `PackageVersion` | `Merkatto.Desktop.csproj` | Versión SemVer del paquete Velopack |
| `AssemblyVersion` | `Merkatto.Desktop.csproj` | Versión del binario .NET |
| `-Version` en script | `publish-win.ps1` parámetro | Sobreescribe ambas en publish |

Bump la versión en el `.csproj` Y en el script. Usar SemVer: `MAJOR.MINOR.PATCH`.
