@echo off
setlocal
set "MOBILE_DIR=%~dp0"
cd /d "%MOBILE_DIR%"

where java >nul 2>&1 || (echo ERRO: Java nao foi encontrado no PATH.& exit /b 1)
if not exist "android\gradlew.bat" (echo ERRO: gradlew.bat nao encontrado.& exit /b 1)
if not exist "%MOBILE_DIR%scripts\export-unity-library.ps1" (
  echo ERRO: script de exportacao Unity nao encontrado.
  exit /b 1
)

echo Exportando novamente a cena AR e os materiais atuais do Unity...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%MOBILE_DIR%scripts\export-unity-library.ps1"
if errorlevel 1 (
  echo ERRO: exportacao Unity falhou. Confirme que o projeto esta aberto no Unity e confira os erros no Editor.
  exit /b 1
)

echo Gerando APK Release para arm64-v8a...
pushd "%MOBILE_DIR%android"
call gradlew.bat :app:assembleRelease -PreactNativeArchitectures=arm64-v8a
set "BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%BUILD_RESULT%"=="0" (
  echo ERRO: build Release falhou.
  exit /b %BUILD_RESULT%
)

echo APK criado em: %MOBILE_DIR%android\app\build\outputs\apk\release\app-release.apk
echo ATENCAO: a configuracao atual do projeto assina o Release com a chave debug.
exit /b 0
