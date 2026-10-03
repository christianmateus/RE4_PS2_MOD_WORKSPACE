# Carregamento dos RELs no SLES_537.02

Esta análise usa o `SLES_537.02` e o `SLPS_000.00` em `_references\ELFs`, os RELs em `_references\REL` e o source code de GameCube para identificar o papel das rotinas do jogo. Os endereços abaixo são **do SLES**, salvo quando marcados SLPS. Nenhum executável ou REL foi alterado.

## Cadeia de ligação observada

| Rotina | SLES | SLPS debug | Evidência |
|---|---:|---:|---|
| `setEmModule` | `0x00262D28` | `0x00290500` | Lê `ReadModule+0x88`, chama `DLL_Link` e depois o prólogo em `REL+0x34`. |
| `DLL_Link` | `0x0022A9B8` | `0x00253340` | Encaminha para `OSLink`. |
| `OSLink` | `0x002544D0` | `0x00281218` | Chama `snDllLoaded` com o ponteiro do módulo. |
| `snDllLoaded` | `0x002A5838` | `0x002E2690` | Valida o cabeçalho, chama `relocate_dll`, registra o módulo e executa construtores. |
| `check_dll` | `0x002A49E0` | `0x002E1838` | Confere assinatura `SNR1`/`SNR2` e alinhamento da base. |
| `relocate_dll` | `0x002A5288` | `0x002E20E0` | Ajusta ponteiros do cabeçalho, símbolos, realocações e exports. |
| `lookup_symbol` | `0x002A4A38` | `0x002E1890` | Busca nome **e** identificador de 16 bits no executável e nos RELs já ligados. |
| `patch` | `0x002A4AE8` | `0x002E1940` | Aplica os tipos MIPS encontrados nos RELs. |
| `snInitDllSystem` | `0x002A57B8` | `0x002E2610` | Ativa as definições globais da tabela `SNR2` do executável. |

Os nomes internos `check_dll`, `relocate_dll`, `lookup_symbol` e `patch` vêm da tabela de símbolos do SLPS de debug. Seus endereços no SLES foram confirmados pela cadeia de chamadas e pelas instruções correspondentes. `DLL_Link`, `OSLink`, `snDllLoaded` e `snInitDllSystem` também aparecem por nome na tabela `SNR2` embutida no SLES.

`setEmModule` lê o ponteiro em `ReadModule+0x88` e o entrega ao carregador. `OSLink` passa esse mesmo ponteiro como **base** para `relocate_dll`; portanto, a base depende do bloco do REL carregado na memória e não é um endereço fixo do SLES. `check_dll` exige que a base respeite o alinhamento declarado no cabeçalho em `+0x28`, que vale `0x80` nos RELs inspecionados. O segundo argumento de `DLL_Link` não é propagado por `OSLink` nessa cadeia PS2; a saída temporária que `OSLink` fornece a `snDllLoaded` é usada para a tabela de exports. O comportamento do BSS no PS2 ainda requer observação em execução.

O [source de GameCube](../re4-gamecube-source-code/src/game/read.cpp) confirma o papel de `ReadModule`, do prólogo e do registro de `EmInitFunc`, mas usa outro formato de módulo e outra implementação de `OSLink`. Para os deslocamentos e a ligação acima, prevalece o código de máquina dos ELFs PS2.

## Resolução de símbolos e realocações

O SLES contém na seção `.sndata` uma tabela `SNR2` com 7.408 símbolos com endereço. No SLPS, os 7.876 símbolos com endereço dessa tabela coincidem exatamente com os endereços da tabela ELF do mesmo executável, validando a leitura dos registros. O mapa `tmp\slps-sles-map.json` registra o endereço, o identificador de 16 bits (`sles_metadata`) e o tipo de cada símbolo do SLES.

`lookup_symbol` percorre a tabela do executável e os RELs já ligados. Ele exige o identificador de 16 bits igual e, em seguida, compara o nome. A tabela principal é ativada por `snInitDllSystem`. A conferência dos RELs disponíveis encontrou **18.699** importações usadas com nome presente nessa tabela; todas têm o identificador correspondente. Entre módulos, a ordem de carregamento pode decidir qual definição será encontrada primeiro quando houver nomes duplicados.

Cada realocação é um registro ELF MIPS de 12 bytes: `r_offset` (32 bits), `r_info` (tipo nos 8 bits baixos e índice do símbolo nos 24 restantes) e `r_addend` (32 bits com sinal). `relocate_dll` resolve o símbolo, soma o addend e chama `patch`. Nos RELs inspecionados aparecem `R_MIPS_32`, `R_MIPS_26`, `R_MIPS_HI16` e `R_MIPS_LO16`. O script `rel_link_audit.py` modela essas quatro operações, o alinhamento, a atualização dos ponteiros do cabeçalho, a tabela de símbolos e os exports. O código do SLES em `patch` confirma a aritmética principal: endereço completo, campo de salto, metade alta arredondada com `0x8000` e metade baixa.

## Como reproduzir a auditoria

Na raiz do workspace, depois de rodar `scripts/map_slps_sles.py`:

