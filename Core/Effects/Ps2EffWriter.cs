using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public static class Ps2EffWriter
{
    public static void Save(Ps2EffFile file, string? backupPath = null)
    {
        byte[] data = File.ReadAllBytes(file.SourcePath);
        foreach (EffGroup group in file.EstGroups.Concat(file.SstGroups)) WriteGroup(data, group);
        foreach (EffEntry entry in file.Entries) WriteEntry(data, entry);
        string temp = file.SourcePath + ".tmp";
        try
        {
            File.WriteAllBytes(temp, data);
            Ps2EffFile check = Ps2EffReader.Read(temp);
            if (check.EntryCount != file.EntryCount || check.EstGroups.Count != file.EstGroups.Count || check.SstGroups.Count != file.SstGroups.Count || check.TexturePackageCount != file.TexturePackageCount || new FileInfo(temp).Length != data.Length)
                throw new InvalidDataException("A validação do EFF salvo encontrou estrutura diferente da original.");
            if (!string.IsNullOrWhiteSpace(backupPath) && !File.Exists(backupPath)) { Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!); File.Copy(file.SourcePath, backupPath); }
            File.Move(temp, file.SourcePath, true);
            file.IsModified = false;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    internal static byte[] SerializeEntry(EffEntry e)
    {
        byte[] d = (byte[])e.RawData.Clone();
        WriteEntryAt(d, 0, e);
        return d;
    }

    private static void WriteGroup(byte[] d, EffGroup g)
    {
        int p=g.FileOffset; Ensure(p>=0&&p+Ps2EffReader.GroupHeaderSize<=d.Length,"Cabeçalho de grupo fora do arquivo.");
        W16(d,p,(ushort)g.Entries.Count);W16(d,p+8,g.Flags);d[p+0x0A]=g.DefaultParts;
        WF(d,p+0x0C,g.PositionX);WF(d,p+0x10,g.PositionY);WF(d,p+0x14,g.PositionZ);WF(d,p+0x18,g.RotationX);WF(d,p+0x1C,g.RotationY);WF(d,p+0x20,g.RotationZ);
        d[p+0x24]=g.Version;W16(d,p+0x26,g.CoreFlags);
    }

    private static void WriteEntry(byte[] d, EffEntry e)
    {
        Ensure(e.FileOffset>=0&&e.FileOffset+Ps2EffReader.EntrySize<=d.Length,"EffectEntry fora do arquivo.");
        WriteEntryAt(d,e.FileOffset,e);
    }

    private static void WriteEntryAt(byte[] d,int p,EffEntry e)
    {
        d[p]=e.EnabledMarker;d[p+1]=e.EspId;d[p+2]=e.ResourceId;d[p+3]=e.Type;W16(d,p+4,e.Time);d[p+6]=e.Parent;d[p+7]=e.ParentPart;W32(d,p+8,e.Flags);
        V3(d,p+0x0C,e.PositionX,e.PositionY,e.PositionZ);V3(d,p+0x18,e.RandomPositionX,e.RandomPositionY,e.RandomPositionZ);V3(d,p+0x24,e.SpeedX,e.SpeedY,e.SpeedZ);WF(d,p+0x30,e.SpeedDamping);V3(d,p+0x34,e.RandomSpeedX,e.RandomSpeedY,e.RandomSpeedZ);
        V3(d,p+0x40,e.AccelerationX,e.AccelerationY,e.AccelerationZ);V3(d,p+0x4C,e.RandomAccelerationX,e.RandomAccelerationY,e.RandomAccelerationZ);V3(d,p+0x58,e.RotationX,e.RotationY,e.RotationZ);V3(d,p+0x64,e.RandomRotationX,e.RandomRotationY,e.RandomRotationZ);
        V3(d,p+0x70,e.AngularVelocityX,e.AngularVelocityY,e.AngularVelocityZ);V3(d,p+0x7C,e.RandomAngularVelocityX,e.RandomAngularVelocityY,e.RandomAngularVelocityZ);WF(d,p+0x88,e.Width);WF(d,p+0x8C,e.Height);WF(d,p+0x90,e.RandomSize);WF(d,p+0x94,e.Grow);WF(d,p+0x98,e.GrowDamping);
        d[p+0x9C]=e.ColorR;d[p+0x9D]=e.ColorG;d[p+0x9E]=e.ColorB;d[p+0x9F]=e.ColorA;WF(d,p+0xA0,e.ColorStepR);WF(d,p+0xA4,e.ColorStepG);WF(d,p+0xA8,e.ColorStepB);WF(d,p+0xAC,e.ColorStepA);
        W16(d,p+0xB0,e.ColorMaxCount);W16(d,p+0xB2,e.ColorStartCount);W16(d,p+0xB4,e.PositionStartCount);W16(d,p+0xB6,e.SizeStartCount);W16(d,p+0xB8,e.Lifetime);W16(d,p+0xBA,e.LifeTime);
        d[p+0xBC]=e.PatternNumber;d[p+0xBD]=unchecked((byte)e.AnimationRate);W16(d,p+0xBE,e.AnimationCounter);d[p+0xC0]=e.ReleaseTime;d[p+0xC1]=e.GroupeNumber;d[p+0xC2]=e.BlendType;d[p+0xC3]=e.ShimmerType;d[p+0xC4]=e.ShimmerPower;d[p+0xC5]=e.MaskTextureId;d[p+0xC6]=e.DeleteFar;d[p+0xC7]=e.DeleteNear;
        d[p+0xC8]=e.Work8_0;d[p+0xC9]=e.Work8_1;d[p+0xCA]=e.Work8_2;d[p+0xCB]=e.Work8_3;W32(d,p+0xCC,e.Work32_0);W32(d,p+0xD0,e.Work32_1);W32(d,p+0xD4,e.Work32_2);
        V3(d,p+0xD8,e.Vector0X,e.Vector0Y,e.Vector0Z);V3(d,p+0xE4,e.Vector1X,e.Vector1Y,e.Vector1Z);V3(d,p+0xF0,e.Vector2X,e.Vector2Y,e.Vector2Z);d[p+0xFC]=e.WorkSpecial8_0;d[p+0xFD]=e.WorkSpecial8_1;d[p+0xFE]=e.WorkSpecial8_2;d[p+0xFF]=e.WorkSpecial8_3;
        W32(d,p+0x100,e.Reserved100);d[p+0x104]=e.GeneratorExtra0;d[p+0x105]=e.GeneratorExtra1;d[p+0x106]=e.GeneratorExtra2;d[p+0x107]=e.GeneratorExtra3;d[p+0x108]=(byte)e.Kind;d[p+0x109]=e.EspgenId;d[p+0x10A]=e.EspgenType;d[p+0x10B]=e.EspgenFlags;
        d[p+0x10C]=unchecked((byte)e.GeneratorWork8_0);d[p+0x10D]=unchecked((byte)e.GeneratorWork8_1);d[p+0x10E]=unchecked((byte)e.GeneratorWork8_2);d[p+0x10F]=unchecked((byte)e.GeneratorWork8_3);WI16(d,p+0x110,e.GeneratorWork16_0);WI16(d,p+0x112,e.GeneratorWork16_1);WI16(d,p+0x114,e.GeneratorWork16_2);WI16(d,p+0x116,e.GeneratorWork16_3);
        V3(d,p+0x118,e.GeneratorVectorX,e.GeneratorVectorY,e.GeneratorVectorZ);d[p+0x124]=unchecked((byte)e.GeneratorCurve0);d[p+0x125]=unchecked((byte)e.GeneratorCurve1);d[p+0x126]=unchecked((byte)e.GeneratorCurve2);d[p+0x127]=unchecked((byte)e.GeneratorCurve3);d[p+0x128]=e.GeneratorParameter0;d[p+0x129]=e.GeneratorParameter1;d[p+0x12A]=e.GeneratorParameter2;d[p+0x12B]=e.GeneratorParameter3;
    }

    private static void Ensure(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
    private static void W16(byte[]d,int p,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(d.AsSpan(p,2),v);
    private static void WI16(byte[]d,int p,short v)=>BinaryPrimitives.WriteInt16LittleEndian(d.AsSpan(p,2),v);
    private static void W32(byte[]d,int p,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(p,4),v);
    private static void WF(byte[]d,int p,float v){if(!float.IsFinite(v))throw new InvalidDataException($"EFF contém número inválido em 0x{p:X}.");BinaryPrimitives.WriteInt32LittleEndian(d.AsSpan(p,4),BitConverter.SingleToInt32Bits(v));}
    private static void V3(byte[]d,int p,float x,float y,float z){WF(d,p,x);WF(d,p+4,y);WF(d,p+8,z);}
}
