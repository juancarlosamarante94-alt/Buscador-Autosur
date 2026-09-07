$ErrorActionPreference = "Stop"
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$installRoot = Join-Path $env:LOCALAPPDATA "Buscador VIN Autosur"
$install = Join-Path $installRoot "app"
$stage = Join-Path $installRoot ("preparando_" + [Guid]::NewGuid().ToString("N"))
$backup = Join-Path $installRoot "respaldo_anterior"
$desktop = [Environment]::GetFolderPath("Desktop")

try {
    New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and $_.Path.StartsWith($installRoot, [StringComparison]::OrdinalIgnoreCase)
    } | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 700

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

    if (Test-Path $backup) { Remove-Item $backup -Recurse -Force }
    if (Test-Path $install) { Move-Item $install $backup }
    Move-Item $stage $install

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
        Move-Item $backup $install -ErrorAction SilentlyContinue
    }
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue }
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "No se pudo instalar el Buscador VIN.`n`n$($_.Exception.Message)",
        "Error de instalacion") | Out-Null
    exit 1
}
