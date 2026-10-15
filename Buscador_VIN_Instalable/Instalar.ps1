param(
    [switch]$SinInteraccion,
    [switch]$NoIniciar,
    [string]$DirectorioInstalacion = "",
    [string]$DirectorioEscritorio = ""
)
$ErrorActionPreference = "Stop"
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$installRoot = if ($DirectorioInstalacion) { [IO.Path]::GetFullPath($DirectorioInstalacion) } else { Join-Path $env:LOCALAPPDATA "Buscador VIN Autosur" }
$desktop = if ($DirectorioEscritorio) { $DirectorioEscritorio } else { [Environment]::GetFolderPath("Desktop") }
$rootPrefix = $installRoot.TrimEnd('\') + '\'
$pointer = Join-Path $installRoot "instalacion-actual.txt"
$shortcutPath = Join-Path $desktop "Buscador VIN Autosur.lnk"
$destination = $null
$activated = $false
Set-Location ([IO.Path]::GetTempPath())

function Stop-BuscadorAutosur {
    $deadline = (Get-Date).AddSeconds(20)
    do {
        $targets = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
            $_.Id -ne $PID -and $_.Path -and
            $_.Path.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)
        })
        if ($targets.Count -eq 0) { return }
        $targets | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "La aplicacion anterior no termino de cerrarse. Cerra App taller y volve a intentar."
}

function Test-OwnPath([string]$path) {
    if (!$path) { return $false }
    return [IO.Path]::GetFullPath($path).StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)
}

try {
    New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $desktop | Out-Null
    Stop-BuscadorAutosur
    $shell = New-Object -ComObject WScript.Shell
    $candidates = @($source)
    # The shortcut identifies the active installation, even if an older update
    # could not finish writing the pointer.
    if (Test-Path -LiteralPath $shortcutPath) {
        $oldShortcut = $shell.CreateShortcut($shortcutPath)
        $activeDirectory = Split-Path -Parent $oldShortcut.TargetPath
        if (Test-OwnPath $activeDirectory) { $candidates += $activeDirectory }
    }
    if (Test-Path -LiteralPath $pointer) {
        $activeDirectory = (Get-Content -Raw -LiteralPath $pointer).Trim()
        if (Test-OwnPath $activeDirectory) { $candidates += $activeDirectory }
    }
    $candidates += (Join-Path $installRoot "app")
    $candidates += (Join-Path $installRoot "respaldo_anterior")
    $candidates += @(Get-ChildItem (Join-Path $installRoot "versiones") -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "Buscador Autosur.exe") } |
        Sort-Object Name -Descending | ForEach-Object FullName)
    # Always copy the pair from one installation; never mix different bases.
    $dataDirectory = $null
    foreach ($candidate in $candidates) {
        if ((Test-Path -LiteralPath (Join-Path $candidate "vehiculos.dat")) -and
            (Test-Path -LiteralPath (Join-Path $candidate "clientes.dat"))) {
            $dataDirectory = $candidate
            break
        }
    }
    if (!$dataDirectory) { throw "No se encontraron vehiculos.dat y clientes.dat. Para la primera instalacion usa el instalador completo." }
    $sourceCode = (Get-Content -Raw -Encoding UTF8 (Join-Path $source "BuscadorVIN.cs")) + [Environment]::NewLine +
                  (Get-Content -Raw -Encoding UTF8 (Join-Path $source "Turnero.cs"))
    $version = [regex]::Match($sourceCode, 'AppVersion="([0-9.]+)"').Groups[1].Value
    if (!$version) { throw "No se encontro la version del programa." }
    # Build in its final, unique directory. No old directory is renamed or
    # removed, and DLLs loaded by the compiler do not need to be moved either.
    $destination = Join-Path (Join-Path $installRoot "versiones") ("v" + $version + "_" + (Get-Date -Format "yyyyMMddHHmmssfff") + "_" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    foreach ($dataName in @("vehiculos.dat", "clientes.dat")) {
        Copy-Item -LiteralPath (Join-Path $dataDirectory $dataName) -Destination (Join-Path $destination $dataName)
    }
    Copy-Item -LiteralPath (Join-Path $source "icono_autosur.ico") -Destination $destination
    Copy-Item -Path (Join-Path $source "lib\*.dll") -Destination $destination
    $exe = Join-Path $destination "Buscador Autosur.exe"
    $references = @("System.Windows.Forms", "System.Drawing", "System.Core") +
                  @(Get-ChildItem (Join-Path $destination "*.dll") | ForEach-Object FullName)
    Add-Type -TypeDefinition $sourceCode -Language CSharp -ReferencedAssemblies $references -OutputAssembly $exe -OutputType WindowsApplication
    if (!(Test-Path -LiteralPath $exe)) { throw "No se pudo generar la aplicacion nueva." }
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exe
    $shortcut.WorkingDirectory = $destination
    $shortcut.IconLocation = Join-Path $destination "icono_autosur.ico"
    $shortcut.Save()
    $activated = $true
    # The shortcut remains the authority if writing this recovery pointer fails.
    try { [IO.File]::WriteAllText($pointer, $destination, [Text.Encoding]::UTF8) }
    catch { Write-Warning "La app quedo instalada; no se pudo guardar el indicador de recuperacion." }
    if (!$SinInteraccion) {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show(
            ("App taller se actualizo correctamente." + [Environment]::NewLine + "Se conservaron las bases, el mismo acceso directo y el icono."),
            "Actualizacion completa") | Out-Null
    }
    if (!$NoIniciar) { Start-Process -FilePath $exe -WorkingDirectory $destination }
    Write-Output "Instalacion completada."
}
catch {
    $detail = $_.Exception.Message
    # Failed preparation leaves the original shortcut and installation intact.
    if (!$activated -and $destination -and (Test-Path -LiteralPath $destination)) {
        Remove-Item -LiteralPath $destination -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($SinInteraccion) { Write-Error ("No se pudo instalar App taller: " + $detail) -ErrorAction Continue }
    else {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show(("No se pudo instalar App taller." + [Environment]::NewLine + $detail), "Error de instalacion") | Out-Null
    }
    exit 1
}
