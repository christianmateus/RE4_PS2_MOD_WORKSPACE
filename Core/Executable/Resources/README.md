# Catálogo CGH para SLUS-21134

`SlusCheats.json` foi extraído de `_references/ELFs/SLUS_211.34_mod_Menu`. Os bytes do segmento original embutido foram comparados com `SLUS_211.34_original` e são idênticos. O rastreamento do instalador está em `scripts/extract_slus_cheats.py` e `artifacts/slus_menu_analysis`.

O recurso contém palavras originais e modificadas do jogo, rotinas comuns e mudanças auxiliares por opção. A composição das 43 posições compatíveis simultâneas (45 habilitadas menos duas variantes alternativas da TMP) foi comparada com o resultado do instalador original.

O instalador do editor preserva o tamanho do ELF:

- `0x2CB000`: imagem das rotinas, carregada na RAM em `0x000EC4D0`, com 16.240 bytes.
- `0x2CF000..0x2D3000`: registro persistente das opções e palavras originais para remoção.
- `0x54`: segundo program header, antes preenchido com zeros; `e_phnum` passa a 2 somente quando há rotinas auxiliares.

A área de arquivo escolhida não pertence ao segmento carregável original e é verificada como inteiramente zerada antes da instalação. O código auxiliar fica abaixo da memória do segmento original, sem sobreposição. Nenhum arquivo de referência é necessário ao executar o editor: o catálogo é um recurso embutido.

A instalação é atômica no documento em memória. Ao mudar a seleção, o editor verifica a instalação existente, restaura suas palavras num buffer, verifica os bytes das novas opções e recompõe os hooks/flags. A remoção preserva mudanças fora das áreas registradas. Se os patches, rotinas ou registros foram alterados por terceiros, a operação é recusada.

IDs 3, 6, 22 e 48 estão desabilitados porque falta mapear o acionamento do runtime contínuo. SLES, SLPS e o SLUS compactado do próprio mod menu não são alvos suportados. Os efeitos em partida ainda precisam de validação em PCSX2/console.

Validação automatizada:

```powershell
dotnet run --project tools/SlusCheatValidation/SlusCheatValidation.csproj -- .
```

O teste cobre as 45 posições habilitadas individualmente, combinações, remoção parcial, preservação de modificações externas, conflitos, backup, reabertura, o ciclo completo em uma ISO9660 sintética e o fluxo real de salvar do formulário. Todos os arquivos de teste são cópias em `tmp/slus-cheat-validation`.
