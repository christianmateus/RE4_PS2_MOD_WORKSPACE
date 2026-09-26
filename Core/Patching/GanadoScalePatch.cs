using System.Buffers.Binary;
using RE4_PS2_MOD_WORKSPACE.Core.Afs;
using RE4_PS2_MOD_WORKSPACE.Core.Iso;

namespace RE4_PS2_MOD_WORKSPACE.Core.Patching;

public sealed record GanadoScalePatchResult(int EntryCount, int ChangedCount, float Multiplier, bool RestoredOriginal);

public static class GanadoScalePatch
{
    private static readonly HashSet<string> SharedEm10Modules = new(StringComparer.OrdinalIgnoreCase)
    {
        "em10.rel", "em11.rel", "em12.rel", "em13.rel", "em14.rel", "em15.rel", "em16.rel", "em17.rel",
        "em19.rel", "em1a.rel", "em1b.rel", "em1c.rel", "em1d.rel", "em1e.rel",
        "em1f.rel", "em20.rel"
    };
    private static readonly HashSet<string> DirectSpawnModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "em09.rel",
        "em21.rel", "em22.rel", "em23.rel", "em24.rel", "em25.rel",
        "em26.rel", "em27.rel", "em28.rel", "em29.rel", "em2a.rel",
        "em2b.rel", "em2c.rel", "em2d.rel", "em2e.rel", "em2f.rel",
        "em30.rel", "em31.rel", "em32.rel", "em34.rel", "em35.rel",
        "em36.rel", "em38.rel", "em39.rel", "em3a.rel", "em3b.rel",
        "em3c.rel", "em3d.rel", "emmark.rel", "em3f.rel",
        "em40.rel", "em41.rel", "em42.rel", "em43.rel", "em44.rel", "em45.rel",
        "em46.rel", "em47.rel", "em48.rel", "em49.rel", "em4a.rel", "em4b.rel",
        "em4c.rel", "em4d.rel", "em4e.rel", "em4f.rel"
    };
    private sealed record IndependentHook(int Offset,int BaseRegister,uint Native1,uint Native2);
    private sealed record IndependentRelPatch(int Cave,params IndependentHook[] Hooks);
    private static readonly Dictionary<string,IndependentRelPatch> IndependentRelPatches = new(StringComparer.OrdinalIgnoreCase)
    {
        ["em23.rel"]=new(0x875C,new IndependentHook(0x698,16,0xAE2000A4,0x240200FF)),
        ["em24.rel"]=new(0x73DC,new IndependentHook(0x5E0,17,0x8E220480,0x8C440010)),
        ["em25.rel"]=new(0xA6DC,new IndependentHook(0xB90,17,0xE6200188,0x962204B2),new IndependentHook(0x1EF8,16,0xE6000188,0xA60004B0)),
        ["em27.rel"]=new(0x82DC,new IndependentHook(0x874,16,0x26040320,0x44806000)),
        ["em29.rel"]=new(0x8CDC,new IndependentHook(0x678,16,0x26040320,0xC60C0440)),
        ["em2f.rel"]=new(0x8BBC,new IndependentHook(0x808,17,0x24020001,0xA2220118),new IndependentHook(0x87C,17,0x0220202D,0x0C000000)),
        ["em3c.rel"]=new(0xDD5C,new IndependentHook(0x4BB4,16,0x7BB00060,0x7BB10050)),
        ["em3f.rel"]=new(0x125DC,new IndependentHook(0x714,17,0x962204B2,0xA62204B0))
    };
    private static readonly IndependentHook[] LargeEm10Hooks =
    {
        new(0x8228,2,0x8FC40010,0x0C01EF8A),
        new(0xB710,2,0x8FC30004,0x246200C0)
    };
    private static readonly Dictionary<string,int> LargeEm10Caves = new(StringComparer.OrdinalIgnoreCase)
    {
        ["em40.rel"]=0x8B15C,["em41.rel"]=0x8B15C,["em42.rel"]=0x8A3DC,["em43.rel"]=0x896DC,
        ["em44.rel"]=0x8A65C,["em45.rel"]=0x8A75C,["em46.rel"]=0x8A75C,["em47.rel"]=0x8A75C,
        ["em48.rel"]=0x8A75C,["em49.rel"]=0x8A75C,["em4a.rel"]=0x8A75C,["em4b.rel"]=0x8A75C,
        ["em4c.rel"]=0x8A75C,["em4d.rel"]=0x8A75C,["em4f.rel"]=0x8A75C
    };
    private const int InitHook = 0x45DE4, ResetHook = 0x6184;
    private const int RelRoutineSearchStart = 0x51000, RelRoutineSearchEnd = 0x53000, ResetRoutineDelta = 0x68, RelRoutineSpace = 0xF4;
    private const int MainHook = 0xBE560, MainRoutine = 0x1F3680;
    private const int MainList2Hook = 0xBE83C, MainList2Routine = 0x1F3710;
    private const int MainEventHook = 0xBEA60, MainEventRoutine = 0x1F3600;
    private static readonly byte[] NativeInitHook = Words(0xE60100E4u,0xE60100E8u);
    private static readonly byte[] NativeResetHook = Words(0xE60100E4u,0xE60100E8u,0x6A0200E7u,0x6E0200E0u,0x6A0300EFu,0x6E0300E8u,0xB22200C7u);
    private static readonly byte[] NativeResetBlock = Convert.FromHexString("0D006228050040100B00622805004054E00001E6803F013C00088144");
    private static readonly byte[] NativeInitBlock = Convert.FromHexString("06006254E00001E6863F013C666621340008814400000000");
    private static readonly byte[] NativeMainHook = Words(0xA0B104C0u, 0x92020000u);
    private static readonly byte[] EmptyMainRoutine = new byte[0x90];
    private static readonly byte[] NativeMainList2Hook = Words(0xA21204C0u,0x92220000u);
    private static readonly byte[] EmptyMainList2Routine = new byte[0x90];
    private static readonly byte[] NativeMainEventHook = Words(0xA20604C0u,0xA2250000u);
    private static readonly byte[] EmptyMainEventRoutine = new byte[0x80];
    private sealed record LegacySite(int Offset, uint First, uint Second);
    private static readonly LegacySite[] LegacySites =
    {
        new(0x44BC,0x3C013F80u,0x44810000u), new(0x5DFC,0x3C013F80u,0x44810000u),
        new(0x20EDC,0x3C013F80u,0x44810000u), new(0x45E18,0x3C013F80u,0x44810000u)
    };

    public static GanadoScalePatchResult Apply(string isoPath, float multiplier)
    {
        if (!float.IsFinite(multiplier) || multiplier < .5f || multiplier > 3f)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "A escala deve estar entre 0,50× e 3,00×.");
        AfsImage afs=AfsService.OpenDefaultAfsFromIso(isoPath);
        AfsEntry[] modules=afs.Entries.Where(x=>!x.IsDummy&&(SharedEm10Modules.Contains(x.FileName)||DirectSpawnModules.Contains(x.FileName))).OrderBy(x=>x.Index).ToArray();
        if(modules.Length==0)throw new InvalidDataException("Nenhum REL compatível (em09 ou em10–em4F) foi encontrado em BIO4DAT.AFS.");
        IsoFileEntry exe=Iso9660Reader.ReadAllFiles(isoPath).FirstOrDefault(x=>!x.IsDirectory&&x.Name.StartsWith("SLES_",StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("O executável SLES não foi encontrado na raiz da ISO.");
        var writes=new List<(long Pos,byte[] Old,byte[] New)>();
        using var stream=new FileStream(isoPath,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
        PrepareMain(stream,exe,ReadR100SpecialScale(stream,afs),multiplier,writes);
        foreach(AfsEntry module in modules)
        {
            if(SharedEm10Modules.Contains(module.FileName))PrepareRel(stream,afs,module,multiplier,writes);
            else if(IndependentRelPatches.TryGetValue(module.FileName,out IndependentRelPatch? patch))PrepareIndependentRel(stream,afs,module,patch,multiplier,writes);
            else if(LargeEm10Caves.TryGetValue(module.FileName,out int cave))PrepareLargeEm10Rel(stream,afs,module,cave,multiplier,writes);
        }
        int changed=0;
        foreach(var w in writes){if(w.Old.AsSpan().SequenceEqual(w.New))continue;stream.Position=w.Pos;stream.Write(w.New);changed++;}
        stream.Flush(true);
        return new(modules.Length,changed,multiplier,false);
    }

    private static byte ReadR100SpecialScale(Stream s,AfsImage afs)
    {
        AfsEntry? esl=afs.Entries.FirstOrDefault(x=>!x.IsDummy&&x.FileName.Equals("emleon00.esl",StringComparison.OrdinalIgnoreCase));
        if(esl==null||esl.StoredSize<0x20)return 0;
        return Read(s,afs.IsoAfsEntry.DataOffset+esl.Offset+0x1C,1)[0];
    }

    private static void PrepareMain(Stream s,IsoFileEntry exe,byte r100Scale,float multiplier,List<(long,byte[],byte[])> writes)
    {
        if(exe.Size<MainList2Routine+EmptyMainList2Routine.Length)throw new InvalidDataException("O SLES é menor que a área esperada do patch individual.");
        long b=exe.DataOffset;byte[] wh=Words(JumpVirtual(0x2F2680u),0),oldWh=Words(JumpVirtual(0x29C958u),0),wr=BuildMainRoutine(multiplier);
        byte[] h=Read(s,b+MainHook,wh.Length),r=Read(s,b+MainRoutine,EmptyMainRoutine.Length);
        if(!h.AsSpan().SequenceEqual(NativeMainHook)&&!h.AsSpan().SequenceEqual(oldWh)&&!h.AsSpan().SequenceEqual(wh))throw new InvalidDataException("SLES_537.02 não possui a assinatura esperada em EmSetFromList. A ISO não foi alterada.");
        if(!r.All(x=>x==0)&&!r.AsSpan().SequenceEqual(wr)&&U32(r,0)!=0xA0B104C0)throw new InvalidDataException("A região ampliada do patch individual no SLES está ocupada. A ISO não foi alterada.");
        writes.Add((b+MainHook,h,wh));writes.Add((b+MainRoutine,r,wr));
        byte[] wh2=Words(JumpVirtual(0x2F2710u),0),oldWh2=Words(JumpVirtual(0x2A4854u),0),wr2=BuildMainList2Routine(multiplier);
        byte[] h2=Read(s,b+MainList2Hook,wh2.Length),r2=Read(s,b+MainList2Routine,EmptyMainList2Routine.Length);
        if(!h2.AsSpan().SequenceEqual(NativeMainList2Hook)&&!h2.AsSpan().SequenceEqual(oldWh2)&&!h2.AsSpan().SequenceEqual(wh2))throw new InvalidDataException("SLES_537.02 não possui a assinatura esperada em EmSetFromList2_sub. A ISO não foi alterada.");
        if(!r2.All(x=>x==0)&&!r2.AsSpan().SequenceEqual(wr2)&&U32(r2,0)!=0xA21204C0)throw new InvalidDataException("A segunda região ampliada do patch individual no SLES está ocupada. A ISO não foi alterada.");
        writes.Add((b+MainList2Hook,h2,wh2));writes.Add((b+MainList2Routine,r2,wr2));
        byte[] wh3=Words(JumpVirtual(0x2F2600u),0),wr3=BuildMainEventRoutine(r100Scale);
        byte[] h3=Read(s,b+MainEventHook,wh3.Length),r3=Read(s,b+MainEventRoutine,EmptyMainEventRoutine.Length);
        if(!h3.AsSpan().SequenceEqual(NativeMainEventHook)&&!h3.AsSpan().SequenceEqual(wh3))throw new InvalidDataException("SLES_537.02 não possui a assinatura esperada em EmSetEvent. A ISO não foi alterada.");
        bool knownEventRoutine=r3.All(x=>x==0)||U32(r3,0)==0xA20604C0;
        if(!knownEventRoutine)throw new InvalidDataException("A terceira região reservada do patch individual no SLES está ocupada. A ISO não foi alterada.");
        writes.Add((b+MainEventHook,h3,wh3));writes.Add((b+MainEventRoutine,r3,wr3));
    }

    private static void PrepareRel(Stream s,AfsImage afs,AfsEntry e,float multiplier,List<(long,byte[],byte[])> writes)
    {
        long b=afs.IsoAfsEntry.DataOffset+e.Offset;
        int initRoutine=FindRelRoutineArea(s,b,e);
        int resetRoutine=initRoutine+ResetRoutineDelta;
        RestoreScaleBlock(s,b,e.Index,0x6160,NativeResetBlock,writes);
        RestoreScaleBlock(s,b,e.Index,0x45DC8,NativeInitBlock,writes);
        foreach(LegacySite x in LegacySites){byte[] c=Read(s,b+x.Offset,8);uint a=U32(c,0),d=U32(c,4);if(!((a==0&&d==0)||((a&0xFFFF0000)==0x3C010000&&d==x.Second)))throw RelSignatureError(e,x.Offset);writes.Add((b+x.Offset,c,Words(x.First,x.Second)));}
        AddHook(s,b,e.Index,InitHook,NativeInitHook,Words(Branch(InitHook,initRoutine),0),writes);
        AddHook(s,b,e.Index,ResetHook,NativeResetHook,BuildPositionIndependentJump(ResetHook,resetRoutine),writes);
        AddRoutine(s,b,e.Index,initRoutine,BuildScaleRoutine(multiplier,initRoutine,InitHook+8,false),writes);
        AddRoutine(s,b,e.Index,resetRoutine,BuildScaleRoutine(multiplier,resetRoutine,ResetHook+NativeResetHook.Length,true),writes);
    }

    private static int FindRelRoutineArea(Stream s,long b,AfsEntry e)
    {
        int end=Math.Min(RelRoutineSearchEnd,checked((int)e.StoredSize));
        if(end-RelRoutineSearchStart<RelRoutineSpace)throw new InvalidDataException($"{e.FileName} (entrada {e.Index}) é menor que a área esperada para o patch. A ISO não foi alterada.");
        byte[] area=Read(s,b+RelRoutineSearchStart,end-RelRoutineSearchStart);
        for(int p=0;p+RelRoutineSpace<=area.Length;p+=4)
            if(U32(area,p)==0x920204C8&&U32(area,p+4)==0x1040000B)return RelRoutineSearchStart+p;
        for(int p=0;p+RelRoutineSpace<=area.Length;p+=4)
            if(area.AsSpan(p,RelRoutineSpace).IndexOfAnyExcept((byte)0)<0)return RelRoutineSearchStart+p;
        throw new InvalidDataException($"{e.FileName} (entrada {e.Index}) não possui espaço livre seguro para as rotinas de escala. A ISO não foi alterada.");
    }

    private static void PrepareIndependentRel(Stream s,AfsImage afs,AfsEntry e,IndependentRelPatch patch,float multiplier,List<(long,byte[],byte[])> writes)
    {
        long b=afs.IsoAfsEntry.DataOffset+e.Offset;
        for(int i=0;i<patch.Hooks.Length;i++)
        {
            IndependentHook hook=patch.Hooks[i];
            int routine=patch.Cave+i*0x90;
            byte[] native=Words(hook.Native1,hook.Native2);
            byte[] wantedHook=Words(Branch(hook.Offset,routine),0);
            byte[] currentHook=Read(s,b+hook.Offset,wantedHook.Length);
            if(!currentHook.AsSpan().SequenceEqual(native)&&!currentHook.AsSpan().SequenceEqual(wantedHook))
                throw new InvalidDataException($"{e.FileName} (entrada {e.Index}, hook +0x{hook.Offset:X}) não possui a assinatura esperada. A ISO não foi alterada.");
            byte[] wantedRoutine=BuildIndependentScaleRoutine(multiplier,hook.BaseRegister,new[]{hook.Native1,hook.Native2},routine,hook.Offset+8);
            byte[] currentRoutine=Read(s,b+routine,wantedRoutine.Length);
            if(!currentRoutine.All(x=>x==0)&&!currentRoutine.AsSpan().SequenceEqual(wantedRoutine)&&U32(currentRoutine,0)!=ScaleReadWord(hook.BaseRegister))
                throw new InvalidDataException($"{e.FileName} (entrada {e.Index}, rotina +0x{routine:X}) não possui espaço livre seguro. A ISO não foi alterada.");
            writes.Add((b+hook.Offset,currentHook,wantedHook));
            writes.Add((b+routine,currentRoutine,wantedRoutine));
        }
    }

    private static void PrepareLargeEm10Rel(Stream s,AfsImage afs,AfsEntry e,int cave,float multiplier,List<(long,byte[],byte[])> writes)
    {
        long b=afs.IsoAfsEntry.DataOffset+e.Offset;
        uint[][] nativeBlocks=
        {
            new uint[]{0x8FC40010,0x0C01EF8A,0,0x8FC20014,0x8C4300E0,0x3C040040,0x00641024},
            new uint[]{0x8FC30004,0x246200C0,0x8FC40000,0x248300E0,0x68640007,0x6C640000,0x6865000F}
        };
        for(int i=0;i<LargeEm10Hooks.Length;i++)
        {
            IndependentHook hook=LargeEm10Hooks[i];
            int routine=cave+i*0xB0;
            byte[] native=Words(nativeBlocks[i]);
            byte[] wantedHook=BuildPositionIndependentJump(hook.Offset,routine);
            byte[] currentHook=Read(s,b+hook.Offset,wantedHook.Length);
            if(!currentHook.AsSpan().SequenceEqual(native)&&!currentHook.AsSpan().SequenceEqual(wantedHook))
                throw new InvalidDataException($"{e.FileName} (entrada {e.Index}, hook longo +0x{hook.Offset:X}) não possui a assinatura esperada. A ISO não foi alterada.");
            byte[] wantedRoutine=BuildIndependentScaleRoutine(multiplier,hook.BaseRegister,nativeBlocks[i],routine,hook.Offset+wantedHook.Length);
            byte[] currentRoutine=Read(s,b+routine,wantedRoutine.Length);
            if(!currentRoutine.All(x=>x==0)&&!currentRoutine.AsSpan().SequenceEqual(wantedRoutine)&&U32(currentRoutine,0)!=ScaleReadWord(hook.BaseRegister))
                throw new InvalidDataException($"{e.FileName} (entrada {e.Index}, rotina longa +0x{routine:X}) não possui espaço livre seguro. A ISO não foi alterada.");
            writes.Add((b+hook.Offset,currentHook,wantedHook));
            writes.Add((b+routine,currentRoutine,wantedRoutine));
        }
    }

    private static void RestoreScaleBlock(Stream s,long b,int index,int offset,byte[] native,List<(long,byte[],byte[])> writes)
    {
        byte[] c=Read(s,b+offset,native.Length);uint a=U32(c,0),d=U32(c,4),m=U32(c,8);
        bool oldGlobal=(a&0xFFFF0000)==0x3C010000&&(d&0xFFFF0000)==0x34210000&&m==0x44810800;
        if(!c.AsSpan().SequenceEqual(native)&&!oldGlobal)throw new InvalidDataException($"REL compatível (entrada {index}, bloco +0x{offset:X}) não possui a assinatura esperada. A ISO não foi alterada.");
        writes.Add((b+offset,c,native));
    }

    private static void AddHook(Stream s,long b,int index,int offset,byte[] native,byte[] wanted,List<(long,byte[],byte[])> writes)
    {byte[] c=Read(s,b+offset,wanted.Length);bool legacy=(U32(c,0)>>26)==2&&U32(c,4)==0;if(!c.AsSpan().SequenceEqual(native)&&!c.AsSpan().SequenceEqual(wanted)&&!legacy)throw new InvalidDataException($"REL compatível (entrada {index}, hook +0x{offset:X}) não possui a assinatura esperada. A ISO não foi alterada.");writes.Add((b+offset,c,wanted));}
    private static void AddRoutine(Stream s,long b,int index,int offset,byte[] wanted,List<(long,byte[],byte[])> writes)
    {byte[] c=Read(s,b+offset,wanted.Length);if(!c.All(x=>x==0)&&U32(c,0)!=0x920204C8)throw new InvalidDataException($"REL compatível (entrada {index}, rotina +0x{offset:X}) não possui espaço livre. A ISO não foi alterada.");writes.Add((b+offset,c,wanted));}

    private static InvalidDataException RelSignatureError(AfsEntry e,int offset)=>new($"{e.FileName} (entrada {e.Index}, +0x{offset:X}) não possui a assinatura esperada. A ISO não foi alterada.");

    private static byte[] BuildMainRoutine(float global)=>BuildEm18SpawnRoutine(global,false);
    private static byte[] BuildMainList2Routine(float global)=>BuildEm18SpawnRoutine(global,true);
    private static byte[] BuildEm18SpawnRoutine(float global,bool list2)
    {
        uint f=unchecked((uint)BitConverter.SingleToInt32Bits(global));
        uint storeC0=list2?0xA21204C0u:0xA0B104C0u;
        uint readScale=list2?0x9222001Cu:0x9202001Cu;
        uint storeScale=list2?0xA20204C8u:0xA0A204C8u;
        uint readId=list2?0x92220001u:0x92020001u;
        uint storeX=list2?0xE60000E0u:0xE4A000E0u;
        uint storeY=list2?0xE60000E4u:0xE4A000E4u;
        uint storeZ=list2?0xE60000E8u:0xE4A000E8u;
        uint originalRead=list2?0x92220000u:0x92020000u;
        uint returnAddress=list2?0x1BD844u:0x1BD568u;
        return Pad(Words(
            storeC0,readScale,storeScale,readId,
            0x2443FFF0,0x2C630040,0x14600003,0x2443FFF7, // 0x10..0x4F, ou 0x09
            0x14600018,0,
            readScale,0x14400005,0,0x3C013F80,0x44810000,0x10000008,0,
            0x44820000,0,0x46800020,0,0x3C014180,0x44811000,0x46020003,
            0x3C010000|(f>>16),0x34210000|(f&0xFFFF),0x44811000,0,0x46020002,0,
            storeX,storeY,storeZ,originalRead,JumpVirtual(returnAddress),0),EmptyMainRoutine.Length);
    }
    private static uint ScaleReadWord(int baseRegister)=>0x90000000u|((uint)baseRegister<<21)|(baseRegister==2?3u:2u)<<16|0x04C8u;
    private static byte[] BuildIndependentScaleRoutine(float global,int baseRegister,IReadOnlyList<uint> native,int routineOffset,int returnOffset)
    {
        uint f=unchecked((uint)BitConverter.SingleToInt32Bits(global));
        uint scaleRegister=baseRegister==2?3u:2u;
        uint readScale=ScaleReadWord(baseRegister);
        uint loadX=0xC40000E0u|((uint)baseRegister<<21);
        uint storeX=0xE40000E0u|((uint)baseRegister<<21);
        uint storeY=0xE40000E4u|((uint)baseRegister<<21);
        uint storeZ=0xE40000E8u|((uint)baseRegister<<21);
        uint branchCustom=0x14000009u|(scaleRegister<<21);
        uint moveScaleToF0=0x44800000u|(scaleRegister<<16);
        var words=new List<uint>
        {
            readScale,branchCustom,0,
            loadX,0x3C010000|(f>>16),0x34210000|(f&0xFFFF),0x44811000,0,0x46020002,0x10000010,0,
            moveScaleToF0,0,0x46800020,0,0x3C014180,0x44811000,0,0x46020003,0,
            0x3C010000|(f>>16),0x34210000|(f&0xFFFF),0x44811000,0,0x46020002,0,
            storeX,storeY,storeZ
        };
        words.AddRange(native);
        int jumpOffset=routineOffset+words.Count*4;
        if(returnOffset>=jumpOffset-0x1FFFC&&returnOffset<=jumpOffset+0x20000){words.Add(Branch(jumpOffset,returnOffset));words.Add(0);}
        else words.AddRange(BuildPositionIndependentJump(jumpOffset,returnOffset).Chunk(4).Select(x=>BinaryPrimitives.ReadUInt32LittleEndian(x)));
        return Words(words.ToArray());
    }
    private static byte[] BuildMainEventRoutine(byte scale)=>Pad(Words(
        0xA20604C0,0xA2250000,             // instruções substituídas
        0x92220001,0x24030012,0x14430015,0,// id == em12
        0x92220003,0x24030013,0x14430011,0,// set == 0x13
        0x8E220004,0x3C032100,0x34630020,0x1443000C,0,// flag == 0x21000020
        0x8622000C,0x2403E1AD,0x14430008,0,// x == -7763
        0x86220010,0x2403F1BE,0x14430004,0,// z == -3650
        0x24020000u|scale,0x10000002,0,     // tamanho da entry 0
        0x0000102D,                         // demais EmSetEvent: automático
        0xA20204C8,JumpVirtual(0x1BDA68),0),EmptyMainEventRoutine.Length);
    private static byte[] BuildScaleRoutine(float global,int routineOffset,int ret,bool restoreResetInstructions)
    {
        uint f=unchecked((uint)BitConverter.SingleToInt32Bits(global));
        var w=new List<uint>{0x920204C8,0x1040000B,0,0x44820000,0,0x46800020,0x3C014180,0x44811000,0,0x46020003,0,0x46000842,0,
            0x3C010000|(f>>16),0x34210000|(f&0xFFFF),0x44810000,0,0x46000842,0,0xE60100E0,0xE60100E4,0xE60100E8};
        if(restoreResetInstructions)w.AddRange(new uint[]{0x6A0200E7,0x6E0200E0,0x6A0300EF,0x6E0300E8,0xB22200C7});
        int jumpOffset=routineOffset+w.Count*4;
        if(ret>=jumpOffset-0x1FFFC&&ret<=jumpOffset+0x20000){w.Add(Branch(jumpOffset,ret));w.Add(0);}
        else w.AddRange(BuildPositionIndependentJump(jumpOffset,ret).Chunk(4).Select(x=>BinaryPrimitives.ReadUInt32LittleEndian(x)));
        return Words(w.ToArray());
    }
    private static byte[] BuildPositionIndependentJump(int from,int target)
    {int ra=from+8,delta=target-ra;uint d=unchecked((uint)delta);return Words(0x04110001,0,0x3C010000|(d>>16),0x34210000|(d&0xFFFF),0x003F0821,0x00200008,0);}
    private static uint Branch(int from,int target){int words=(target-(from+4))/4;if(words<short.MinValue||words>short.MaxValue)throw new InvalidOperationException("Desvio REL fora do alcance.");return 0x10000000u|unchecked((ushort)(short)words);}
    private static uint Jump(int offset)=>0x08000000u|((uint)offset>>2);
    private static uint JumpVirtual(uint address)=>0x08000000u|((address>>2)&0x03FFFFFF);
    private static uint U32(byte[] b,int o)=>BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o,4));
    private static byte[] Pad(byte[] b,int n){byte[] r=new byte[n];b.CopyTo(r,0);return r;}
    private static byte[] Words(params uint[] w){byte[] b=new byte[w.Length*4];for(int i=0;i<w.Length;i++)BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(i*4,4),w[i]);return b;}
    private static byte[] Read(Stream s,long p,int n){byte[] b=new byte[n];s.Position=p;s.ReadExactly(b);return b;}
}
