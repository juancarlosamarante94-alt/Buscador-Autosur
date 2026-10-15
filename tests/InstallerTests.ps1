$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$package = Join-Path $repoRoot 'Buscador_VIN_Instalable'
$installer = Join-Path $package 'Instalar.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('AutosurInstallerTest_' + [Guid]::NewGuid().ToString('N'))
$root = Join-Path $fixture 'Buscador VIN Autosur'
$desktop = Join-Path $fixture 'Desktop'
$old = Join-Path $root 'app'
$locks = @()
function Assert($condition, [string]$message) { if (!$condition) { throw $message } }
function Run-Installer([string]$script, [string]$target, [string]$links, [bool]$success) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -SinInteraccion -NoIniciar -DirectorioInstalacion $target -DirectorioEscritorio $links
    if ($success) { Assert ($LASTEXITCODE -eq 0) 'El instalador devolvio error' }
    else { Assert ($LASTEXITCODE -ne 0) 'Se esperaba fallo sin activar la instalacion' }
}
function Read-Active {
    $shell = New-Object -ComObject WScript.Shell
    return $shell.CreateShortcut((Join-Path $desktop 'Buscador VIN Autosur.lnk'))
}
try {
    New-Item -ItemType Directory -Force -Path $old,$desktop | Out-Null
    Copy-Item -LiteralPath (Join-Path $package 'Buscador Autosur.exe') -Destination $old
    # Synthetic bases only; never use or log customer data.
    foreach ($name in @('vehiculos.dat','clientes.dat')) { [IO.File]::WriteAllText((Join-Path $old $name), 'SYNTHETIC_' + $name) }
    $expected = @{}
    foreach ($name in @('vehiculos.dat','clientes.dat')) { $expected[$name] = (Get-FileHash (Join-Path $old $name)).Hash }
    $oldExe = Join-Path $old 'Buscador Autosur.exe'
    $locks += [IO.File]::Open($oldExe, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $renameBlocked = $false
    try { [IO.Directory]::Move($old, (Join-Path $root 'rename-test')) } catch { $renameBlocked = $true }
    Assert $renameBlocked 'La prueba no reprodujo una carpeta bloqueada'
    Write-Output 'OK bloqueo real de archivo impide mover la instalacion anterior'
    Run-Installer $installer $root $desktop $true
    $first = Read-Active
    $firstDirectory = Split-Path -Parent $first.TargetPath
    Assert (Test-Path -LiteralPath $first.TargetPath) 'Falta ejecutable instalado'
    Assert ($firstDirectory -ne $old) 'Reutilizo la carpeta bloqueada'
    Assert ($first.WorkingDirectory -eq $firstDirectory) 'Acceso directo sin directorio de trabajo'
    Assert (Test-Path -LiteralPath $oldExe) 'Se altero la instalacion anterior'
    foreach ($name in $expected.Keys) { Assert ((Get-FileHash (Join-Path $firstDirectory $name)).Hash -eq $expected[$name]) 'No conservo las bases' }
    Write-Output 'OK instalacion con archivo antiguo bloqueado; conserva bases y actualiza el mismo acceso directo'
    # Next update must use the active bases, not the stale legacy app folder.
    foreach ($name in $expected.Keys) { [IO.File]::WriteAllText((Join-Path $old $name), 'STALE_SYNTHETIC') }
    $locks += [IO.File]::Open($first.TargetPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    Run-Installer $installer $root $desktop $true
    $second = Read-Active
    $secondDirectory = Split-Path -Parent $second.TargetPath
    Assert ($secondDirectory -ne $firstDirectory) 'Reutilizo una version bloqueada'
    foreach ($name in $expected.Keys) { Assert ((Get-FileHash (Join-Path $secondDirectory $name)).Hash -eq $expected[$name]) 'Leyendo bases obsoletas al actualizar por segunda vez' }
    Assert ((Get-Content -Raw (Join-Path $root 'instalacion-actual.txt')).Trim() -eq $secondDirectory) 'Indicador activo incorrecto'
    Write-Output 'OK actualizacion sucesiva desde la version activa, aunque ambas versiones anteriores sigan bloqueadas'
    # Failed compilation cannot change the active shortcut or erase its bases.
    $brokenSource = Join-Path $fixture 'broken-source'
    Copy-Item -LiteralPath $package -Destination $brokenSource -Recurse
    [IO.File]::WriteAllText((Join-Path $brokenSource 'BuscadorVIN.cs'), 'invalid C# AppVersion="9.9.9"')
    Run-Installer (Join-Path $brokenSource 'Instalar.ps1') $root $desktop $false
    $after = Read-Active
    Assert ($after.TargetPath -eq $second.TargetPath) 'Fallo de compilacion cambio el acceso directo activo'
    foreach ($name in $expected.Keys) { Assert ((Get-FileHash (Join-Path $secondDirectory $name)).Hash -eq $expected[$name]) 'Fallo de compilacion afecto las bases activas' }
    Write-Output 'OK fallo de compilacion deja intacta la instalacion activa y las bases'
    $emptyRoot = Join-Path $fixture 'empty'
    $emptyDesktop = Join-Path $fixture 'empty-desktop'
    Run-Installer $installer $emptyRoot $emptyDesktop $false
    Assert (!(Test-Path (Join-Path $emptyDesktop 'Buscador VIN Autosur.lnk'))) 'Se activo instalacion sin bases'
    Write-Output 'OK instalacion sin bases falla sin crear acceso directo'
    Write-Output 'Pruebas reales del instalador aprobadas.'
}
finally {
    foreach ($handle in $locks) { $handle.Dispose() }
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}

exit 0
