using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.Iso;

namespace RE4_PS2_MOD_WORKSPACE.Core.Afs;

public sealed class AfsEntry
{
    public int Index { get; set; }
    public uint Offset { get; set; }
    public uint StoredSize { get; set; }
    public uint ActualSize { get; set; }
    public long AllocatedSize { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsEmpty { get; set; }
    // The primary AFS table is the canonical source for payload size. Some original
    // PS2 images contain stale/invalid values in the optional filename TOC; using
    // that metadata as the payload size can turn a ~4 MB DAT into a fictitious
    // 800 MB entry and make extraction exceed its physical slot.
    public uint CurrentSize => IsEmpty ? 0 : StoredSize;
    public bool IsDummy => IsEmpty || StoredSize == 0;
    public long FreeSpace => Math.Max(0, AllocatedSize - CurrentSize);
    public override string ToString() => string.IsNullOrWhiteSpace(FileName) ? $"Entry {Index:D5}" : FileName;
}

public sealed class AfsImage
{
    public required string IsoPath { get; init; }
    public required IsoFileEntry IsoAfsEntry { get; init; }
    public required IReadOnlyList<AfsEntry> Entries { get; init; }
    public uint TocOffset { get; set; }
    public uint TocSize { get; set; }
}

public static class AfsService
{
    private const uint EmptySentinel = 0xFFFFF801;
    private const int Alignment = 0x800;

