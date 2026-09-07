using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public static class Ps2EffWriter
{
    public static void Save(Ps2EffFile file, string? backupPath = null)
    {
        byte[] data = File.ReadAllBytes(file.SourcePath);
        foreach (EffEntry e in file.Entries) WriteEntry(data, e);
        string temp = file.SourcePath + ".tmp";
        try
        {
            File.WriteAllBytes(temp, data);
            Ps2EffFile check = Ps2EffReader.Read(temp);
            if (check.EntryCount != file.EntryCount || check.TexturePackageCount != file.TexturePackageCount || new FileInfo(temp).Length != data.Length)
                throw new InvalidDataException("A validação do EFF salvo encontrou estrutura diferente da original.");
            if (!string.IsNullOrWhiteSpace(backupPath) && !File.Exists(backupPath)) { Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!); File.Copy(file.SourcePath, backupPath); }
            File.Move(temp, file.SourcePath, true);
            file.IsModified = false;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void WriteEntry(byte[] d, EffEntry e)
    {
        int p=e.FileOffset; Ensure(p>=0&&p+Ps2EffReader.EntrySize<=d.Length); d[p]=1;d[p+1]=e.EspId;d[p+2]=e.ResourceId;
        W16(d,p+4,e.Time);d[p+6]=e.Parent;d[p+7]=e.ParentPart;W32(d,p+8,e.Flags);
        WF(d,p+0x0C,e.PositionX);WF(d,p+0x10,e.PositionY);WF(d,p+0x14,e.PositionZ);
        WF(d,p+0x18,e.RandomPositionX);WF(d,p+0x1C,e.RandomPositionY);WF(d,p+0x20,e.RandomPositionZ);
        WF(d,p+0x24,e.SpeedX);WF(d,p+0x28,e.SpeedY);WF(d,p+0x2C,e.SpeedZ);
        WF(d,p+0x40,e.AccelerationX);WF(d,p+0x44,e.AccelerationY);WF(d,p+0x48,e.AccelerationZ);
        WF(d,p+0x58,e.RotationX);WF(d,p+0x5C,e.RotationY);WF(d,p+0x60,e.RotationZ);
        WF(d,p+0x88,e.Width);WF(d,p+0x8C,e.Height);WF(d,p+0x94,e.Grow);
        d[p+0x9C]=e.ColorR;d[p+0x9D]=e.ColorG;d[p+0x9E]=e.ColorB;d[p+0x9F]=e.ColorA;
        W16(d,p+0xB8,e.Lifetime);W32(d,p+0xBA,e.AnimationSpeed);W16(d,p+0xC1,e.Blend);
    }
    private static void Ensure(bool ok){if(!ok)throw new InvalidDataException("EffectEntry fora do arquivo.");}
    private static void W16(byte[]d,int p,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(d.AsSpan(p,2),v);
    private static void W32(byte[]d,int p,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(p,4),v);
    private static void WF(byte[]d,int p,float v){if(!float.IsFinite(v))throw new InvalidDataException("EFF contém número inválido.");BinaryPrimitives.WriteInt32LittleEndian(d.AsSpan(p,4),BitConverter.SingleToInt32Bits(v));}
}
