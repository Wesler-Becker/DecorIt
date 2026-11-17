# Interface visual

## React Native

- `mobile/src/theme.tsx`: paletas clara e escura, raios e provedor do tema. O botão de sol/lua no cabeçalho alterna o tema.
- `mobile/src/components/UI.tsx`: cabeçalhos, cartões, campos, botões e navegação auxiliar.
- `mobile/src/screens.tsx`: composição das telas e estilos derivados do tema, incluindo os atalhos do início e as ações dos clientes.
- `mobile/App.tsx`: navegação inferior, seletor de cliente do AR rápido e cores da navegação.

A escolha fica na chave `ui_theme` da tabela `app_meta`, usando o banco já existente. Não requer migration de esquema nem altera cadastros ou fotos. Se a gravação falhar, o aplicativo mantém o tema anterior e informa o erro.

## Unity

O Android transmite a escolha ao abrir o Unity, pelo extra `decorit_dark_theme`. `UnityARHostBridge` recebe essa preferência. O catálogo aplica a classe USS `dark` e usa as variáveis em `Assets/UI/Catalog/Catalog.uss`.

A câmera mantém seus controles sobre superfícies escuras, independentemente do tema das telas de atendimento. As cores ficam em `Assets/Scripts/ARInteractionUI.cs`. A iluminação e os materiais da prévia 3D continuam independentes do tema.

Mudanças na interface Unity precisam de nova exportação da biblioteca antes da build Android integrada. A tipografia utiliza a fonte nativa disponível no aplicativo; a fonte web e os ícones Material Symbols do HTML não são dependências do app. Os ícones continuam em Lucide no React Native.

## Verificação

Além dos testes de domínio e formulário, `mobile/src/theme.test.tsx` verifica a recuperação da preferência, a alternância e o tratamento de falha na escrita. Para avaliar visualmente, confira início, clientes, formulários, perfil, registros, galeria, seletor AR e catálogo em ambos os temas, especialmente com nomes longos e teclado aberto.
