param(
  [Parameter(Mandatory = $true)]
  [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
  [string]$BackupPath,
  [string]$PackageName = 'com.Wesler_Becker.DecorIt',
  [string]$AdbPath
)

$ErrorActionPreference = 'Stop'

if (-not $AdbPath) {
  if ($env:ANDROID_HOME) {
    $AdbPath = Join-Path $env:ANDROID_HOME 'platform-tools\adb.exe'
  } else {
    $AdbPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
  }
}

if (-not (Test-Path -LiteralPath $AdbPath -PathType Leaf)) {
  throw "adb.exe não encontrado em: $AdbPath"
}

$deviceLines = @(& $AdbPath devices | Where-Object { $_ -match '^\S+\s+device\b' })
if ($deviceLines.Count -ne 1) {
  throw "Conecte exatamente um aparelho Android autorizado por ADB. Aparelhos disponíveis: $($deviceLines.Count)."
}

$installedPackage = (& $AdbPath shell pm path $PackageName 2>$null | Out-String).Trim()
if (-not $installedPackage.StartsWith('package:')) {
  throw "O pacote $PackageName ainda não está instalado. Instale o APK antes de restaurar os dados."
}

$deviceDirectory = "/sdcard/Android/data/$PackageName/files"
$deviceFile = "$deviceDirectory/clients.json"
& $AdbPath shell test -e $deviceFile 2>$null
if ($LASTEXITCODE -eq 0) {
  throw "Já existe um clients.json no aparelho; a restauração foi interrompida para não sobrescrever dados."
}

& $AdbPath shell mkdir -p $deviceDirectory
if ($LASTEXITCODE -ne 0) {
  throw 'Não foi possível preparar o diretório privado do aplicativo no aparelho.'
}

& $AdbPath push $BackupPath $deviceFile
if ($LASTEXITCODE -ne 0) {
  throw 'O envio do backup falhou; o arquivo local permanece intacto.'
}

$localHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $BackupPath).Hash.ToLowerInvariant()
$remoteHashOutput = (& $AdbPath shell sha256sum $deviceFile 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0) {
  throw 'Não foi possível conferir o arquivo restaurado no aparelho.'
}
$remoteHash = ($remoteHashOutput -split '\s+')[0].ToLowerInvariant()
if ($remoteHash -ne $localHash) {
  throw 'O hash do arquivo no aparelho não corresponde ao backup local.'
}

Write-Output "Backup restaurado e verificado em $deviceFile (SHA-256: $localHash)."
