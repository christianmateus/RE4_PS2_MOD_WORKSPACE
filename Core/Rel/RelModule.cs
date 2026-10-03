using System.Buffers.Binary;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.Rel;

public sealed record RelHeader(
    uint RelocationOffset, uint RelocationCount, uint SymbolOffset, uint SymbolCount,
    uint ImageNameOffset, uint ConstructorsOffset, uint DestructorsOffset,
    uint ExportOffset, uint ExportCount, uint Alignment, uint DeclaredSize,
    uint NextModule, uint Prolog, uint Epilog);

public sealed record RelSymbol(int Index, string Name, uint NameOffset, uint Address,
    ushort Metadata, byte Kind, byte Flags)
{
    public bool IsImport => Index != 0 && Address == 0;
    public bool IsExport => Address != 0 && Kind is 2 or 3 or 4;
}

public sealed record RelRelocation(int Index, uint Offset, byte Type, int SymbolIndex, int Addend)
{
    public string TypeName => Type switch
    {
        2 => "R_MIPS_32", 4 => "R_MIPS_26", 5 => "R_MIPS_HI16",
        6 => "R_MIPS_LO16", 0 => "NONE", _ => $"UNKNOWN_{Type}"
    };
}

public sealed class RelModule
{
    public string SourcePath { get; }
    public byte[] Data { get; }
    public RelHeader Header { get; }
    public string ImageName { get; }
    public IReadOnlyList<RelSymbol> Symbols { get; }
    public IReadOnlyList<RelRelocation> Relocations { get; }
    public IReadOnlyList<uint> Exports { get; }

    private RelModule(string path, byte[] data, RelHeader header, string imageName,
        RelSymbol[] symbols, RelRelocation[] relocations, uint[] exports)
    {
        SourcePath = path;
        Data = data;
        Header = header;
        ImageName = imageName;
        Symbols = symbols;
        Relocations = relocations;
        Exports = exports;
    }

    public static RelModule Open(string path) => Parse(path, File.ReadAllBytes(path));

    public static RelModule Parse(string sourcePath, byte[] data)
    {
        if (data.Length < 0x3C || !data.AsSpan(0, 4).SequenceEqual("SNR2"u8))
            throw new InvalidDataException($"{sourcePath}: cabeçalho SNR2 ausente ou incompleto.");
        uint U32(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        var h = new RelHeader(U32(4), U32(8), U32(12), U32(16), U32(20), U32(24),
            U32(28), U32(32), U32(36), U32(40), U32(44), U32(48), U32(52), U32(56));
        if (h.DeclaredSize != data.Length)
            throw new InvalidDataException($"{sourcePath}: tamanho declarado 0x{h.DeclaredSize:X} difere dos {data.Length} bytes do arquivo.");
        if (h.Alignment == 0 || (h.Alignment & (h.Alignment - 1)) != 0)
            throw new InvalidDataException($"{sourcePath}: alinhamento SNR2 inválido: 0x{h.Alignment:X}.");
        CheckRange(data, h.RelocationOffset, (ulong)h.RelocationCount * 12, "realocações", sourcePath);
        CheckRange(data, h.SymbolOffset, (ulong)h.SymbolCount * 12, "símbolos", sourcePath);
        CheckRange(data, h.ExportOffset, (ulong)h.ExportCount * 4, "exports", sourcePath);
        string imageName = ReadAscii(data, h.ImageNameOffset, sourcePath);

        var symbols = new RelSymbol[checked((int)h.SymbolCount)];
        for (int i = 0; i < symbols.Length; i++)
        {
            int pos = checked((int)(h.SymbolOffset + (uint)i * 12));
            uint nameOffset = U32(pos), address = U32(pos + 4);
            ushort metadata = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(pos + 8, 2));
            string name = ReadAscii(data, nameOffset, sourcePath);
            if (address != 0 && address >= data.Length)
                throw new InvalidDataException($"{sourcePath}: símbolo {i} ({name}) aponta para fora do REL.");
            symbols[i] = new RelSymbol(i, name, nameOffset, address, metadata, data[pos + 10], data[pos + 11]);
        }
        var relocations = new RelRelocation[checked((int)h.RelocationCount)];
        for (int i = 0; i < relocations.Length; i++)
        {
            int pos = checked((int)(h.RelocationOffset + (uint)i * 12));
            uint offset = U32(pos), info = U32(pos + 4);
            int symbolIndex = checked((int)(info >> 8));
            if (offset >= data.Length || symbolIndex >= symbols.Length)
                throw new InvalidDataException($"{sourcePath}: realocação {i} tem offset ou índice de símbolo inválido.");
            relocations[i] = new RelRelocation(i, offset, (byte)info, symbolIndex,
                BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos + 8, 4)));
        }
        var exports = new uint[checked((int)h.ExportCount)];
        for (int i = 0; i < exports.Length; i++)
            exports[i] = U32(checked((int)(h.ExportOffset + (uint)i * 4)));
        return new RelModule(sourcePath, data, h, imageName, symbols, relocations, exports);
    }

    internal static void CheckRange(byte[] data, uint offset, ulong length, string label, string source)
    {
        if (offset > data.Length || length > (ulong)data.Length - offset)
            throw new InvalidDataException($"{source}: tabela de {label} fora do arquivo (0x{offset:X}+0x{length:X}).");
    }

    internal static string ReadAscii(byte[] data, uint offset, string source)
    {
        CheckRange(data, offset, 1, "string", source);
        int start = (int)offset;
        int end = Array.IndexOf(data, (byte)0, start);
        if (end < 0) throw new InvalidDataException($"{source}: string sem terminador em 0x{offset:X}.");
        for (int i = start; i < end; i++)
            if (data[i] is < 0x20 or > 0x7E)
                throw new InvalidDataException($"{source}: string não ASCII em 0x{offset:X}.");
        return Encoding.ASCII.GetString(data, start, end - start);
    }
}
