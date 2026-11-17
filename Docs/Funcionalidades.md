# Funcionalidades e fluxos

O DecorIt é um protótipo para organizar clientes e visualizar cortinas em AR. Esta página descreve os fluxos disponíveis na implementação atual, não funcionalidades planejadas.

## Clientes e cômodos

Na aba **Clientes**, é possível pesquisar, abrir o perfil e acessar as ações de editar e criar registro. O cadastro exige somente o nome; telefone, CEP, logradouro, complemento, bairro, cidade, estado e observações são opcionais. Telefone e CEP recebem máscara. Um CEP completo aciona uma consulta ao ViaCEP e, quando encontrado, preenche dados de endereço; a consulta depende de rede e não substitui a revisão dos dados.

O perfil reúne os dados cadastrados e permite editá-los. Um cliente pode ter cômodos sem registros. Para criar um cômodo, informe um nome diferente dos já existentes no perfil; a comparação desconsidera diferenças de maiúsculas, acentos e espaços repetidos. **Não informado** é reservado para a apresentação de fotos sem cômodo.

## Registros e fotos

O cadastro de registro tem duas etapas: dados e revisão. Primeiro, selecione um cômodo, informe a abertura e as observações desejadas e abra **Configurar produto e materiais**. No catálogo 3D, configure a cortina, incluindo largura e altura em centímetros para cada camada, e confirme em **Usar esta cortina**. Ao retornar, o formulário mostra as medidas da primeira camada, tecidos e suporte; a revisão apresenta esses dados antes de salvar. É necessário confirmar uma configuração com medidas positivas para avançar. Voltar do catálogo sem confirmar mantém a escolha anterior do rascunho.

Na revisão, é possível salvar o registro ou salvar e abrir a visualização AR. A configuração completa escolhida fica associada ao registro e é enviada ao Unity quando o registro é aberto em AR. No detalhe do registro, abertura e observações podem ser editadas individualmente.

As fotos podem ser consultadas pelo perfil, pelo cômodo, pelo registro e pela aba geral **Fotos**, conforme seus vínculos. O visualizador apresenta cliente, cômodo e data; **Sobre** expande informações disponíveis e ações relacionadas. É possível percorrer imagens lateralmente.

Uma foto feita sem cômodo é associada ao cliente e apresentada como **Não informado**. Na visualização, a opção de identificação permite escolher um cômodo existente ou criar um. Essa identificação altera o vínculo da foto; o agrupamento sem cômodo deixa de aparecer quando não houver fotos nele.

## AR rápido e câmera

O botão central **AR** na navegação abre um seletor de cliente. A busca permite selecionar um cliente ou criar um pelo nome quando não houver correspondência. O atalho no perfil usa o cliente já aberto. O AR rápido tenta reutilizar a última configuração de cortina; sem uma configuração disponível, usa os padrões. Fotos feitas por esse caminho começam sem cômodo e podem ser identificadas depois.

Na câmera, use **+ → Adicionar objeto** e toque na imagem para posicionar. Cada solicitação adiciona um objeto; depois é necessário solicitar novamente para adicionar outro. O botão de edição mostra os modos de gesto para ajustar o objeto. O menu superior contém as opções disponíveis de planos, modelo, acabamento e tecido. Os planos iniciam ocultos; a detecção é usada quando disponível, mas há posicionamento à frente da câmera como alternativa.

O app usa AR Foundation e ARCore no Android. Permissão de câmera, serviços de realidade aumentada e compatibilidade do aparelho afetam o funcionamento. O emulador permite conferir parte da interface, mas não comprova a qualidade de detecção, câmera ou desempenho em um telefone físico.

## Catálogo e configuração

A aba **Catálogo** abre a prévia sem confirmar uma seleção. Pelo cadastro de registro, o catálogo permite confirmar a configuração para aquele registro; no AR, permite configurar e usar a cortina na câmera. A prévia acompanha a rolagem e pode ser recolhida. Largura, altura e distância entre camadas solicitam teclado decimal; o conteúdo é filtrado ao confirmar o campo.

Ao abrir a configuração dentro do AR, a sessão de rastreamento e os objetos permanecem carregados.
Concluir ou voltar aplica as alterações ao objeto selecionado, preservando posição, rotação e escala;
os demais objetos continuam como estavam. Sem objeto posicionado, a volta prepara um único posicionamento.
A escolha de mostrar ou ocultar os planos também permanece. O primeiro posicionamento usa a última
configuração disponível no modelo procedural; o antigo prefab básico de teste foi removido.

A configuração permite medidas e painéis por camada, até oito camadas, tecidos, estilos de prega, caimento, distância entre camadas e suporte por varão ou trilho. O suporte pode ser simples ou duplo. No duplo, as camadas são atribuídas a um dos suportes e as camadas do mesmo grupo compartilham a abertura. A distância é medida entre os planos centrais; para impedir interseção, pregas podem ser comprimidas. A busca de tecidos procura em qualquer parte do nome e ignora acentos e maiúsculas.

Os materiais atualmente fazem parte do projeto e do catálogo Unity. Upload de modelos ou tecidos pelo usuário e sincronização remota não estão implementados. Para o funcionamento interno das camadas e limitações da simulação, consulte [Cortinas procedurais](ProceduralCurtains.md).

## Mapa das telas

| Tela | Acesso e função |
| --- | --- |
| Início | Acesso rápido a clientes, catálogo e fotos; o botão central abre o AR rápido. |
| Clientes e cadastro | Busca e ações dos clientes; formulário de dados e consulta de CEP. |
| Perfil do cliente | Dados, edição, cômodos, fotos e atalho para AR. |
| Cômodo | Lista de registros e fotos associadas; acesso à criação de registro. |
| Novo registro e revisão | Abertura, observações e seleção da cortina no catálogo, seguidos de revisão e gravação. |
| Detalhe do registro | Dados, edição de abertura e observações, fotos e acesso ao AR. |
| Fotos e visualizador | Galeria geral e visualização com data, detalhes expansíveis e navegação lateral. |
| Catálogo Unity | Prévia e configuração de medidas, camadas, tecidos e suporte; o botão de confirmação depende do fluxo de entrada. |
| Câmera AR | Posicionamento e ajuste de objetos, escolha de materiais e captura de fotos. |
