# R100 — mapa GameCube → PlayStation 2 PAL

Alvo: `SLES_537.02` (PAL), sala `r100`, eventos `r100s03/s20/s30/s40`.

## Conclusão executiva

- Os dados de cenário do PS2 preservam a arquitetura lógica vista no código GameCube.
- Os 40 registros de `r100_14.AEV` têm índices contínuos `0x00..0x27`. Esses índices são os mesmos números usados por `SceAtHitCheck`, `SceAtSetEnable` e `SceAtDataSet_exec` em `r100.cpp`.
- Os quatro EVDs do PS2 usam o mesmo sistema de pacotes documentado por `event.cpp`.
- Os offsets de estruturas e o código nativo não são portáveis. IDs de área, fluxo, flags, nomes de eventos e semântica das chamadas são portáveis com alta confiança.
- O `SLES_537.02` não possui `.symtab` nem `.mdebug` utilizável. Nomes de funções aparecem como strings, mas obter endereços exige análise MIPS por referências.

## Índice do r100.dat

| Índice | Tipo | Papel |
|---:|---|---|
| 00 | CAM | câmeras normais da sala |
| 01 | SAT | colisão do jogador |
| 02–03 | LIT | iluminação |
| 04 | SMD | geometria/cenário |
| 05 | SMX | dados auxiliares do cenário |
| 06 | EFF | efeitos |
| 07 | TEX | texturas do cenário |
| 08 | ITM | catálogo de modelos de itens |
| 09 | ETM | catálogo de modelos de objetos |
| 10 | ETS | instâncias de objetos |
| 11 | EAR | áreas/recursos auxiliares |
| 12 | SAR | áreas/recursos auxiliares |
| 13 | EAT | colisão/atributos auxiliares |
| 14 | AEV | 40 áreas e gatilhos `0x00..0x27` |
| 15 | RTP | navegação: 66 nós e 160 conexões |
| 16 | MDT | mensagens da sala |
| 17 | CNS | configurações da sala |
| 18 | STB | tabela auxiliar |
| 19 | FSE | zonas sonoras |
| 20 | ESE | emissores sonoros |
| 21 | OSD | dados de objetos |
| 22 | BLK | blocos do mapa |
| 23–36 | BIN/TPL/DRA | modelos, animações e texturas locais |
| 37–48 | FCV/EMI/BIN/TPL | animações de câmera, enemy-info e recursos adicionais |

## Áreas AEV e funções do script

Os nomes abaixo têm três níveis de confiança:

- **Confirmado**: o índice aparece explicitamente em uma chamada `SceAt...` do código.
- **Provável**: tipo/posição/parâmetros do AEV combinam com o comportamento do código.
- **Desconhecido**: ainda falta identificar a rotina responsável.

| AEV | Tipo PS2 | Interpretação no script | Confiança |
|---:|---|---|---|
| 00 | Door Way | saída/transição de sala | provável |
| 01 | Cutscene | ravina/desfiladeiro; associado a `r100_GakeEvent` após o estado do carro | confirmado |
| 02 | Cutscene | gatilho de cutscene auxiliar | desconhecido |
| 03 | Grouped Enemy Spawn | grupo inimigo | provável |
| 04 | Map Block | desativado por `r100_Car_pos_move` | confirmado |
| 05 | Map Block | desativado por `r100_Car_pos_move` | confirmado |
| 06 | General Purpose | ao entrar, seta RSF 0 e pré-carrega s03/s20 | confirmado |
| 07 | General Purpose | junto com 08, seta RSF 1 | confirmado |
| 08 | General Purpose | junto com 07, seta RSF 1 | confirmado |
| 09 | Grouped Enemy Spawn | grupo inimigo `0x40` | provável |
| 0A | Cutscene | olhar para o carro, executa `r100_Sce_look`; habilitado após RSF 3 | confirmado |
| 0B | Cutscene | porta, executa `r100_MesDoor`; controlado por `r100_DoorCk` | confirmado |
| 0C | Cutscene | caminhão, executa `r100_MesTruck`; desativado após s03 | confirmado |
| 0D | General Purpose | usado pela condição que sela RSF 12; também mensagem do Ganado criada dinamicamente | confirmado |
| 0E | Unknown | região auxiliar próxima ao evento inicial | desconhecido |
| 0F | Unknown | região auxiliar próxima ao evento inicial | desconhecido |
| 10 | Message | mensagem 0x11 | provável |
| 11 | Message | mensagem 0x12 | provável |
| 12 | Message | mensagem 0x13 | provável |
| 13 | Message | mensagem 0x14, parâmetro secundário 8 | provável |
| 14 | Typewriter Save | máquina de escrever | confirmado pelo tipo |
| 15 | Cutscene | casa/Ganado, executa `r100_HouseEvent` | confirmado |
| 16 | Message | esqueleto; executa `r100_mes_gaikotu` | confirmado |
| 17 | Message | mensagem 0x31 | provável |
| 18 | Cutscene | mensagem da ponte, `r100_MesBrige` | confirmado |
| 19 | Cutscene | policial 1/carro, `r100_MesCar00` | confirmado |
| 1A | Cutscene | policial 2/carro, `r100_MesCar01` | confirmado |
| 1B | Cutscene | evento da ponte, `r100_EventBrige` | confirmado |
| 1C | Map Block | alternância antes/depois do evento s03 | confirmado |
| 1D | Message | mensagem 0x30 | provável |
| 1E | Map Block | alternância antes/depois do evento s03 | confirmado |
| 1F | Message | mensagem 0x32, parâmetro 0x0E | provável |
| 20 | Message | mensagem 0x32, parâmetro 0x10 | provável |
| 21 | Message | mensagem 0x32, parâmetro 0x0F | provável |
| 22 | Cutscene | reduz BGM perto do esqueleto, `r100_mes_gaikotu_bgm_down` | confirmado |
| 23 | Cutscene | restaura BGM, `r100_mes_gaikotu_bgm_up` | confirmado |
| 24 | Grouped Enemy Spawn | grupo inimigo `0x41` | provável |
| 25 | Map Block | desativado por `r100_Car_pos_move` | confirmado |
| 26 | Map Block | desativado por `r100_Car_pos_move` | confirmado |
| 27 | Map Block | desativado por `r100_Car_pos_move` | confirmado |

