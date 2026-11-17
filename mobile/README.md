# Aplicativo mobile

Este diretório contém o aplicativo React Native/TypeScript, o projeto Android que hospeda a biblioteca Unity e os scripts auxiliares. Para os passos completos de instalação, execução e geração de APK, consulte o [README principal](../README.md).

O alvo atual é Android; a estrutura inicial iOS e o Gemfile foram retirados. Mantenha `patches/`: o `postinstall` aplica correções de ligação nativa necessárias para as dependências Android. Consulte a [revisão dos arquivos versionados](../Docs/RevisaoRepositorio.md) para os critérios de limpeza.

## Organização

- `App.tsx`: navegação raiz, abas e atalho de AR rápido.
- `src/screens.tsx`: telas React Native.
- `src/state/`: estado do app e rascunhos de registro.
- `src/data/`: persistência SQLite local (OP-SQLite + Drizzle), esquema e regras de clientes, cômodos, registros e fotos.
- `drizzle/`: migrations versionadas aplicadas na inicialização; gere novas migrations com `npm run db:generate`.
- `android/`: integração nativa Android/Unity e configuração Gradle.
- `android/app/src/main/java/com/com.weslerbecker.decorit/UnityArModule.kt`: entrada React Native para abrir a Unity.
- `android/app/src/main/java/com/com.weslerbecker.decorit/ArSettingsStore.kt`: persistência da configuração de cortina no SQLite.
- `android/app/src/main/java/com/com.weslerbecker.decorit/SecureFieldsModule.kt`: proteção de campos com Android Keystore.
- `scripts/export-unity-library.ps1`: exporta o projeto Unity para `android/unityExport/` e prepara o módulo para a build combinada.
- `scripts/atualizar-release.ps1`: exporta Unity, compila o Release, pergunta qual dispositivo usar se houver mais de um, instala com `adb install -r` e abre o app; aceita `-DeviceId` e `-SkipUnityExport`.

## Desenvolvimento

Instale dependências com `npm ci`, exporte a biblioteca Unity com `.\scripts\export-unity-library.ps1` e então inicie o Metro com `npm start`. Para gerar e instalar um Release de uma vez, execute `.\scripts\atualizar-release.ps1` a partir deste diretório. Os comandos detalhados e os requisitos estão no [guia principal](../README.md).

O Debug depende do Metro; o Release inclui o bundle JavaScript. Mudanças em scripts, cenas, materiais ou UI Unity exigem nova exportação antes da build Android. `-SkipUnityExport` é apropriado somente quando a exportação existente já corresponde ao estado do Unity.

O armazenamento integrado e a proteção dos campos são implementados para Android. Veja [arquitetura e segurança](../Docs/Arquitetura.md) e [funcionalidades](../Docs/Funcionalidades.md) para o fluxo entre os dois processos, campos protegidos e limitações.

As verificações JavaScript/TypeScript podem ser executadas neste diretório:

```powershell
npx tsc --noEmit
npm test -- --runInBand
npm run lint
```
