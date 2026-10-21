# Aplicativo mobile

Este diretório contém o aplicativo React Native/TypeScript, o projeto Android que hospeda a biblioteca Unity e os scripts auxiliares. Para os passos completos de instalação, execução e geração de APK, consulte o [README principal](../README.md).

## Organização

- `App.tsx`: navegação raiz, abas e atalho de AR rápido.
- `src/screens.tsx`: telas React Native.
- `src/state/`: estado do app e rascunhos de registro.
- `src/data/`: persistência local e regras de clientes, cômodos, registros e fotos.
- `android/`: integração nativa Android/Unity e configuração Gradle.
- `scripts/export-unity-library.ps1`: exporta o projeto Unity para `android/unityExport/` e prepara o módulo para a build combinada.
- `scripts/restore-android-clients.ps1`: restaura um backup local de clientes para um aparelho conectado por ADB, com verificações para evitar sobrescrever dados existentes.

## Desenvolvimento

Instale dependências com `npm ci`, exporte a biblioteca Unity com `.\scripts\export-unity-library.ps1` e então inicie o Metro com `npm start`. A partir de `android/`, use o Gradle Wrapper para compilar e instalar Debug ou gerar Release. Os comandos detalhados e os requisitos estão no [guia principal](../README.md).

As verificações JavaScript/TypeScript podem ser executadas neste diretório:

```powershell
npx tsc --noEmit
npm test -- --runInBand
npm run lint
```
