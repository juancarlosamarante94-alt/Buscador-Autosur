$ErrorActionPreference = "Stop"
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$installRoot = Join-Path $env:LOCALAPPDATA "Buscador VIN Autosur"
$install = Join-Path $installRoot "app"
$stage = Join-Path $installRoot ("preparando_" + [Guid]::NewGuid().ToString("N"))
$backup = Join-Path $installRoot "respaldo_anterior"
$desktop = [Environment]::GetFolderPath("Desktop")

# Evita que PowerShell mantenga bloqueada la carpeta "app" por usarla como ubicacion actual.
Set-Location ([IO.Path]::GetTempPath())

function Stop-BuscadorAutosur {
    $deadline = (Get-Date).AddSeconds(20)
    do {
        $targets = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
            $_.ProcessName -eq "Buscador Autosur" -or
            ($_.Path -and $_.Path.StartsWith($installRoot, [StringComparison]::OrdinalIgnoreCase))
        })
        if ($targets.Count -eq 0) { return }
        $targets | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    throw "La aplicacion anterior no termino de cerrarse. Cerra Buscador Autosur y volve a intentar."
}

function Invoke-WithRetry {
    param(
        [Parameter(Mandatory=$true)][scriptblock]$Action,
        [Parameter(Mandatory=$true)][string]$Description,
        [int]$Attempts = 15
    )
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            & $Action
            return
        }
        catch {
            if ($attempt -eq $Attempts) {
                throw "$Description Windows mantuvo un archivo en uso despues de $Attempts intentos."
            }
            [GC]::Collect()
            [GC]::WaitForPendingFinalizers()
            Start-Sleep -Milliseconds 700
        }
    }
}

try {
    New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
    Stop-BuscadorAutosur

    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    $legacy = Get-ChildItem (Join-Path $installRoot "versiones") -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | Select-Object -First 1

    foreach ($dataName in @("vehiculos.dat", "clientes.dat")) {
        $sourceData = Join-Path $source $dataName
        $currentData = Join-Path $install $dataName
        $legacyData = if ($legacy) { Join-Path $legacy.FullName $dataName } else { $null }
        if (Test-Path $sourceData) { Copy-Item -Force $sourceData (Join-Path $stage $dataName) }
        elseif (Test-Path $currentData) { Copy-Item -Force $currentData (Join-Path $stage $dataName) }
        elseif ($legacyData -and (Test-Path $legacyData)) { Copy-Item -Force $legacyData (Join-Path $stage $dataName) }
        else { throw "No se encontro $dataName. Para la primera instalacion usa el instalador completo." }
    }

    Copy-Item -Force (Join-Path $source "icono_autosur.ico") (Join-Path $stage "icono_autosur.ico")
    Copy-Item -Force (Join-Path $source "lib\*.dll") $stage
    $sourceCode = (Get-Content -Raw -Encoding UTF8 (Join-Path $source "BuscadorVIN.cs")) + "`r`n" +
                  (Get-Content -Raw -Encoding UTF8 (Join-Path $source "Turnero.cs"))
    $stageExe = Join-Path $stage "Buscador Autosur.exe"
    $references = @("System.Windows.Forms", "System.Drawing", "System.Core") +
                  @(Get-ChildItem (Join-Path $stage "*.dll") | ForEach-Object { $_.FullName })
    Add-Type -TypeDefinition $sourceCode -Language CSharp `
        -ReferencedAssemblies $references `
        -OutputAssembly $stageExe -OutputType WindowsApplication
    if (!(Test-Path $stageExe)) { throw "No se pudo generar la aplicacion nueva." }

    if (Test-Path $backup) {
        Invoke-WithRetry { Remove-Item -LiteralPath $backup -Recurse -Force } "No se pudo quitar el respaldo anterior."
    }
    if (Test-Path $install) {
        Invoke-WithRetry { Move-Item -LiteralPath $install -Destination $backup } "No se pudo reemplazar la instalacion anterior."
    }
    Invoke-WithRetry { Move-Item -LiteralPath $stage -Destination $install } "No se pudo activar la actualizacion."

    $exe = Join-Path $install "Buscador Autosur.exe"
    $icon = Join-Path $install "icono_autosur.ico"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path $desktop "Buscador VIN Autosur.lnk"))
    $shortcut.TargetPath = $exe
    $shortcut.WorkingDirectory = $install
    $shortcut.IconLocation = $icon
    $shortcut.Save()

    $legacyRoot = Join-Path $installRoot "versiones"
    if (Test-Path $legacyRoot) { Remove-Item $legacyRoot -Recurse -Force -ErrorAction SilentlyContinue }

    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "Buscador Autosur se actualizo correctamente sobre la instalacion existente.`nSe conserva el mismo acceso directo y el mismo icono.",
        "Actualizacion completa") | Out-Null
    Start-Process $exe
}
catch {
    if ((!(Test-Path $install)) -and (Test-Path $backup)) {
        Move-Item -LiteralPath $backup -Destination $install -ErrorAction SilentlyContinue
    }
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue }
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "No se pudo instalar el Buscador VIN.`n`n$($_.Exception.Message)",
        "Error de instalacion") | Out-Null
    exit 1
}
