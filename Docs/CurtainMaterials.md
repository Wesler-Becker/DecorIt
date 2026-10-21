# Materiais de cortina — revisão do catálogo

Os materiais de uso no aplicativo ficam em `Assets/DecorItCurtains/Materials/Active`.
As pastas `URP` e `BuiltIn` contêm as versões correspondentes aos dois pipelines.
As variantes de cor compartilham os mapas de uma mesma família.

## Escala de uma cortina de 300 × 200 cm

A malha `ProceduralCurtainMesh` usa UVs em metros de superfície. Mantenha
**Use Metric UVs** ativado e **Texture Repeats Per Meter = 1**. O material aplica
o multiplicador `1 / tileSizeMeters`, definido por família em `catalog.json`.
Assim, aumentar a cortina acrescenta repetições da trama sem aumentar seus fios.
O comprimento do tecido nas pregas é considerado pela malha e pode superar a
largura projetada de 3 m.

O Voil Trend usa uma amostra visual de **8 × 8 cm**, ou **12,5 repetições por metro**.
Os tecidos finos usam inicialmente 4 cm; listrados usam 24 cm e bordados, 60 cm.
São escalas artísticas ajustáveis. O catálogo não fornece uma régua de macrofotografia,
e a largura comercial do tecido não permite medir os fios fotografados.

## Trama, cor e transparência

- As fotografias maiores orientam o padrão; as cores das variantes do catálogo
  foram conservadas nos materiais. Não há calibração colorimétrica das fotos.
- O Voil Trend usa uma máscara autoral de filamentos finos enrolados, reconstruída
  a partir da referência da página 3. O preto significa espaço aberto entre fios.
  O albedo neutro e o normal são derivados dessa máscara, mantendo-os alinhados.
- Nas demais tramas abertas, a cobertura vem do alpha da imagem reconstruída.
  A luminosidade de uma fotografia não é tratada como prova de um furo.
- Nos tecidos finos sem uma separação confiável de vazados, há translucidez
  aproximada, modulada pela textura. Os blackouts são opacos.
- Os materiais transparentes usam mistura gradual e mipmaps: os pequenos vazados
  convergem para cobertura média a distância, em vez de desaparecer por um corte
  binário. Não escrevem profundidade nem projetam sombra opaca.
- O shader aceita luz no verso e renderiza ambos os lados. A transmissão é uma
  aproximação visual, não uma simulação física de tecido.
- Os mapas normais descrevem relevo aproximado da trama; as pregas continuam na
  geometria. Não são alturas medidas da amostra real.

## Fontes e reconstrução

`ArtSource/Curtains/rebuild-manifest.json` registra a fonte, o prompt utilizado
no gerador integrado `image_gen` e os parâmetros de cada família. As imagens
autorais ficam ao lado do manifesto, fora de `Assets`, para não entrarem no build.
As 56 fontes são mantidas na resolução nativa de **1254 × 1254 px** produzida pelo gerador. Ampliar um
arquivo para 2K ou 4K não criaria novos detalhes; a densidade visual vem também da
área física coberta pelo tile.

No menu **DecorIt**:

1. **Reconstruir mapas das fontes de tecido** refaz os mapas técnicos e os materiais.
2. **Criar ou atualizar materiais do catalogo** reaplica as cores, escalas,
   shaders e importação definidos em `catalog.json`.
3. **Atualizar opções de tecidos AR** atualiza a lista usada pelo aplicativo.

Essas operações sobrescrevem os ajustes dos materiais gerados. Para mudar o
padrão de uma família, edite o manifesto e reconstrua; para alterar apenas uma
configuração de material, edite o catálogo e reaplique os materiais.
Os arquivos `.meta` existentes são preservados, mantendo as referências dos assets.

## Android e revisão visual

Há mipmaps, filtragem trilinear e anisotropia 8. A importação Android mantém até
2048 px para Voil Trend, 1024 px para outras famílias translúcidas e 512 px para
blackouts e tecidos opacos. Cor e normal usam ASTC 4×4; máscaras usam R8.
Máscaras totalmente opacas ocupam apenas um pixel. O desempenho no aparelho AR
deve ser avaliado no dispositivo; uma renderização do Editor não o mede.

`Tools/Curtains/RenderMaterialPreview.cs` renderiza prévias reais pelo Unity,
sem abrir ou salvar a cena em uso. A vista geral representa uma cortina de
300 × 200 cm; o detalhe mostra aproximadamente 5 cm de altura nas tramas,
28 cm nos listrados e 50 cm nos bordados. As barras ao
fundo ajudam a observar a transparência. As imagens estão em `Docs/CurtainMaterialReview`.

Limitações visuais: sem amostras escaneadas, os padrões reconstruídos são
aproximações. Tecidos com bordados ou listras exigem conferir o tamanho do motivo
com uma amostra real. As fontes foram solicitadas com repetição contínua, mas
não representam um escaneamento têxtil com bordas calibradas. Superfícies transparentes com pregas muito sobrepostas
podem apresentar limitações de ordenação da mistura de transparência.

A cópia anterior à revisão foi preservada em `tmp/curtain-review/original-DecorItCurtains`.