## Eventos EVD do PS2

| Evento | Tamanho | Pacotes | Câmeras | Função de sala |
|---|---:|---:|---:|---|
| r100s03 | 19.840 | 31 | 21 | encontro inicial/primeiro Ganado; callback `Evt_R100S03_Func` |
| r100s20 | 6.784 | 12 | 5 | carro/caminhão; callback `Evt_R100S20_Func` |
| r100s30 | 3.168 | 8 | 3 | evento curto sem callback de sala registrado |
| r100s40 | 17.248 | 21 | 8 | ravina/ponte; callback `Evt_R100S40_Func` |

IDs de pacote encontrados:

| ID | Handler reconstruído | Uso observado |
|---:|---|---|
| 00 | BeginEvt | início de todos os quatro eventos |
| 06 | Cam | uma entrada para cada corte/câmera |
| 0F | Str | stream/áudio de evento |
| 11 | Mes | mensagens/legendas |
| 1A | EndEvt | finalização do evento |
| 1B | EndPac | término do fluxo de pacotes |
| 20 | SetMdt | registra o MDT específico do evento |

O PS2 contém quatro EVDs para `r100`. O código GameCube mantém uma tabela de dez nomes. Os eventos auxiliares `s01`, `s02`, `s41`, `s42`, `s43` e `s44` foram eliminados, incorporados ou substituídos no port de PS2. Não se deve copiar a tabela GameCube literalmente.

## Flags persistentes da sala

| RSF | Papel observado |
|---:|---|
| 0 | jogador entrou na zona 06; inicia pré-leitura de eventos |
| 1 | jogador atravessou 07/08 |
| 3 | evento/olhar inicial concluído; libera área 0A |
| 4 | porta/janela avançou de estado |
| 10 | morte dos policiais/evento do carro concluído; principal divisor antes/depois |
| 12 | condição posterior ao olhar inicial concluída |
| 13 | inicialização única da sala |
| 14 | mensagem/estado da ravina já executado |
| 15 | Ganado da casa já ativado |

O papel exato de algumas flags ainda deve ser validado em runtime, mas os pontos de leitura e escrita estão identificados.

## Inimigos e grupos

- Ganado inicial: criado manualmente por `EmSetEvent`, ID `0x12`, set `0x13`, HP 500 no código GC.
- Emboscada: índices ESL/lista `03, 04, 05, 12, 13, 26, 27`.
- Ganado da casa: índice `0x25`.
- Grupo ativado por `r100_trap_set`: `09, 0A, 0B, 0C, 0D, 0E, 1D, 7A`, mais `06, 07, 08` sob condições adicionais.
- `r100_39.EMI` contém somente uma entrada desabilitada (`work_type 0`), portanto a lógica relevante desta sala depende principalmente das listas de inimigos/ESL e do código, não de pontos EMI ativos.

Os números acima são índices de listas usados pelo script, não IDs de tipo de inimigo. Eles precisam ser cruzados com o arquivo ESL correspondente para obter tipo, posição, HP e flags no PS2.

## Objetos ETS e itens ITA

- ETS: 11 instâncias.
  - objeto 00: 2 instâncias;
  - objeto 0D: 1 instância;
  - objeto 01: 5 instâncias;
  - objeto 02: 3 instâncias.
- ITA: 24 registros.
  - itens colocados diretamente: munições de handgun, ervas e outros;
  - itens ligados a inimigos: Spinel e granada;
  - itens ligados a ETS/recipientes: granadas, caixas de tesouro e erva verde.
- Ligações confirmadas ITA → ETS incluem instâncias `03, 04, 05, 06, 07, 08, 09, 19`.

Os nove primeiros registros ITA ocupam a mesma posição fora da área jogável e funcionam como definições/tabela auxiliar, não como nove pickups visíveis sobrepostos.

## Rotas RTP

`r100_15.RTP` contém 66 nós e 160 ligações direcionadas. É a malha de rotas usada pela IA/navegação da sala. Ela é independente dos pontos EMI; editar EMI não substitui a conectividade RTP.

## O que pode ser implementado no editor

1. Mostrar o índice hexadecimal de cada AEV como “SceAt ID”.
2. Aplicar nomes específicos da r100 aos IDs confirmados acima.
3. Exibir RSF, mensagem e grupo inimigo decodificados dos parâmetros AEV conhecidos.
4. Criar ligações navegáveis AEV → função do script → EVD.
5. Mostrar ligações ITA → ETS e alertar quando a instância referenciada não existir.
6. Adicionar um leitor EVD que liste pacotes, cortes, frames, câmeras, MDT e mensagens.
7. Adicionar um leitor EMI nativo; o formato é cabeçalho de 8 bytes seguido por registros de 0x40.

## Limites atuais

- `SLES_537.02` está sem símbolos endereçáveis. Os nomes preservados em strings comprovam a proximidade do port, mas ainda não dão o endereço de `R100Init`/`R100Main`.
- O código GameCube documenta comportamento, mas offsets de estruturas, alinhamento de vetores/matrizes e ABI não podem ser reutilizados.
- Para mapear endereços MIPS, o próximo passo é localizar referências às strings exclusivas de r100 e seguir suas funções no disassembly; depois validar em PCSX2 debugger.

