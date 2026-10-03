"""Trace the menu's patch installer in a small, bounded MIPS interpreter.

Does not run the game or modify an input ELF. Unknown instructions fail closed.
"""
from pathlib import Path
import struct
import json
import csv
import hashlib
import argparse

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/slus_menu_analysis'
U = (OUT / 'menu_unpacked.bin').read_bytes()
GAME = (OUT / 'game_segment.bin').read_bytes()

def trace(low=0, high=0, sight=0x000000A8, dot=0x00321EFA):
    r = [0] * 32
    r[4:8] = [high, low, sight, dot]
    r[29], r[31] = 0x1F00000, 0xFFFFFFFF
    mem, writes = {}, []
    def read(addr, size):
        result = bytearray()
        for a in range(addr, addr + size):
            if a in mem: result.append(mem[a])
            elif 0x800000 <= a < 0x800000 + len(U): result.append(U[a-0x800000])
            elif 0x100000 <= a < 0x100000 + len(GAME): result.append(GAME[a-0x100000])
            else: result.append(0)
        return int.from_bytes(result, 'little')
    pc, pending = 0x802A00, None
    for step in range(20000):
        if pc == 0xFFFFFFFF: return writes
        w = read(pc, 4)
        op, rs, rt, rd, sa, fn = w>>26, (w>>21)&31, (w>>16)&31, (w>>11)&31, (w>>6)&31, w&63
        imm = w&65535; signed = imm if imm<32768 else imm-65536
        dest, next_branch = None, None
        if op == 0:
            if fn == 0: dest=(rd, r[rt]<<sa)
            elif fn == 2: dest=(rd, (r[rt]&0xFFFFFFFF)>>sa)
            elif fn == 3: dest=(rd, (r[rt] if r[rt]<0x80000000 else r[rt]-0x100000000)>>sa)
            elif fn == 8: next_branch=r[rs]
            elif fn in (0x21,0x2D): dest=(rd,r[rs]+r[rt])
            elif fn in (0x23,0x2F): dest=(rd,r[rs]-r[rt])
            elif fn == 0x24: dest=(rd,r[rs]&r[rt])
            elif fn == 0x25: dest=(rd,r[rs]|r[rt])
            elif fn == 0x26: dest=(rd,r[rs]^r[rt])
            elif fn == 0x2B: dest=(rd,int(r[rs]<r[rt]))
            else: raise RuntimeError(f'Unknown special {pc:08X}: {w:08X}')
        elif op == 15: dest=(rt,imm<<16)
        elif op in (9,25): dest=(rt,r[rs]+signed)
        elif op == 12: dest=(rt,r[rs]&imm)
        elif op == 13: dest=(rt,r[rs]|imm)
        elif op == 14: dest=(rt,r[rs]^imm)
        elif op == 1 and rt in (0,1):
            negative=bool(r[rs]&0x80000000)
            if negative == (rt==0): next_branch=pc+4+signed*4
        elif op in (4,5,20,21):
            condition=(r[rs]==r[rt]) if op in (4,20) else (r[rs]!=r[rt])
            if condition: next_branch=pc+4+signed*4
            elif op in (20,21): pc+=4
        elif op in (2,3):
            next_branch=((pc+4)&0xF0000000)|((w&0x3FFFFFF)<<2)
            if op==3: r[31]=pc+8
        elif op in (32,33,35,36,37,55):
            size={32:1,33:2,35:4,36:1,37:2,55:8}[op]
            val=read((r[rs]+signed)&0xFFFFFFFF,size)
            if op in (32,33) and val&(1<<(size*8-1)): val-=1<<(size*8)
            dest=(rt,val)
        elif op in (40,41,43,44,45,63):
            if op in (44,45):
                addr=(r[rs]+signed)&0xFFFFFFFF
                k=addr&7
                raw=(r[rt]&0xFFFFFFFFFFFFFFFF).to_bytes(8,'little')
                if op==44: addr=addr&~7; raw=raw[7-k:]
                else: raw=raw[:8-k]
                size=len(raw); val=int.from_bytes(raw,'little')
            else:
                size={40:1,41:2,43:4,63:8}[op]
                addr=(r[rs]+signed)&0xFFFFFFFF
                val=r[rt]&((1<<(size*8))-1)
                raw=val.to_bytes(size,'little')
            for j,b in enumerate(raw): mem[addr+j]=b
            if not 0x1E00000<=addr<0x2000000:
                writes.append({'address':addr,'size':size,'value':val,'installer_pc':pc})
        else: raise RuntimeError(f'Unknown opcode {pc:08X}: {w:08X}')
        if dest and dest[0]: r[dest[0]]=dest[1]&0xFFFFFFFFFFFFFFFF
        r[0]=0
        pc = pending if pending is not None else pc+4
        pending=next_branch
    raise RuntimeError('Instruction budget exceeded')

def final_bytes(writes):
    result={}
    for w in writes:
        for j,b in enumerate(w['value'].to_bytes(w['size'],'little')):
            result[w['address']+j]=b
    return result

