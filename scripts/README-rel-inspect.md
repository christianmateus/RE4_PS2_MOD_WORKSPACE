# Inspetor de REL SNR2 (RE4 PS2)

## Inspetor no aplicativo

Em **Ferramentas → REL: patches e inspeção → INSPECIONAR REL**, selecione um `.rel` e o executável principal `SLES_537.02` ou uma ISO que o contenha. A tela mostra o cabeçalho, símbolos, importações e realocações, e simula a ligação na base EE informada. A base inicial `0x00500000` é sintética; para comparar com uma captura do emulador, use a base real do módulo. O campo **Pasta de RELs** procura exports de possíveis provedores sem presumir que estejam carregados. Em **Carregados**, informe os provedores na ordem de carga com `caminho\arquivo.rel@0xBASE`, separados por ponto e vírgula. A tela é somente leitura e não grava no REL ou na ISO.

O núcleo C# em `Core/Rel` valida limites e tamanho do `SNR2`, lê símbolos e realocações MIPS, extrai exports da seção `.sndata` do ELF e resolve importações por nome **e** identificador de 16 bits. A simulação verifica `R_MIPS_32`, `R_MIPS_26`, `R_MIPS_HI16` e `R_MIPS_LO16`. Com os RELs e o SLES de referência, ela passou em `em21` (299/299) e nos 115 módulos sem dependências pendentes (236.373/236.373). Em `wep03`, mostrou 10/12 sem `wep02` carregado e 12/12 com `wep02` informado. Esta auditoria não executa as rotinas do jogo; a ordem efetiva de carregamento ainda deve ser conferida no emulador quando houver dependência entre RELs.

O `rel_inspect.py` lê módulos ProDG `SNR2` sem alterar os arquivos. Ele valida o cabeçalho, lê a tabela de símbolos, distingue símbolos locais de dependências externas e associa cada realocação ao símbolo correspondente. Também aceita o ELF `SLPS_000.00` para mostrar quais nomes externos existem nele. Os endereços do SLPS **não** são endereços do SLES.

Requer Python 3.10 ou posterior, sem pacotes adicionais. Copie os comandos abaixo no PowerShell. Eles entram na raiz deste workspace e usam os RELs em `_references\REL`:

Quando o caminho é uma pasta, ambos os scripts percorrem também suas subpastas. Depois de adicionar RELs, rode `map_slps_sles.py` novamente para atualizar o mapa e gere de novo os relatórios que desejar consultar.
O mapeador ignora cópias binárias idênticas ao contar usos de símbolos; o inspetor continua listando cada arquivo e seu caminho.

```powershell
Set-Location 'C:\Users\chris\source\repos\RE4_PS2_MOD_WORKSPACE'
$pythonExe = "$env:USERPROFILE\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
& $pythonExe scripts/rel_inspect.py '_references\REL\em21.rel' --symbols --limit 20
& $pythonExe scripts/rel_inspect.py '_references\REL' --debug-elf '_references\ELFs\SLPS_000.00' --json 'tmp\rel-report.json'
& $pythonExe scripts/rel_inspect.py '_references\REL\em42.rel' --relocations --filter EmInitFunc
```

## Mapa SLPS → SLES

O `map_slps_sles.py` cruza os nomes importados pelos RELs com a tabela `SNR2` embutida no próprio SLES. Essa tabela fornece nomes e endereços diretamente. Para símbolos ausentes nela, o script usa o SLPS de debug como apoio: compara instruções, funções vizinhas e chamadas `JAL`; para dados, procura bytes únicos e referências em funções mapeadas. O endereço do SLES embutido tem precedência, e o JSON registra candidatos heurísticos divergentes.
Como conferência do parser, os 7.876 símbolos com endereço na tabela `SNR2` embutida no SLPS coincidem exatamente com os endereços da tabela de símbolos ELF do próprio SLPS. O SLES não traz uma tabela ELF comparável; sua tabela `SNR2` contém 7.408 símbolos com endereço.

