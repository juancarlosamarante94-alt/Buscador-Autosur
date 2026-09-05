$ErrorActionPreference = "Stop"
$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$installRoot = Join-Path $env:LOCALAPPDATA "Buscador VIN Autosur"
$version = Get-Date -Format "yyyyMMdd_HHmmss_fff"
$install = Join-Path (Join-Path $installRoot "versiones") $version
$desktop = [Environment]::GetFolderPath("Desktop")

try {
    New-Item -ItemType Directory -Force -Path $install | Out-Null
    $previous = Get-ChildItem (Join-Path $installRoot "versiones") -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -ne $install } | Sort-Object Name -Descending | Select-Object -First 1
    foreach ($dataName in @("vehiculos.dat", "clientes.dat")) {
        $sourceData = Join-Path $source $dataName
        $previousData = if ($previous) { Join-Path $previous.FullName $dataName } else { $null }
        if (Test-Path $sourceData) { Copy-Item -Force $sourceData (Join-Path $install $dataName) }
        elseif ($previousData -and (Test-Path $previousData)) { Copy-Item -Force $previousData (Join-Path $install $dataName) }
        else { throw "No se encontro $dataName. Para la primera instalacion usa el instalador completo." }
    }
    Copy-Item -Force (Join-Path $source "icono_autosur.ico") (Join-Path $install "icono_autosur.ico")
    Copy-Item -Force (Join-Path $source "lib\*.dll") $install
    $sourceCode = (Get-Content -Raw -Encoding UTF8 (Join-Path $source "BuscadorVIN.cs")) + "`r`n" +
                  (Get-Content -Raw -Encoding UTF8 (Join-Path $source "Turnero.cs"))
    $exe = Join-Path $install "Buscador Autosur.exe"
    $references = @("System.Windows.Forms", "System.Drawing", "System.Core") +
                  @(Get-ChildItem (Join-Path $install "*.dll") | ForEach-Object { $_.FullName })
    Add-Type -TypeDefinition $sourceCode -Language CSharp `
        -ReferencedAssemblies $references `
        -OutputAssembly $exe -OutputType WindowsApplication

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path $desktop "Buscador VIN Autosur.lnk"))
    $shortcut.TargetPath = $exe
    $shortcut.WorkingDirectory = $install
    $shortcut.IconLocation = (Join-Path $install "icono_autosur.ico")
    $shortcut.Save()

    Get-ChildItem (Join-Path $installRoot "versiones") -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -ne $install } |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }

    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "Buscador Autosur se instalo correctamente.`nSe creo o actualizo el acceso directo en el escritorio.`nLa version anterior puede cerrarse normalmente.",
        "Instalacion completa") | Out-Null
    Start-Process $exe
}
catch {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "No se pudo instalar el Buscador VIN.`n`n$($_.Exception.Message)",
        "Error de instalacion") | Out-Null
    exit 1
}