def words(data):
    result=[]
    for addr in sorted(set(a&~3 for a in data)):
        original=GAME[addr-0x100000:addr-0x100000+4] if 0x100000<=addr<=0x100000+len(GAME)-4 else b'\0'*4
        raw=bytes(data.get(addr+j,original[j]) for j in range(4))
        value=int.from_bytes(raw,'little')
        entry={'ram_address':f'{addr:08X}','value':f'{value:08X}'}
        if 0x100000<=addr<=0x100000+len(GAME)-4:
            entry.update(elf_offset=f'{addr-0x100000+0x1000:08X}',original=f'{int.from_bytes(original,"little"):08X}')
        result.append(entry)
    return result

def names():
    result={}
    for off in range(0x2F5DC8,0x2F6014,12):
        index,kind,selection,pointer=struct.unpack_from('<HHII',U,off)
        if kind:
            result[index]=U[pointer-0x800000:].split(b'\0')[0].decode('ascii')
    result[19]='TMP: silencer and stock'
    result[20]='TMP: stock'
    result[21]='TMP: silencer'
    return result

def export():
    baseline=final_bytes(trace())
    (OUT/'cheats').mkdir(exist_ok=True)
    catalogue=[]
    all_state=final_bytes(trace(0xFFFFFFFF,0x1FFFF))
    runtime_base=min(a for a in all_state if a<0x100000)
    runtime_end=max(a for a in all_state if a<0x100000)+1
    common=bytes(baseline.get(a,0) for a in range(runtime_base,runtime_end))
    (OUT/'runtime_common.bin').write_bytes(common)
    table=['# Catálogo de trapaças SLUS_211.34','',
           'Extração estática da rotina de instalação do menu CGH. Não foi validada em emulador.',
           '', 'Os endereços são da RAM do Emotion Engine. Os offsets são do ELF original fornecido, não do ELF compactado do menu.',
           '', '| ID | Opção | Uso | Endereços de instalação no jogo |',
           '|---|---|---|---|']
    rows=[]
    for index,name in sorted(names().items()):
        writes=trace(1<<index if index<32 else 0,1<<(index-32) if index>=32 else 0)
        state=final_bytes(writes)
        # Full game writes, including bytes unchanged relative to original.
        game_data={a:b for a,b in state.items() if a>=0x100000}
        game_words=words(game_data)
        extra={a:b for a,b in state.items() if a<0x100000 and not 0xF0000<=a<0xF0008 and baseline.get(a)!=b}
        runtime=bytes(state.get(a,0) for a in range(runtime_base,runtime_end))
        flag_only=not game_words
        jumps=[]
        for w in game_words:
            value=int(w['value'],16)
            if value>>26 in (2,3):
                target=(value&0x3FFFFFF)<<2
                if runtime_base<=target<0x100000:jumps.append(f'{target:08X}')
        kind='runtime_continuo' if flag_only else 'hook_com_auxiliar' if jumps or extra else 'patch_direto'
        record={'id':index,'name':name,'kind':kind,
                'low_mask':f'{(1<<index) if index<32 else 0:08X}',
                'high_mask':f'{(1<<(index-32)) if index>=32 else 0:08X}',
                'game_writes':game_words,'auxiliary_overrides':words(extra),
                'hook_targets':sorted(set(jumps)),
                'raw_installer_trace':[{**w,'address':f'{w["address"]:08X}','value':f'{w["value"]:0{w["size"]*2}X}','installer_pc':f'{w["installer_pc"]:08X}'} for w in writes]}
        stem=f'{index:02d}'
        (OUT/'cheats'/f'{stem}.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
        (OUT/'cheats'/f'{stem}_runtime.bin').write_bytes(runtime)
        lines=[f'# {index}: {name}',f'# {kind}; RAM address / 32-bit value / original ELF offset / original word',
               f'# Auxiliary RAM image: load matching runtime.bin at {runtime_base:08X}; flags at 000F0000/000F0004. Continuous options require their dispatcher.']
        lines.extend(f'{w["ram_address"]} {w["value"]} {w["elf_offset"]} {w["original"]}' for w in game_words)
        (OUT/'cheats'/f'{stem}_patches.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8')
        addresses=', '.join(w['ram_address'] for w in game_words)
        label={'runtime_continuo':'runtime contínuo','hook_com_auxiliar':'hook + auxiliar','patch_direto':'patch direto'}[kind]
        table.append(f'| {index} | {name} | {label} | {addresses or "Flags em 000F0000/000F0004; ver seção de runtime"} |')
        catalogue.append({k:v for k,v in record.items() if k!='raw_installer_trace'})
        for w in game_words:rows.append({'id':index,'name':name,'kind':kind,**w})
    (OUT/'cheats_catalogue.json').write_text(json.dumps({'source_sha256':hashlib.sha256((ROOT/'_references/ELFs/SLUS_211.34_original').read_bytes()).hexdigest(),
         'runtime_base':f'{runtime_base:08X}','runtime_size':len(common),'cheats':catalogue},indent=2),encoding='utf-8')
    with (OUT/'cheats_catalogue.csv').open('w',newline='',encoding='utf-8-sig') as f:
        writer=csv.DictWriter(f,fieldnames=['id','name','kind','ram_address','value','elf_offset','original']);writer.writeheader();writer.writerows(rows)
    table.extend(['','## Reutilização','',
    '- `patch_direto`: as escritas no jogo não precisam das rotinas auxiliares extraídas. É possível aplicar os valores no ELF se os bytes originais e a versão forem compatíveis.',
    f'- `hook_com_auxiliar`: além das escritas do jogo, carregar o arquivo `cheats/ID_runtime.bin` em `0x{runtime_base:08X}`. Cada imagem inclui a infraestrutura comum, as flags e o código específico. Essa RAM fica abaixo do segmento carregável do ELF original: um editor que só troca bytes no arquivo não instala esse código. Um launcher ou segmento adicional precisa carregá-lo sem conflito. O bloco contínuo contém lacunas preenchidas com zero; o JSON registra apenas as escritas efetivas, preferível para um loader que já ocupe essa região.',
    '- `runtime_continuo`: IDs 3 (PTAS), 6 (braço Krauser), 22 (carga P.R.L.) e 48 (bottle caps) apenas configuram flags nesta rotina de instalação. A alteração efetiva acontece no código compartilhado em execução. Sua ligação completa ao dispatcher do loader ainda não foi rastreada: os arquivos dessas opções não são patches autônomos prontos.',
    '- A associação de nomes e índices vem da tabela do próprio menu. As variantes TMP usam os bits 19, 20 e 21; selecionar apenas uma.',
    '- Os arquivos runtime individuais não podem ser sobrepostos para combinar opções: isso zeraria flags ou código de outra opção. Gerar uma imagem combinada com o script usando as duas máscaras desejadas.',
    '- Para SLES ou outra versão, os endereços precisam ser remapeados. Mesmo tamanho de arquivo não comprova compatibilidade.',
    '', '## Conteúdo dos arquivos','',
    '`cheats_catalogue.csv`: índice por escrita com endereço RAM, valor, offset e valor original. `cheats_catalogue.json`: catálogo com máscaras, dependências e destinos dos hooks. `cheats/ID.json`: também contém cada escrita observada e o PC da instrução instaladora. `cheats/ID_patches.txt`: lista legível por opção. `cheats/ID_runtime.bin`: imagem auxiliar para RAM, não um ELF.',
    '', '## Verificação e limitações','',
    'A rotina MIPS foi interpretada com orçamento limitado, incluindo delay slots, chamadas internas e stores desalinhados SDL/SDR. Instruções desconhecidas interrompem a extração. Cada opção foi rastreada isoladamente; também foi rastreada uma seleção combinada de todos os bits. O segmento do jogo foi previamente comparado byte a byte com o original.',
    '', 'Isso verifica o comportamento do instalador, não o efeito de cada trapaça durante uma partida. Não há garantia de ausência de conflito com modificações preexistentes. O código auxiliar conserva endereços absolutos da versão SLUS: mover a imagem de RAM exige relocar suas referências e os hooks.'])
    (OUT/'trapaças.md').write_text('\n'.join(table)+'\n',encoding='utf-8')
    print('Exported',len(catalogue),'code positions;',len(rows),'game words; runtime',hex(runtime_base),len(common),'bytes')
    print('Direct:',[c['id'] for c in catalogue if c['kind']=='patch_direto'])
    print('Continuous:',[c['id'] for c in catalogue if c['kind']=='runtime_continuo'])

def combine(ids):
    if any(i not in names() for i in ids):raise ValueError('Unknown ID')
    if len(set(ids)&{19,20,21})>1:raise ValueError('Select only one TMP variant')
    low=sum(1<<i for i in set(ids) if i<32)
    high=sum(1<<(i-32) for i in set(ids) if i>=32)
    state=final_bytes(trace(low,high))
    catalogue=json.loads((OUT/'cheats_catalogue.json').read_text(encoding='utf-8'))
    base=int(catalogue['runtime_base'],16);size=catalogue['runtime_size']
    (OUT/'combined_runtime.bin').write_bytes(bytes(state.get(a,0) for a in range(base,base+size)))
    (OUT/'combined_patches.json').write_text(json.dumps({'ids':ids,'low_mask':f'{low:08X}','high_mask':f'{high:08X}',
        'runtime_base':f'{base:08X}','game_writes':words({a:b for a,b in state.items() if a>=0x100000}),
        'auxiliary_writes':words({a:b for a,b in state.items() if a<0x100000})},indent=2),encoding='utf-8')
    print('Combined resources generated for',ids)

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ids',help='Generate combined resources for comma-separated IDs')
    args=parser.parse_args()
    if args.ids:combine([int(i) for i in args.ids.split(',')])
    else:export()