```powershell
& $pythonExe scripts/map_slps_sles.py
& $pythonExe scripts/rel_inspect.py '_references\REL\em21.rel' --debug-elf '_references\ELFs\SLPS_000.00' --sles-map 'tmp\slps-sles-map.json' --symbols --filter pG
& $pythonExe scripts/rel_inspect.py '_references\REL\em21.rel' --sles-map 'tmp\slps-sles-map.json' --relocations --filter EmInitFunc
& $pythonExe scripts/rel_inspect.py '_references\REL' --sles-map 'tmp\slps-sles-map.json' | Out-File -Encoding utf8 'tmp\rel-sles-summary.txt'
```

O mapa é salvo em `tmp\slps-sles-map.json` e em `tmp\slps-sles-map.txt`. `high` indica endereço direto da tabela `SNR2` do SLES ou várias evidências heurísticas concordantes; `medium` indica evidência heurística menor. Cada entrada JSON contém a origem e as evidências usadas. O mapa **não é um patch**: valide o endereço e as instruções antes de editar o SLES.

## Auditoria de ligação

Depois de atualizar o mapa, este comando gera a lista de importações pendentes por REL e simula as realocações de `em21.rel` numa cópia em memória:

```powershell
& $pythonExe scripts/rel_link_audit.py
& $pythonExe scripts/rel_link_audit.py --verify-all-ready
```

O resultado fica em `tmp\rel-link-audit.txt` e `tmp\rel-link-audit.json`. O teste usa a base **sintética** `0x00500000` para o REL e modela a ligação observada no SLES: alinhamento, ponteiros do cabeçalho, símbolos, exports e as realocações `R_MIPS_32`, `R_MIPS_26`, `R_MIPS_HI16` e `R_MIPS_LO16`. Ele não executa o código nem confirma a base ou a ordem de carregamento reais do jogo. Para testar outro módulo, acrescente `--module '_references\REL\armas\wep03.rel'` e, se desejar, `--base 0x00500000`. Um módulo com importações pendentes produz resultado `incomplete` e código de saída 2, mas ainda gera os relatórios. O campo `other_rel_export` indica que a definição foi encontrada em outro REL. Use `--loaded 'CAMINHO_DO_REL@0xBASE'` para modelar módulos já ligados, na ordem em que seriam carregados.

Por exemplo, `wep03.rel` importa `ObjMauser_init__FP4cObj`, que é definido em `wep02.rel`. O source code de GameCube em `re4-gamecube-source-code/src/wep02/objMauser.cpp` ajuda a identificar a função; a definição no REL PS2 é a evidência para a dependência entre módulos.

A cadeia do carregador, os endereços de suas rotinas e as limitações do teste estão em [README-rel-loader.md](README-rel-loader.md).

O comando com `--json` gera um inventário **completo**, incluindo cada símbolo e cada realocação. Com as subpastas, ele pode ficar bem maior que o inventário anterior de 63 RELs (cerca de 58 MB e 2,25 milhões de linhas). Para guardar apenas o resumo legível, execute:

```powershell
& $pythonExe scripts/rel_inspect.py '_references\REL' --debug-elf '_references\ELFs\SLPS_000.00' | Out-File -Encoding utf8 'tmp\rel-summary.txt'
```

O JSON completo inclui a lista `exports` com os valores brutos da tabela. O campo `unknown_28` foi identificado como o alinhamento exigido pelo carregador; os demais `unknown_*`, além de `metadata`, `kind`, `flags` e `extra_hex`, preservam os valores originais para pesquisa. Cada realocação expõe `addend` segundo o layout ELF MIPS `offset`, `r_info`, `addend`. O campo `debug_address` identifica apenas uma coincidência exata de nome no SLPS.

Referências usadas para o layout: [loader de REL](https://github.com/emoose/re4-research/blob/master/prodg-rel.py) e [template SNR2](https://github.com/emoose/re4-research/blob/master/file-formats/ProDG-SNR2.bt), confrontados com os RELs extraídos. Para comportamento e estruturas do jogo, consulte `re4-gamecube-source-code/src/game/read.cpp` e `re4-gamecube-source-code/include/read.h`; o formato binário do REL de GameCube é diferente.
