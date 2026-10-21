param([string]$OutputDirectory = 'Deliverables')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
[IO.Directory]::CreateDirectory($destination) | Out-Null

function New-CurtainArchive([string]$Name, [IO.FileInfo[]]$Files, [string]$Readme) {
    $target = Join-Path $destination $Name
    # CreateNew preserves an earlier delivery if this script is run again.
    $stream = [IO.File]::Open($target, [IO.FileMode]::CreateNew)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $Files) {
            $relative = [IO.Path]::GetRelativePath($projectRoot, $file.FullName).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        $entry = $archive.CreateEntry('LEIA-ME.txt')
        $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.Write($Readme) } finally { $writer.Dispose() }
    }
    finally { $archive.Dispose(); $stream.Dispose() }
    $result = Get-Item -LiteralPath $target
    [pscustomobject]@{File=$result.FullName;SizeMB=[math]::Round($result.Length / 1MB, 1);Entries=$Files.Count + 1}
}

$materialFiles = @(Get-Item -LiteralPath (Join-Path $projectRoot 'Assets/DecorItCurtains.meta'))
$materialFiles += Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/DecorItCurtains') -File -Recurse |
    Where-Object { $_.Name -notin @('Height.png','Height.png.meta') }
$materialFiles += Get-Item -LiteralPath (Join-Path $projectRoot 'Docs/CurtainMaterials.md')
$materialFiles += Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Docs/CurtainMaterialReview') -File
$readme = @'
DECORIT - MATERIAIS DE CORTINA - 21/10/2026

Conteudo: 56 familias de tramas, 198 variantes de cor por pipeline, mapas
BaseColor/Normal/Opacity, shaders, importador, catalogo e previas do Unity.

USO NO PROJETO DECORIT
1. Extraia na raiz do projeto, preservando a estrutura Assets/DecorItCurtains.
2. Mantenha os arquivos .meta junto aos assets para conservar as referencias.
3. Use os materiais em Assets/DecorItCurtains/Materials/Active.
4. Se mudar de pipeline, execute DecorIt > Criar ou atualizar materiais do catalogo.

O projeto em E:/Unity/DecorIt ja recebeu estes arquivos; nao e necessario
reimportar este ZIP no mesmo projeto. Este pacote e uma copia para entrega.
Ao extrair sobre um projeto existente, os arquivos de mesmo nome serao substituidos.

ESCALA
Na malha ProceduralCurtainMesh do DecorIt: Use Metric UVs ativado e
Texture Repeats Per Meter = 1. O Voil Trend usa 12,5 repeticoes por metro.
Outra malha precisa de UVs correspondentes a metros de superficie.
A escala e visual e estimada, sem medicao de uma amostra fisica.

FONTES EDITAVEIS
Para executar Reconstruir mapas das fontes de tecido, extraia tambem
DecorIt-Cortinas-Fontes-2026-10-21.zip na raiz do projeto. Ele contem as
fontes geradas, prompts e parametros. Nao e necessario para usar os materiais.

Este pacote contem a biblioteca de tecidos, nao o aplicativo AR completo.
Os comandos da lista AR e a malha ProceduralCurtainMesh pertencem ao projeto DecorIt.
As previas sao renderizacoes do Editor; nao medem o desempenho no aparelho.

Guia: Docs/CurtainMaterials.md
Previas: Docs/CurtainMaterialReview/README.md
'@
New-CurtainArchive 'DecorIt-Cortinas-Materiais-2026-10-21.zip' $materialFiles $readme

$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'ArtSource/Curtains') -File -Recurse)
$sourceReadme = @'
DECORIT - FONTES DOS TECIDOS - 21/10/2026

Extraia na raiz do projeto para obter ArtSource/Curtains.
As 56 imagens nativas de 1254 x 1254 px, os prompts e os parametros estao incluidos.
As referencias generatedPath no manifesto registram os arquivos originais da
geracao. A reconstrucao usa o campo source, relativo ao projeto e incluido no ZIP.

Com a biblioteca de materiais instalada, o menu DecorIt > Reconstruir mapas das
fontes de tecido recria BaseColor, Normal, Opacity e os materiais.
Essa operacao sobrescreve os mapas e ajustes gerados dessas familias.

Estas fontes sao reconstrucoes visuais do catalogo, sem calibracao fisica.
'@
New-CurtainArchive 'DecorIt-Cortinas-Fontes-2026-10-21.zip' $sourceFiles $sourceReadme