```powershell
$pythonExe = "$env:USERPROFILE\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
& $pythonExe scripts/rel_link_audit.py
& $pythonExe scripts/rel_link_audit.py --verify-all-ready
& $pythonExe scripts/rel_link_audit.py --module '_references\REL\armas\wep03.rel' --base 0x00520000 --loaded '_references\REL\armas\wep02.rel@0x00510000' --json 'tmp\wep03-linked-audit.json' --text 'tmp\wep03-linked-audit.txt'
```

Com essas bases **sintéticas**, `em21.rel` aplica 299/299 realocações. A verificação em lote passa nos 115 RELs sem importações pendentes, com 236.373 realocações. `wep02.rel` aplica 482/482 e depois `wep03.rel` aplica 12/12. Sem `wep02.rel` carregado, `wep03.rel` para em 10/12 porque importa `ObjMauser_init__FP4cObj`, definido em `wep02.rel`. O [source de GameCube](../re4-gamecube-source-code/src/wep02/objMauser.cpp) ajuda a identificar essa função, e os RELs PS2 confirmam o vínculo binário.

O teste estático **não executa** o código do jogo. Para verificar uma partida real, capture o ponteiro `ReadModule+0x88` do REL em memória e compare os bytes finais. Passe esse ponteiro com `--base` para repetir a auditoria com a base efetiva; a ordem de módulos previamente carregados deve ser informada com `--loaded`.

## Verificação em execução: em21 no PCSX2 (29/09/2026)

Com o jogador diante do cão ferido, foi feita uma leitura **somente de memória** do processo PCSX2 de PID 25632. A ISO `Halloween Mod/Build/RE4_PS2_MOD.iso` inicia `SLUS_211.34` (`SYSTEM.CNF`, NTSC), não o SLES analisado acima. O executável extraído da ISO não é idêntico aos dois SLUS de referência, mas sua tabela principal `SNR2` fornece os mesmos 76 endereços usados pelo `em21.rel` que o `SLUS_211.34_original`.

A base da RAM EE no processo era `0x7FF7D0000000`. A tabela `EmReadModule` do **SLUS** começava em `0x00455E50`; no slot 2, `id=0x21`, `flag=0x7` e `pModule=0x01DCB700`. O cabeçalho nesse endereço é `SNR2`, com 299 realocações, 278 símbolos, 3 exports, alinhamento `0x80` e tamanho `0xCAC1` (51.905 bytes), como o arquivo `_references/REL/em21.rel`. Os seis ponteiros principais do cabeçalho tinham sido ajustados pela base real. Os 278 registros de símbolos, bem como as **299 palavras realocadas**, corresponderam exatamente à simulação usando a tabela `SNR2` do SLUS contido na ISO. O relatório detalhado está em `tmp/live-em21-verification.json` nesta estação.

Como controle, o mapa `tmp/slps-sles-map.json` gerado para `SLES_537.02` discordou de 77 endereços de símbolos e de 260 palavras realocadas do SLUS em execução. Isso é uma diferença entre executáveis regionais, não uma falha observada no carregador. Esta captura valida o modelo de ligação para o **SLUS** mostrado.

## Verificação em execução: SLES e em21 no PCSX2 (29/09/2026)

Após iniciar a versão SLES, foi feita uma nova leitura **somente de memória** do processo PCSX2 de PID 25820. A base da RAM EE no processo era `0x7FF7D0000000`. O cabeçalho principal `SNR2` estava em `0x0035EB80`; seus 7.408 exports com endereço, nome, identificador de 16 bits e tipo coincidiram com a tabela do arquivo `_references/ELFs/SLES_537.02`. O bit de ativação dos registros estava marcado na RAM, conforme esperado após `snInitDllSystem`.

A tabela `EmReadModule` estava em `0x004560D0`, exatamente o endereço obtido do SLES. No slot 2, `id=0x21`, `flag=0x7` e `pModule=0x01CA8D00`. O REL carregado nesse endereço tinha o cabeçalho `SNR2` do `_references/REL/em21.rel`. Os seis ponteiros principais do cabeçalho, os **278 registros de símbolos** e as **299 palavras realocadas** coincidiram com a simulação usando a base real e os exports do SLES; nenhuma divergência foi encontrada. As 299 realocações são 214 `R_MIPS_26`, 6 `R_MIPS_32`, 39 `R_MIPS_HI16` e 40 `R_MIPS_LO16`. O relatório detalhado está em `tmp/live-em21-sles-verification.json` nesta estação.

Essa captura confirma o modelo de resolução de símbolos e aplicação dessas quatro realocações para `em21.rel` na versão **SLES em execução**. Ela não verifica automaticamente todos os demais RELs nem o comportamento de suas rotinas após a ligação.

Na mesma sessão, os outros três slots de inimigo também foram conferidos contra o SLES de referência: `em12` em `0x01B84300` (538/538 registros de símbolos e 5.913/5.913 realocações), `em2a` em `0x01C3EC00` (286/286 e 294/294) e `em23` em `0x01CE6180` (261/261 e 196/196). Os quatro módulos somam **6.702 realocações conferidas sem divergência**. Os relatórios individuais estão em `tmp/live-em12-sles-verification.json`, `tmp/live-em2a-sles-verification.json` e `tmp/live-em23-sles-verification.json`. Esses quatro RELs resolveram suas importações pela tabela principal; um caso com dependência entre RELs ainda requer teste em execução.
