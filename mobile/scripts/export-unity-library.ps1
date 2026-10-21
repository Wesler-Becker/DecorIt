$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$unityExport = Join-Path $projectRoot 'mobile\android\unityExport'

$callerArgs = @('--project-path', $projectRoot, '--format', 'json', '--caller', 'plugin', '--skill', 'unity-cli')
$exportMode = & unity command eval @callerArgs --code 'UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject = true; return UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject;'
if ($LASTEXITCODE -ne 0 -or -not ($exportMode | ConvertFrom-Json).success) {
  throw "Não foi possível habilitar a exportação Android do Unity: $exportMode"
}

$buildRequest = & unity command build @callerArgs --target Android --outputPath $unityExport --confirm true
if ($LASTEXITCODE -ne 0) { throw "Unity recusou iniciar a exportação Android: $buildRequest" }

$deadline = (Get-Date).AddMinutes(20)
do {
  Start-Sleep -Seconds 5
  $buildStatus = & unity command build_status @callerArgs
  if ($LASTEXITCODE -ne 0) { throw "Não foi possível consultar o build Unity: $buildStatus" }
  $statusData = ($buildStatus | ConvertFrom-Json).data.result
  if ($statusData.status -eq 'completed') {
    if ($statusData.result -ne 'Succeeded' -or $statusData.totalErrors -gt 0) {
      throw "Exportação Unity terminou como $($statusData.result), com $($statusData.totalErrors) erros."
    }
    break
  }
  if ((Get-Date) -gt $deadline) { throw 'Tempo limite excedido aguardando a exportação Android do Unity.' }
} while ($true)

$libraryBuildFile = Join-Path $unityExport 'unityLibrary\build.gradle'
$libraryBuild = Get-Content -LiteralPath $libraryBuildFile -Raw
$libraryBuild = [regex]::Replace($libraryBuild, "(?m)^\s*implementation project\(':unityLibrary:xrmanifest\.androidlib'\)\r?\n", '')
$libraryBuild = $libraryBuild.Replace('implementation fileTree', 'api fileTree')
$libraryBuild = $libraryBuild.Replace("implementation 'androidx.games:games-activity:", "api 'androidx.games:games-activity:")
$libraryBuild = $libraryBuild.Replace('implementation "androidx.games:games-activity:', 'api "androidx.games:games-activity:')
$libraryBuild = [regex]::Replace($libraryBuild, '(?m)^\s*ndkPath "[^"]+"\r?\n', '')
$libraryBuild = [regex]::Replace($libraryBuild, '(?m)^\s*ndkVersion "[^"]+"\r?\n', '    ndkVersion rootProject.ext.ndkVersion' + "`n")
$libraryBuild = $libraryBuild.Replace("android {`r`n", "android {`r`n    ndkPath rootProject.ext.ndkPath`r`n")
$libraryBuild = $libraryBuild.Replace("android {`n", "android {`n    ndkPath rootProject.ext.ndkPath`n")
$libraryBuild = $libraryBuild.Replace('getProperty("unity.androidNdkPath")', 'android.ndkPath')
$libraryBuild = $libraryBuild.Replace('getProperty("unity.androidSdkPath")', 'getSdkDir()')

Set-Content -LiteralPath $libraryBuildFile -Value $libraryBuild -NoNewline
Write-Output "unityLibrary exportado em: $unityExport"
