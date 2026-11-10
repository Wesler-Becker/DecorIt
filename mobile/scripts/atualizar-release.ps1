[CmdletBinding()]
param(
  [string]$DeviceId,
  [switch]$SkipUnityExport
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$androidDir = Join-Path $projectRoot 'mobile\android'
$exportScript = Join-Path $PSScriptRoot 'export-unity-library.ps1'
$apkPath = Join-Path $androidDir 'app\build\outputs\apk\release\app-release.apk'

function Find-Adb {
  $candidates = [System.Collections.Generic.List[string]]::new()
  foreach ($sdkRoot in @($env:ANDROID_SDK_ROOT, $env:ANDROID_HOME)) {
    if (-not [string]::IsNullOrWhiteSpace($sdkRoot)) {
      $candidates.Add((Join-Path $sdkRoot 'platform-tools\adb.exe'))
    }
  }

  $localProperties = Join-Path $androidDir 'local.properties'
  if (Test-Path -LiteralPath $localProperties) {
    $sdkLine = Select-String -LiteralPath $localProperties -Pattern '^\s*sdk\.dir=(.+)$' | Select-Object -First 1
    if ($sdkLine) {
      $candidates.Add((Join-Path $sdkLine.Matches[0].Groups[1].Value.Trim() 'platform-tools\adb.exe'))
    }
  }

  if ($env:LOCALAPPDATA) {
    $candidates.Add((Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'))
  }

  foreach ($candidate in $candidates) {
    if (Test-Path -LiteralPath $candidate) { return (Resolve-Path -LiteralPath $candidate).Path }
  }

  $adbCommand = Get-Command adb -ErrorAction SilentlyContinue
  if ($adbCommand) { return $adbCommand.Source }
  throw 'Não encontrei o adb. Configure ANDROID_SDK_ROOT/ANDROID_HOME ou instale o Android SDK Platform-Tools.'
}

$adb = Find-Adb
$deviceOutput = & $adb devices
if ($LASTEXITCODE -ne 0) { throw 'Não foi possível consultar os dispositivos ADB.' }
$connectedDevices = @(
  foreach ($line in $deviceOutput) {
    if ($line -match '^([^\s]+)\s+device(?:\s|$)') { $Matches[1] }
  }
)

if ($DeviceId) {
  if ($connectedDevices -notcontains $DeviceId) {
    throw "O dispositivo '$DeviceId' não está conectado/autorizado. Dispositivos disponíveis: $($connectedDevices -join ', ')"
  }
  $targetDevice = $DeviceId
} elseif ($connectedDevices.Count -eq 1) {
  $targetDevice = $connectedDevices[0]
} elseif ($connectedDevices.Count -eq 0) {
  throw 'Nenhum aparelho/emulador ADB autorizado foi encontrado. Inicie o emulador ou conecte o aparelho e aceite a depuração USB.'
} else {
  Write-Host 'Mais de um dispositivo ADB está conectado. Escolha onde instalar o DecorIt:'
  for ($index = 0; $index -lt $connectedDevices.Count; $index++) {
    Write-Host "  $($index + 1)) $($connectedDevices[$index])"
  }

  while (-not $targetDevice) {
    $choice = Read-Host 'Digite o número ou o identificador do dispositivo (Q para cancelar)'
    if ($choice -match '^(?i:q|sair|cancelar)$') { throw 'Atualização cancelada pelo usuário.' }

    for ($index = 0; $index -lt $connectedDevices.Count; $index++) {
      if ($choice -eq "$($index + 1)" -or $choice -eq $connectedDevices[$index]) {
        $targetDevice = $connectedDevices[$index]
        break
      }
    }

    if (-not $targetDevice) {
      Write-Warning 'Opção inválida. Escolha um dos números ou identificadores listados.'
    }
  }
}

if (-not $SkipUnityExport) {
  Write-Host 'Exportando a biblioteca Android do Unity...'
  & $exportScript
} else {
  Write-Host 'Pulando a exportação Unity solicitada.'
}

$originalJavaToolOptions = $env:JAVA_TOOL_OPTIONS
$requiredJavaOption = '-Djdk.net.unixdomain.tmpdir=C:\Windows\Temp'
$javaOptions = @($originalJavaToolOptions, $requiredJavaOption) |
  Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
  Select-Object -Unique
$env:JAVA_TOOL_OPTIONS = $javaOptions -join ' '

Push-Location $androidDir
try {
  Write-Host 'Compilando o APK Release (o bundle JavaScript será incluído)...'
  & .\gradlew.bat :app:assembleRelease -PreactNativeArchitectures=arm64-v8a
  if ($LASTEXITCODE -ne 0) { throw 'A compilação Gradle do Release falhou.' }
} finally {
  Pop-Location
  if ($null -eq $originalJavaToolOptions) {
    Remove-Item Env:JAVA_TOOL_OPTIONS -ErrorAction SilentlyContinue
  } else {
    $env:JAVA_TOOL_OPTIONS = $originalJavaToolOptions
  }
}

if (-not (Test-Path -LiteralPath $apkPath)) { throw "Gradle terminou, mas o APK não foi encontrado: $apkPath" }

Write-Host "Instalando no dispositivo $targetDevice (com -r para preservar os dados)..."
& $adb -s $targetDevice install -r $apkPath
if ($LASTEXITCODE -ne 0) { throw 'O ADB não conseguiu instalar o APK.' }

Write-Host 'Abrindo o DecorIt...'
& $adb -s $targetDevice shell monkey -p com.Wesler_Becker.DecorIt 1
if ($LASTEXITCODE -ne 0) { throw 'O APK foi instalado, mas não consegui iniciar o aplicativo.' }

Write-Host "Concluído. APK: $apkPath"
