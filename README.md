# DecorIt

Aplicativo mobile de apoio à visualização de cortinas em realidade aumentada. O projeto combina uma interface em **React Native + TypeScript** com uma experiência de câmera e AR desenvolvida em **Unity**.

## O que o aplicativo faz

- Mantém clientes, cômodos, registros de atendimento e fotos associados a cada cliente.
- Permite iniciar a câmera AR a partir do fluxo de um registro ou pelo atalho de AR rápido.
- Usa o Unity para posicionar e ajustar a cortina virtual, configurar dimensões e aplicar materiais.
- Salva as fotos capturadas no histórico do cliente; fotos do AR rápido podem ser identificadas com um cômodo depois.

As telas de cadastro e histórico ficam no React Native. A câmera, o posicionamento AR e a visualização 3D ficam no Unity, iniciado por uma Activity Android integrada ao app.

## Estrutura do repositório

```text
Assets/                  Cenas, scripts, prefabs, materiais e recursos do Unity
  Scenes/                Home, AR_Test, Catalog e outras cenas do projeto
  Scripts/               AR, cortina procedural, dados e integração com Android
  DecorItCurtains/       Materiais e texturas de cortinas
mobile/                  Aplicativo React Native e projeto Android
  src/screens.tsx        Telas do app
  src/state/             Estado compartilhado e rascunho de registro
  src/data/              Persistência, clientes, cômodos e registros
  android/               Activity Unity, módulo nativo e build Android
  scripts/               Exportação Unity e utilitários Android
Docs/                    Notas técnicas sobre materiais e cortinas procedurais
ArtSource/               Fontes de arte usadas na preparação dos tecidos
```

## Requisitos

- **Unity 6000.5.9f1**, instalado pelo Unity Hub. Para Android, instale também Android Build Support, SDK, NDK e OpenJDK pelo Hub.
- **Node.js 22.11 ou superior** e npm.
- Android Studio com Android SDK e **ADB** configurados.
- Para testar AR em aparelho, um dispositivo Android compatível com ARCore e com os serviços necessários do Google instalados.

O projeto é configurado para preservar o identificador Android `com.Wesler_Becker.DecorIt`, usado para manter compatibilidade com os dados locais existentes.

## Preparar o projeto

1. Abra a pasta do repositório no Unity Hub e aguarde a importação dos assets e a resolução dos pacotes.
2. Em outro terminal, instale as dependências JavaScript:

   ```powershell
   cd mobile
   npm ci
   ```

3. Conecte um aparelho Android com depuração USB ativada ou inicie um emulador. Confira a conexão com `adb devices`.
4. Com o projeto Unity preparado, exporte a biblioteca Android do Unity. Esse passo precisa ser repetido em cada máquina e após mudanças que afetem a build Unity:

   ```powershell
   cd mobile
   .\scripts\export-unity-library.ps1
   ```

O script grava a exportação em `mobile/android/unityExport/`. Essa pasta é gerada localmente e não faz parte do controle de versão.

## Executar uma build de desenvolvimento

No primeiro terminal, inicie o Metro:

```powershell
cd mobile
npm start
```

No segundo terminal, instale e compile a variante Debug:

```powershell
cd mobile/android
.\gradlew.bat :app:installDebug -PreactNativeArchitectures=arm64-v8a
```

Se o aparelho estiver conectado por USB e o app não alcançar o Metro, redirecione a porta padrão e abra o aplicativo:

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb reverse tcp:8081 tcp:8081
& $adb shell am start -n com.Wesler_Becker.DecorIt/com.weslerbecker.decorit.MainActivity
```

Se o Android SDK estiver em outro local, ajuste o caminho do `adb` ou adicione `platform-tools` ao `PATH`. A variante Debug espera o Metro ativo para carregar o código JavaScript.

## Gerar uma build Release

Para exportar a biblioteca Unity, compilar, instalar no único dispositivo ADB conectado e abrir o app com um comando, use o script:

```powershell
Set-Location 'E:\Unity\DecorIt\mobile'
.\scripts\atualizar-release.ps1
```

O script não precisa do Metro. Se houver mais de um emulador/aparelho conectado, ele mostra uma lista e pergunta em qual instalar; para escolher sem prompt, informe `-DeviceId emulator-5554`. Se as mudanças forem somente no React Native e a exportação Unity já estiver atualizada, passe `-SkipUnityExport` para pular a exportação.

Para compilar sem instalar, ou acompanhar cada etapa manualmente, exporte primeiro a biblioteca Unity e então execute o Gradle conforme abaixo.

Depois de exportar a biblioteca Unity, gere o APK sem depender do Metro:

```powershell
cd mobile/android
.\gradlew.bat :app:assembleRelease -PreactNativeArchitectures=arm64-v8a
```

O APK será criado em:

```text
mobile/android/app/build/outputs/apk/release/app-release.apk
```

**Antes de distribuir o app**, configure uma chave de assinatura de produção. Atualmente a variante Release está assinada com a chave Debug, adequada apenas para desenvolvimento e testes controlados.

Para instalar uma atualização preservando os dados locais:

```powershell
& $adb install -r .\app\build\outputs\apk\release\app-release.apk
```

Execute esse comando a partir de `mobile/android`, com `$adb` configurado como acima.

## Verificações do React Native

Na pasta `mobile`, os comandos principais são:

```powershell
npx tsc --noEmit
npm test -- --runInBand
npm run lint
```

## Dados e integração Unity

O aplicativo usa SQLite local através de OP-SQLite, com esquema e consultas tipados pelo Drizzle ORM. O esquema está em `mobile/src/data/schema.ts`; as migrations do Drizzle ficam em `mobile/drizzle/` e são aplicadas ao iniciar o app. Para gerar uma migration após alterar o esquema, execute `npm run db:generate` dentro de `mobile`.

Na primeira inicialização com essa versão, clientes, cômodos, registros e metadados de fotos existentes em `clients.json` são importados uma única vez para `decorit.db`. O JSON original e seu backup são mantidos como cópia da migração. Os arquivos PNG permanecem no armazenamento privado do app; o banco guarda os caminhos e metadados.

Ao abrir a Unity, o Android passa o contexto do cliente e do registro. Fotos capturadas durante o AR são colocadas em uma fila de eventos compartilhada no armazenamento do app; quando o React Native volta ao primeiro plano, importa esses eventos para o SQLite. Assim, a Unity não altera o JSON legado durante o fluxo integrado.

O catálogo de materiais usado no Unity é construído a partir de `Assets/DecorItCurtains/Materials/Active`; suas texturas estão em `Assets/DecorItCurtains/Textures`. A documentação sobre escala, materiais e geração da cortina procedural está em [`Docs/CurtainMaterials.md`](Docs/CurtainMaterials.md) e [`Docs/ProceduralCurtains.md`](Docs/ProceduralCurtains.md).

## Documentação complementar

- [Guia do aplicativo mobile](mobile/README.md)
- [Revisão dos materiais de cortina](Docs/CurtainMaterials.md)
- [Cortinas procedurais](Docs/ProceduralCurtains.md)