    public static IReadOnlyList<IsoFileEntry> FindAfsFiles(string isoPath)
    {
        return Iso9660Reader.ReadAllFiles(isoPath)
            .Where(x => !x.IsDirectory && x.Name.EndsWith(".AFS", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static AfsEntry? FindFirstValidEntryByName(AfsImage image, string fileName)
    {
        return image.Entries
            .Where(x => !x.IsDummy && x.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Index)
            .FirstOrDefault();
    }

    public static IReadOnlyList<AfsEntry> GetEmleonEslEntries(AfsImage image)
    {
        return image.Entries
            .Where(x => !x.IsDummy && x.FileName.StartsWith("emleon", StringComparison.OrdinalIgnoreCase) && x.FileName.EndsWith(".ESL", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Index)
            .ToArray();
    }

    public static IReadOnlyList<AfsEntry> GetUniqueValidDatEntries(AfsImage image)
    {
        return image.Entries
            .Where(x => !x.IsDummy && x.FileName.EndsWith(".DAT", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Index)
            .GroupBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<AfsEntry> GetUniqueValidEntries(AfsImage image)
    {
        return image.Entries
            .Where(x => !x.IsDummy && !string.IsNullOrWhiteSpace(x.FileName))
            .OrderBy(x => x.Index)
            .GroupBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static AfsImage OpenDefaultAfsFromIso(string isoPath, string preferredName = "BIO4DAT.AFS")
    {
        var files = FindAfsFiles(isoPath);
        var afs = files.FirstOrDefault(x => x.Name.Equals(preferredName, StringComparison.OrdinalIgnoreCase))
            ?? files.FirstOrDefault()
            ?? throw new InvalidDataException("Nenhum arquivo .AFS foi encontrado na ISO.");
        return OpenAfsFromIso(isoPath, afs);
    }

    public static AfsImage OpenAfsFromIso(string isoPath, IsoFileEntry afs)
    {
        using var fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        long baseOffset = afs.DataOffset;
        long logicalLength = afs.Size;
        fs.Position = baseOffset;
        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
        byte[] magic = br.ReadBytes(4);
        if (magic.Length != 4 || magic[0] != 0x41 || magic[1] != 0x46 || magic[2] != 0x53 || magic[3] != 0x00) throw new InvalidDataException("Assinatura AFS inválida.");
        uint count = br.ReadUInt32();
        if (count == 0 || count > 1_000_000) throw new InvalidDataException($"Quantidade de entradas suspeita: {count:N0}");
        if (8L + count * 8L + 8L > logicalLength) throw new InvalidDataException("A tabela do AFS ultrapassa os limites do arquivo.");
        var entries = new List<AfsEntry>((int)count);
        for (int i = 0; i < count; i++)
        {
            uint offset = br.ReadUInt32(); uint size = br.ReadUInt32();
            entries.Add(new AfsEntry { Index = i, Offset = offset, StoredSize = size, IsEmpty = size == EmptySentinel });
        }
        uint tocOffset = br.ReadUInt32(); uint tocSize = br.ReadUInt32();
        bool tocValid = tocOffset > 0 && tocOffset < logicalLength && tocSize > 0 && (long)tocOffset + tocSize <= logicalLength;
        if (tocValid) ReadToc(fs, br, baseOffset, tocOffset, tocSize, entries);
        CalculateAllocation(logicalLength, tocOffset, entries);
        return new AfsImage { IsoPath = isoPath, IsoAfsEntry = afs, Entries = entries, TocOffset = tocOffset, TocSize = tocSize };
    }

    private static void ReadToc(Stream fs, BinaryReader br, long baseOffset, uint tocOffset, uint tocSize, List<AfsEntry> entries)
    {
        long count = entries.Count;
        if (tocSize >= count * 48L)
        {
            fs.Position = baseOffset + tocOffset;
            foreach (var e in entries)
            {
                byte[] name = br.ReadBytes(32); if (name.Length < 32) break;
                e.FileName = DecodeName(name);
                br.ReadUInt16(); br.ReadUInt16(); br.ReadUInt16(); br.ReadUInt16(); br.ReadUInt16(); br.ReadUInt16();
                e.ActualSize = br.ReadUInt32();
            }
        }
        else if (tocSize >= count * 32L)
        {
            fs.Position = baseOffset + tocOffset;
            foreach (var e in entries) { byte[] name = br.ReadBytes(32); if (name.Length < 32) break; e.FileName = DecodeName(name); }
        }
    }

    private static string DecodeName(byte[] raw)
    {
        int zero = Array.IndexOf(raw, (byte)0); int len = zero >= 0 ? zero : raw.Length;
        return len <= 0 ? string.Empty : Encoding.ASCII.GetString(raw, 0, len).Trim();
    }

    private static void CalculateAllocation(long afsLength, uint tocOffset, List<AfsEntry> entries)
    {
        var valid = entries.Where(x => !x.IsEmpty && x.Offset > 0 && x.Offset < afsLength).OrderBy(x => x.Offset).ToList();
        for (int i = 0; i < valid.Count; i++)
        {
            var cur = valid[i]; long next;
            if (i + 1 < valid.Count) next = valid[i + 1].Offset;
            else if (tocOffset > cur.Offset) next = tocOffset;
            else next = cur.Offset + AlignUp(cur.CurrentSize, Alignment);
            cur.AllocatedSize = Math.Max(0, next - cur.Offset);
        }
    }

    public static void ExtractEntry(AfsImage image, AfsEntry entry, string destination)
    {
        if (entry.IsDummy) throw new InvalidOperationException("Não é possível extrair uma entrada dummy (tamanho real 0).");
        long absolute = image.IsoAfsEntry.DataOffset + entry.Offset;
        long size = entry.CurrentSize;
        if (entry.Offset + size > image.IsoAfsEntry.Size) throw new InvalidDataException("A entrada ultrapassa os limites físicos do AFS.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var input = new FileStream(image.IsoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        input.Position = absolute;
        byte[] buffer = new byte[1024 * 1024]; long remaining = size;
        while (remaining > 0) { int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining)); if (read <= 0) throw new EndOfStreamException(); output.Write(buffer, 0, read); remaining -= read; }
    }

    /// <summary>
    /// Injects an AFS entry and automatically grows its reserved slot when necessary.
    /// Growth is sector-aligned (0x800): the ISO tail is shifted forward, all affected
    /// ISO9660 LBAs are patched, later AFS entry offsets/TOC are advanced and the AFS
    /// directory-record size is updated. The original/source ISO is never chosen here;
    /// callers decide which ISO image is being modified.
    /// </summary>
    public static void InjectEntryAuto(AfsImage image, AfsEntry entry, string sourceFile, Action<string>? log = null)
    {
        if (entry.IsEmpty) throw new InvalidOperationException("Não é possível importar sobre uma entrada vazia.");
        if (!File.Exists(sourceFile)) throw new FileNotFoundException("O arquivo a ser injetado não existe.", sourceFile);

        long newSize = new FileInfo(sourceFile).Length;
        if (newSize <= entry.AllocatedSize)
        {
            InjectEntryInPlace(image, entry, sourceFile);
            return;
        }

        ExpandEntrySlot(image, entry, newSize, log);
        InjectEntryInPlace(image, entry, sourceFile);
    }

    private static void ExpandEntrySlot(AfsImage image, AfsEntry entry, long requiredSize, Action<string>? log)
    {
        if (requiredSize > uint.MaxValue)
            throw new InvalidDataException("O arquivo é grande demais para o campo de tamanho do AFS.");
        if (image.TocOffset == 0 || image.TocSize < image.Entries.Count * 48L)
            throw new InvalidDataException("A TOC de 48 bytes não foi encontrada. A realocação segura exige a TOC completa.");

        long oldAllocation = entry.AllocatedSize;
        long wantedAllocation = AlignUp(requiredSize, Alignment);
        long growBy = wantedAllocation - oldAllocation;
        if (growBy <= 0) return;
        growBy = AlignUp(growBy, Alignment);

        long afsBase = image.IsoAfsEntry.DataOffset;
        long shiftRelative = entry.Offset + oldAllocation;
        long shiftAbsolute = afsBase + shiftRelative;

        if ((shiftRelative % Alignment) != 0 || (shiftAbsolute % Alignment) != 0)
            throw new InvalidDataException(
                $"O limite físico do slot não está alinhado a 0x{Alignment:X}. " +
                $"Offset relativo: 0x{shiftRelative:X}. A realocação automática foi interrompida por segurança.");

        long oldAfsSize = image.IsoAfsEntry.Size;
        long newAfsSizeLong = checked(oldAfsSize + growBy);
        if (newAfsSizeLong > uint.MaxValue)
            throw new InvalidDataException("O AFS expandido ultrapassaria o limite de 4 GiB do ISO9660/AFS.");

        log?.Invoke($"Reserved Space insuficiente: {oldAllocation:N0} bytes; necessário: {requiredSize:N0} bytes.");
        log?.Invoke($"Expandindo slot AFS em {growBy:N0} bytes ({growBy / Alignment:N0} setor(es) de 0x{Alignment:X})...");

        // Physically create room at the exact end of the entry's current slot. This moves the
        // remainder of the AFS and every later ISO extent while keeping the AFS start LBA fixed.
        Iso9660Reader.InsertSpaceBeforeExtent(image.IsoPath, shiftAbsolute, growBy);

        // The AFS is now physically larger. Its ISO9660 directory record starts before the
        // insertion boundary, so its LBA is unchanged; only its data length must be patched.
        Iso9660Reader.UpdateFileSize(image.IsoPath, image.IsoAfsEntry, checked((uint)newAfsSizeLong));

        // Adjust the in-memory AFS layout to match the bytes that InsertSpaceBeforeExtent moved.
        foreach (AfsEntry other in image.Entries)
        {
            if (other.Index == entry.Index || other.IsEmpty) continue;
            if (other.Offset >= shiftRelative)
                other.Offset = checked((uint)(other.Offset + growBy));
        }

        uint newTocOffset = image.TocOffset;
        if (image.TocOffset >= shiftRelative)
            newTocOffset = checked((uint)(image.TocOffset + growBy));

        // Patch AFS header table + TOC pointer. All these structures live before the inserted
        // payload boundary except the TOC contents themselves (which were moved automatically).
        using (var iso = new FileStream(image.IsoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (var bw = new BinaryWriter(iso, Encoding.ASCII, leaveOpen: true))
        {
            long tableBase = afsBase + 8L;
            foreach (AfsEntry item in image.Entries)
            {
                if (item.IsEmpty) continue;
                iso.Position = tableBase + item.Index * 8L;
                bw.Write(item.Offset);
                // The main AFS table is the actual file size; reserved space comes from the
                // distance to the next offset. Keep all other sizes untouched here.
                bw.Write(item.Index == entry.Index ? checked((uint)requiredSize) : item.StoredSize);
            }

            long tocPointer = afsBase + 8L + image.Entries.Count * 8L;
            iso.Position = tocPointer;
            bw.Write(newTocOffset);
            bw.Write(image.TocSize);
            iso.Flush(true);
        }

        entry.StoredSize = checked((uint)requiredSize);
        entry.AllocatedSize = oldAllocation + growBy;
        image.TocOffset = newTocOffset;

        // Every later allocation is unchanged in size because both its start and following start
        // moved by the same delta. Recalculate all allocations to catch the last-entry/TOC case.
        if (image.Entries is List<AfsEntry> list)
            CalculateAllocation(newAfsSizeLong, newTocOffset, list);
        else
        {
            var mutable = image.Entries.ToList();
            CalculateAllocation(newAfsSizeLong, newTocOffset, mutable);
            foreach (var updated in mutable)
            {
                AfsEntry original = image.Entries.First(x => x.Index == updated.Index);
                original.AllocatedSize = updated.AllocatedSize;
            }
        }

        log?.Invoke($"Realocação concluída. Novo Reserved Space da entry: {entry.AllocatedSize:N0} bytes.");
    }

    public static void InjectEntryInPlace(AfsImage image, AfsEntry entry, string sourceFile)
    {
        if (entry.IsEmpty) throw new InvalidOperationException("Não é possível importar sobre uma entrada vazia.");
        if (!File.Exists(sourceFile)) throw new FileNotFoundException("O arquivo a ser injetado não existe.", sourceFile);

        long newSize = new FileInfo(sourceFile).Length;
        if (newSize > uint.MaxValue) throw new InvalidDataException("O arquivo é grande demais para o campo de tamanho do AFS.");
        if (newSize > entry.AllocatedSize)
            throw new InvalidDataException($"O novo arquivo possui {newSize:N0} bytes, mas o slot reservado permite {entry.AllocatedSize:N0} bytes. A injeção foi bloqueada para evitar corromper a ISO.");
        if (image.TocOffset == 0 || image.TocSize < image.Entries.Count * 48L)
            throw new InvalidDataException("A TOC de 48 bytes não foi encontrada. A importação segura exige a TOC para atualizar o Current Size.");

        long afsBase = image.IsoAfsEntry.DataOffset;
        long absoluteEntry = afsBase + entry.Offset;
        long absoluteSlotEnd = absoluteEntry + entry.AllocatedSize;
        long afsEnd = afsBase + image.IsoAfsEntry.Size;
        if (absoluteSlotEnd > afsEnd) throw new InvalidDataException("O slot físico da entrada ultrapassa os limites do AFS dentro da ISO.");

        using var iso = new FileStream(image.IsoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        // Keep the primary AFS table size synchronized with the TOC Current Size. Reserved
        // capacity is represented by the distance to the next entry offset, not by this field.
        iso.Position = afsBase + 8L + entry.Index * 8L + 4L;
        using (var sizeWriter = new BinaryWriter(iso, Encoding.ASCII, leaveOpen: true))
            sizeWriter.Write(checked((uint)newSize));
        entry.StoredSize = checked((uint)newSize);

        iso.Position = absoluteEntry;
        using (var input = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            input.CopyTo(iso, 1024 * 1024);

        long remaining = entry.AllocatedSize - newSize;
        if (remaining > 0) FillZeros(iso, remaining);

        DateTime now = DateTime.Now;
        long metadataOffset = afsBase + image.TocOffset + ((long)entry.Index * 48L) + 32L;
        if (metadataOffset < afsBase || metadataOffset + 16 > afsEnd)
            throw new InvalidDataException("O metadata da entrada está fora dos limites físicos do AFS.");

        iso.Position = metadataOffset;
        using (var bw = new BinaryWriter(iso, Encoding.UTF8, leaveOpen: true))
        {
            bw.Write((ushort)now.Year); bw.Write((ushort)now.Month); bw.Write((ushort)now.Day);
            bw.Write((ushort)now.Hour); bw.Write((ushort)now.Minute); bw.Write((ushort)now.Second);
            bw.Write((uint)newSize);
        }
        iso.Flush(true);
        entry.ActualSize = (uint)newSize;
    }

    private static void FillZeros(Stream stream, long count)
    {
        byte[] zeros = new byte[1024 * 1024];
        while (count > 0)
        {
            int n = (int)Math.Min(zeros.Length, count);
            stream.Write(zeros, 0, n);
            count -= n;
        }
    }

    private static long AlignUp(long value, int alignment) => ((value + alignment - 1) / alignment) * alignment;
}
