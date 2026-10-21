@echo off
setlocal
set "MOBILE_DIR=%~dp0"
cd /d "%MOBILE_DIR%"

where node >nul 2>&1 || (echo ERRO: Node.js nao foi encontrado no PATH.& exit /b 1)
where npm >nul 2>&1 || (echo ERRO: npm nao foi encontrado no PATH.& exit /b 1)
set "ADB_EXE=adb"
where adb >nul 2>&1
if errorlevel 1 (
  set "ADB_EXE=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"
  if not exist "%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe" (
    echo ERRO: adb nao foi encontrado. Instale o Android SDK Platform-Tools ou adicione adb ao PATH.
    exit /b 1
  )
)
if not exist "node_modules" (echo ERRO: dependencias ausentes. Execute npm install em "%MOBILE_DIR%".& exit /b 1)
if not exist "android\gradlew.bat" (echo ERRO: gradlew.bat nao encontrado.& exit /b 1)
if not exist "%MOBILE_DIR%scripts\export-unity-library.ps1" (
  echo ERRO: script de exportacao Unity nao encontrado.
  exit /b 1
)

"%ADB_EXE%" get-state >nul 2>&1 || (echo ERRO: nenhum dispositivo Android autorizado encontrado pelo ADB.& exit /b 1)

echo Exportando novamente a cena AR e os materiais atuais do Unity...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%MOBILE_DIR%scripts\export-unity-library.ps1"
if errorlevel 1 (
  echo ERRO: exportacao Unity falhou. Confirme que o projeto esta aberto no Unity e confira os erros no Editor.
  exit /b 1
)

netstat -ano | findstr /r /c:":8081 .*LISTENING" >nul
if errorlevel 1 (
  echo Iniciando Metro em uma nova janela...
  start "DecorIt Metro" /D "%MOBILE_DIR%" cmd /k "npm start -- --reset-cache --host 0.0.0.0"
)

echo Aguardando o Metro em http://127.0.0.1:8081 ...
for /L %%i in (1,1,90) do (
  curl.exe --silent --fail http://127.0.0.1:8081/status 2>nul | findstr /c:"packager-status:running" >nul
  if not errorlevel 1 goto metro_ready
  ping 127.0.0.1 -n 2 >nul
)
echo ERRO: Metro nao ficou disponivel em 90 segundos. Confira a janela DecorIt Metro.
exit /b 1

:metro_ready
echo Metro ativo. Conectando o ADB reverso...
"%ADB_EXE%" reverse tcp:8081 tcp:8081 || exit /b 1

echo Compilando e instalando a versao debug...
pushd "%MOBILE_DIR%android"
call gradlew.bat :app:installDebug -PreactNativeArchitectures=arm64-v8a
set "BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%BUILD_RESULT%"=="0" (
  echo ERRO: build ou instalacao debug falhou.
  exit /b %BUILD_RESULT%
)

echo Ajustando a porta do Metro salva no app...
"%ADB_EXE%" shell am force-stop com.Wesler_Becker.DecorIt >nul 2>&1
"%ADB_EXE%" shell run-as com.Wesler_Becker.DecorIt sed -i s/localhost:8082/localhost:8081/ shared_prefs/com.Wesler_Becker.DecorIt_preferences.xml >nul 2>&1
"%ADB_EXE%" reverse --remove tcp:8082 >nul 2>&1
"%ADB_EXE%" reverse tcp:8081 tcp:8081 || exit /b 1

echo Abrindo DecorIt no dispositivo...
"%ADB_EXE%" shell am start -n com.Wesler_Becker.DecorIt/com.weslerbecker.decorit.MainActivity
if errorlevel 1 exit /b 1
echo Debug atualizado. Mantenha a janela do Metro aberta enquanto usa o app.
exit /b 0
