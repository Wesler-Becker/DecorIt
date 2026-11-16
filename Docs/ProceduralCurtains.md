# Cortinas procedurais

O tecido e gerado por `Assets/Scripts/ProceduralCurtainMesh.cs` e coordenado por
`Assets/Scripts/CurtainController.cs`. O prefab existente continua usando esses componentes.

## Ajustar no Unity

1. Abra `Assets/Prefabs/CurtainPrefab.prefab` ou selecione uma instancia na cena.
2. No `CurtainController`, escolha **Fabric Style**: **Liso**, **Natural**, **Wave**
   ou **Personalizado**. Personalizado preserva os campos de Fold Settings.
3. Defina **Width Cm** e **Height Cm** em centimetros. O topo permanece na origem.
4. **Opening** varia de 0 (fechada) a 1 (recolhida). O painel unico recolhe para a
   esquerda; no modelo duplo, cada painel recolhe para sua lateral.
5. **Fabric Naturalness** controla as irregularidades; **Fabric Seed** produz outro
   caimento repetivel. Os dois paineis recebem variacoes diferentes.

As mudancas aparecem no Editor, inclusive fora do Play. A malha so e recalculada
quando ha uma alteracao ou uma chamada explicita de configuracao.

Nos componentes `ProceduralCurtainMesh` de cada painel:

| Campo | Efeito |
| --- | --- |
| Fold Variation | Diferencas suaves de espacamento e profundidade entre pregas. |
| Drape | Curvatura e soltura das pregas ao longo da altura. |
| Wrinkle Strength | Rugas menores que se dissipam abaixo da fixacao. |
| Hem Width / Hem Lift | Bainha e pequena variacao da altura da barra. |
| Texture Repeats Per Meter | Repeticoes da textura por metro de superficie. |
| Segments Per Fold / Segments Per Meter | Resolucao horizontal e vertical. |

O controlador define as dimensoes, as pregas e a intensidade geral dos paineis;
edite esses valores no objeto raiz. Os ajustes finos acima permanecem em cada painel.

## Camadas e suportes no aplicativo

Na tela **Configurar cortina**, escolha **Varão** ou **Trilho**, simples ou duplo.
Use **+** para adicionar camadas (até oito) e selecione a aba da camada para editar
largura, altura, quantidade de painéis, tecido, estilo, pregas e caimento. A nova
camada começa com uma cópia independente da camada selecionada.

No suporte duplo, cada camada pode pertencer ao **suporte 1 (frente)** ou ao
**suporte 2 (trás)**. A abertura movimenta todas as camadas daquele grupo. Ao
voltar ao suporte simples, todas usam a abertura do suporte 1; a atribuição dos
grupos e a abertura do suporte 2 são preservadas para quando o duplo for reativado.
A ordem das camadas dentro de cada grupo segue a ordem das abas.

**Folga à frente**, em milímetros, é o espaço livre entre o tecido selecionado e
o anterior. O controlador reserva também a profundidade real das dobras e aplica
um mínimo de **2 mm**, mesmo quando solicitado zero. As posições são recalculadas
ao mudar medidas, caimento ou abertura. Essa restrição geométrica impede a
interseção das camadas da mesma cortina; não simula forças, vento ou colisões com
outros objetos AR.

`CurtainViewSettings` guarda a lista de `CurtainLayerViewSettings`, o tipo de
suporte e as duas aberturas. A configuração completa também acompanha
`VisitDraft.curtainSettings` no arquivo `last_ar_config.json`, usado pelo AR rápido.
Configurações antigas herdam as medidas e painéis compartilhados e convertem
o afastamento de centímetros para milímetros automaticamente.

## Configurar por codigo

```csharp
curtain.SetFabricStyle(CurtainController.FabricStyle.Natural);
curtain.SetDimensions(320f, 260f); // largura e altura em centimetros
curtain.SetOpening(0.35f);

// Ativa Personalizado: ondas por metro, profundidade em metros, perfil.
curtain.SetFoldSettings(5f, 0.06f, 1.1f);
```

## Geometria e materiais

- O tecido mantem a largura pedida e a altura ate o ponto mais baixo da barra.
- Ao abrir, a quantidade de pregas e mantida e sua profundidade aumenta por uma
  aproximacao geometrica. As UVs usam a superficie de referencia fechada para a
  estampa nao encolher com o painel.
- **Use Metric UVs** fica ativo por padrao. As texturas devem usar **Wrap Mode:
  Repeat**; o tiling do material continua multiplicando a escala UV. Desative a
  opcao para recuperar o mapeamento anterior de 0 a 1.
- Normais e tangentes sao atualizadas, permitindo usar mapas de normal da trama.
  Os materiais `DecorIt/Curtain URP` ja aceitam normal map e renderizam os dois lados.
- A forma representa tecido pendurado em repouso. Nao simula vento, forças de colisao,
  gravidade fisica nem conservacao exata da area durante a abertura.
- O gerador reutiliza a malha e os buffers enquanto a resolucao nao muda. Limita
  cada painel a 60 mil vertices para manter indices de 16 bits; cortinas muito
  grandes recebem menos subdivisoes por prega.
- Para materiais transparentes, os triângulos das dobras são ordenados do fundo
  para a frente antes de renderizar cada câmera. A ordenação reutiliza buffers,
  usa quatro passagens lineares e só se repete quando a malha ou o ângulo muda.
  Isso reduz os artefatos de transparência em vistas oblíquas sem alterar os
  materiais importados.

Os estilos sao pontos de partida visuais. O resultado final tambem depende do
material do tecido e da iluminacao da cena.
