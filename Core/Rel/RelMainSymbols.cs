using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Rel;

public sealed record RelMainSymbol(string Name, uint Address, ushort Metadata, byte Kind, byte Flags);

public sealed class RelMainSymbols
{
    private readonly Dictionary<(string Name, ushort Metadata), RelMainSymbol> lookup;
    public string SourcePath { get; }
    public IReadOnlyList<RelMainSymbol> Symbols { get; }

    private RelMainSymbols(string sourcePath, List<RelMainSymbol> symbols)
    {
        SourcePath = sourcePath;
        Symbols = symbols;
        lookup = new Dictionary<(string, ushort), RelMainSymbol>();
        foreach (RelMainSymbol symbol in symbols)
        {
            var key = (symbol.Name, symbol.Metadata);
            if (lookup.TryGetValue(key, out RelMainSymbol? previous) && previous.Address != symbol.Address)
                throw new InvalidDataException($"{sourcePath}: export SNR2 duplicado com endereços distintos: {symbol.Name}.");
            lookup[key] = symbol;
        }
    }

    public RelMainSymbol? Find(string name, ushort metadata) =>
        lookup.GetValueOrDefault((name, metadata));

    public static RelMainSymbols Open(string path) => Parse(path, File.ReadAllBytes(path));

    public static RelMainSymbols Parse(string sourcePath, byte[] elf)
    {
        if (elf.Length < 52 || !elf.AsSpan(0, 6).SequenceEqual(new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F', 1, 1 }) ||
            U16(elf, 18) != 8)
            throw new InvalidDataException($"{sourcePath}: esperado ELF32 MIPS little-endian.");
        uint sectionOffset = U32(elf, 0x20);
        ushort entrySize = U16(elf, 0x2E), sectionCount = U16(elf, 0x30), nameIndex = U16(elf, 0x32);
        if (entrySize < 40 || nameIndex >= sectionCount)
            throw new InvalidDataException($"{sourcePath}: tabela de seções ELF inválida.");
        RelModule.CheckRange(elf, sectionOffset, (ulong)entrySize * sectionCount, "seções ELF", sourcePath);
        int nameHeader = checked((int)(sectionOffset + (uint)nameIndex * entrySize));
        uint namesOffset = U32(elf, nameHeader + 16), namesSize = U32(elf, nameHeader + 20);
        RelModule.CheckRange(elf, namesOffset, namesSize, "nomes de seções ELF", sourcePath);
        uint sectionVa = 0, sectionFile = 0, sectionSize = 0;
        bool found = false;
        for (int i = 0; i < sectionCount; i++)
        {
            int pos = checked((int)(sectionOffset + (uint)i * entrySize));
            uint namePos = U32(elf, pos);
            if (namePos >= namesSize) continue;
            int start = checked((int)(namesOffset + namePos));
            if (!elf.AsSpan(start).StartsWith(".sndata\0"u8)) continue;
            sectionVa = U32(elf, pos + 12);
            sectionFile = U32(elf, pos + 16);
            sectionSize = U32(elf, pos + 20);
            RelModule.CheckRange(elf, sectionFile, sectionSize, ".sndata", sourcePath);
            found = true;
            break;
        }
        if (!found) throw new InvalidDataException($"{sourcePath}: seção .sndata ausente.");
        ReadOnlySpan<byte> sndata = elf.AsSpan((int)sectionFile, (int)sectionSize);
        int headerInSection = sndata.IndexOf("SNR2"u8);
        if (headerInSection < 0 || (ulong)headerInSection + 0x3C > sectionSize)
            throw new InvalidDataException($"{sourcePath}: cabeçalho SNR2 principal ausente na .sndata.");
        int header = checked((int)sectionFile + headerInSection);
        uint tableVa = U32(elf, header + 12), count = U32(elf, header + 16);
        int table = FilePosition(tableVa, (ulong)count * 12, "símbolos SNR2");
        var symbols = new List<RelMainSymbol>(checked((int)count));
        for (int i = 0; i < count; i++)
        {
            int pos = checked(table + i * 12);
            uint nameVa = U32(elf, pos), address = U32(elf, pos + 4);
            ushort metadata = U16(elf, pos + 8);
            if (nameVa == 0 || address == 0) continue;
            int nameAt = FilePosition(nameVa, 1, $"nome do símbolo {i}");
            int end = Array.IndexOf(elf, (byte)0, nameAt, checked((int)(sectionFile + sectionSize) - nameAt));
            if (end < 0) throw new InvalidDataException($"{sourcePath}: nome SNR2 sem terminador.");
            string name = System.Text.Encoding.ASCII.GetString(elf, nameAt, end - nameAt);
            byte kind = elf[pos + 10], flags = elf[pos + 11];
            if (kind is 2 or 3 or 4)
                symbols.Add(new RelMainSymbol(name, address, metadata, kind, flags));
        }
        return new RelMainSymbols(sourcePath, symbols);

        int FilePosition(uint address, ulong length, string label)
        {
            if (address < sectionVa || (ulong)address - sectionVa > sectionSize ||
                length > sectionSize - ((ulong)address - sectionVa))
                throw new InvalidDataException($"{sourcePath}: {label} fora da .sndata.");
            return checked((int)(sectionFile + address - sectionVa));
        }
    }

    private static ushort U16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
    private static uint U32(byte[] data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
}
