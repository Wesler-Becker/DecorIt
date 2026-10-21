# DecorIt Mobile

Aplicativo mobile em React Native + TypeScript. As telas comuns ficam em `src/screens.tsx`; navegação, dados compartilhados e armazenamento ficam em `App.tsx`, `src/state` e `src/data`.

## Executar

Na pasta `mobile`, instale as dependências com `npm install`, inicie o Metro com `npm start` e, em outro terminal, execute `npm run android` com um aparelho ou emulador Android conectado. Verifique o ambiente com `npx react-native doctor`.

No Windows, os atalhos `atualizar-debug.bat` e `gerar-release.bat` automatizam os fluxos mais comuns. Ambos regeneram a biblioteca Unity antes de compilar, então deixe o projeto aberto no Editor. O primeiro também inicia o Metro em outra janela com `--reset-cache`, instala/atualiza o Debug em um dispositivo via ADB, ajusta a porta salva do bundler para `localhost:8081` e abre o app. O segundo gera `android/app/build/outputs/apk/release/app-release.apk` sem Metro. A assinatura Release atual ainda usa a chave debug e deve ser substituída por uma chave de distribuição antes de publicar.

## Dados e Unity

No Android, o app conserva o identificador `com.Wesler_Becker.DecorIt` para compartilhar `clients.json` e as fotos no diretório privado já usado pelo Unity. O modelo JSON mantém clientes, cômodos, registros e caminhos de fotos existentes.

O código nativo de `UnityArModule` abre a Activity do Unity passando o cliente, cômodo, registro e medidas. `Assets/Scripts/UnityARHostBridge.cs` recebe esses dados e devolve ao React ao sair do AR. A captura e a detecção AR continuam no Unity; as telas de cadastro, clientes, cômodos, registros e catálogo ficam em React Native.

Antes de compilar o APK, gere a biblioteca Unity Android com o Editor aberto no projeto:

```powershell
./scripts/export-unity-library.ps1
```

O script exporta somente a cena `AR_Test` para `android/unityExport/` e aplica os ajustes do módulo de biblioteca. A exportação é um artefato local ignorado pelo Git, então precisa ser regenerada em outra máquina antes da build Android.

Para gerar um APK Release independente do Metro, na pasta `mobile/android`, execute:

```powershell
.\gradlew.bat :app:assembleRelease -PreactNativeArchitectures=arm64-v8a
```

O APK fica em `app/build/outputs/apk/release/app-release.apk`. Essa build inclui o bundle React Native e continua usando o mesmo identificador Android do aplicativo Unity existente.

Na build Debug, a integração pode ser verificada sem abrir a interface manualmente com `adb shell am start -n com.Wesler_Becker.DecorIt/com.weslerbecker.decorit.MainActivity --ez decorit_unity_smoke_test true`. O acionador usa um cliente de teste e não grava dados no cadastro.

Para restaurar um backup local de clientes depois de instalar o app, use `scripts/restore-android-clients.ps1 -BackupPath <caminho-do-clients.json>`. O script exige um aparelho autorizado por ADB, interrompe se já houver um arquivo no destino e confere o SHA-256 após a cópia.

## Verificações

```sh
npx tsc --noEmit
npm test -- --runInBand
npm run lint
```
