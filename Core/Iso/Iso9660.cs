using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.Iso
{
    public sealed class IsoFileEntry
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public uint Lba { get; set; }
        public uint Size { get; set; }
        public bool IsDirectory { get; set; }
        public long DirectoryRecordOffset { get; set; }
        public long DataOffset => (long)Lba * 2048L;
        public override string ToString() => FullPath;
    }

    public static class Iso9660Reader
    {
        private const int SectorSize = 2048;

        public static List<IsoFileEntry> ReadAllFiles(string isoPath)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] pvd = new byte[SectorSize];
            fs.Position = 16L * SectorSize;
            ReadExactly(fs, pvd, 0, pvd.Length);

            if (pvd[0] != 1 || Encoding.ASCII.GetString(pvd, 1, 5) != "CD001")
                throw new InvalidDataException("A imagem não possui um Primary Volume Descriptor ISO9660 válido.");

            int rootLength = pvd[156];
            if (rootLength < 34)
                throw new InvalidDataException("Registro do diretório raiz ISO9660 inválido.");

            IsoFileEntry root = ParseRecord(pvd, 156, 16L * SectorSize + 156, string.Empty);
            root.Name = string.Empty;
            root.FullPath = string.Empty;

            List<IsoFileEntry> result = new List<IsoFileEntry>();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ReadDirectory(fs, root, result, visited);
            return result;
        }

        private static void ReadDirectory(FileStream fs, IsoFileEntry dir, List<IsoFileEntry> result, HashSet<string> visited)
        {
            string key = $"{dir.Lba}:{dir.Size}";
            if (!visited.Add(key))
                return;

            long start = dir.DataOffset;
            long end = start + dir.Size;
            long pos = start;

            while (pos < end)
            {
                fs.Position = pos;
                int length = fs.ReadByte();

                if (length < 0)
                    break;

                if (length == 0)
                {
                    pos = ((pos / SectorSize) + 1) * SectorSize;
                    continue;
                }

                if (pos + length > end)
                    break;

                byte[] record = new byte[length];
                record[0] = (byte)length;
                ReadExactly(fs, record, 1, length - 1);

                IsoFileEntry entry = ParseRecord(record, 0, pos, dir.FullPath);

                pos += length;

                if (entry.Name == "." || entry.Name == "..")
                    continue;

                result.Add(entry);

                if (entry.IsDirectory)
                    ReadDirectory(fs, entry, result, visited);
            }
        }

        private static IsoFileEntry ParseRecord(byte[] data, int offset, long recordOffset, string parent)
        {
            int length = data[offset];
            if (length < 34)
                throw new InvalidDataException("Registro ISO9660 curto demais.");

            uint lba = BitConverter.ToUInt32(data, offset + 2);
            uint size = BitConverter.ToUInt32(data, offset + 10);
            byte flags = data[offset + 25];
            int nameLength = data[offset + 32];

            string name;
            if (nameLength == 1 && data[offset + 33] == 0)
                name = ".";
            else if (nameLength == 1 && data[offset + 33] == 1)
                name = "..";
            else
            {
                name = Encoding.ASCII.GetString(data, offset + 33, nameLength);
                int semicolon = name.IndexOf(';');
                if (semicolon >= 0)
                    name = name.Substring(0, semicolon);
            }

            string full = string.IsNullOrEmpty(parent) ? name : parent + "/" + name;

            return new IsoFileEntry
            {
                Name = name,
                FullPath = full,
                Lba = lba,
                Size = size,
                IsDirectory = (flags & 0x02) != 0,
                DirectoryRecordOffset = recordOffset
            };
        }



        /// <summary>
        /// Inserts sector-aligned space at <paramref name="shiftStart"/> by moving the ISO tail forward.
        /// This keeps all data before shiftStart at the same LBA (notably the AFS being expanded) and
        /// updates ISO9660 directory records for regular files that were moved.
        ///
        /// For safety this routine only operates when all directory extents, directory records and path
        /// tables are located before shiftStart. This is the normal layout used by the RE4 PS2 ISO and
        /// avoids having to rewrite directory/path-table structures while they are being moved.
        /// </summary>
        public static void InsertSpaceBeforeExtent(string isoPath, long shiftStart, long bytesToInsert, Action<long, long>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(isoPath)) throw new ArgumentNullException(nameof(isoPath));
            if (bytesToInsert <= 0) return;
            if ((shiftStart % SectorSize) != 0 || (bytesToInsert % SectorSize) != 0)
                throw new InvalidDataException("O deslocamento e o espaço inserido na ISO precisam estar alinhados a 0x800.");

            List<IsoFileEntry> entries = ReadAllFiles(isoPath);
            long oldLength = new FileInfo(isoPath).Length;
            if (shiftStart < 0 || shiftStart > oldLength)
                throw new InvalidDataException("O ponto de expansão está fora dos limites da ISO.");

            // Directory records must remain at their original physical positions so their LBA fields
            // can be patched safely after the tail move.
            IsoFileEntry? directoryAfterBoundary = entries
                .Where(x => x.IsDirectory && x.DataOffset >= shiftStart)
                .OrderBy(x => x.DataOffset)
                .FirstOrDefault();
            if (directoryAfterBoundary != null)
                throw new InvalidDataException($"Não é seguro expandir a ISO automaticamente porque o diretório '{directoryAfterBoundary.FullPath}' está depois do ponto de expansão.");

            IsoFileEntry? recordAfterBoundary = entries
                .Where(x => x.DirectoryRecordOffset >= shiftStart)
                .OrderBy(x => x.DirectoryRecordOffset)
                .FirstOrDefault();
            if (recordAfterBoundary != null)
                throw new InvalidDataException($"Não é seguro expandir a ISO automaticamente porque registros de diretório estão depois do ponto de expansão ('{recordAfterBoundary.FullPath}').");

            ValidateDescriptorStructuresBeforeBoundary(isoPath, shiftStart);
            List<IsoLbaReference> lbaReferences = CollectDirectoryLbaReferences(isoPath, shiftStart);

            using (FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                const int BufferSize = 8 * 1024 * 1024;
                byte[] buffer = new byte[BufferSize];
                long readEnd = oldLength;
                long totalToMove = Math.Max(0, oldLength - shiftStart);
                long moved = 0;
                fs.SetLength(checked(oldLength + bytesToInsert));

                // Copy backwards so source bytes are never overwritten before they are read.
                while (readEnd > shiftStart)
                {
                    int count = (int)Math.Min(buffer.Length, readEnd - shiftStart);
                    long source = readEnd - count;
                    fs.Position = source;
                    ReadExactly(fs, buffer, 0, count);
                    fs.Position = checked(source + bytesToInsert);
                    fs.Write(buffer, 0, count);
                    readEnd = source;
                    moved += count;
                    progress?.Invoke(moved, Math.Max(1L, totalToMove));
                }

                // Clear the newly inserted sector range. The expanded AFS will occupy part/all of it.
                fs.Position = shiftStart;
                long remaining = bytesToInsert;
                Array.Clear(buffer, 0, buffer.Length);
                while (remaining > 0)
                {
                    int count = (int)Math.Min(buffer.Length, remaining);
                    fs.Write(buffer, 0, count);
                    remaining -= count;
                }
                fs.Flush(true);
            }

            uint deltaLba = checked((uint)(bytesToInsert / SectorSize));
            uint shiftStartLba = checked((uint)(shiftStart / SectorSize));
            PatchDirectoryLbaReferences(isoPath, lbaReferences, shiftStartLba, deltaLba);

            UpdateVolumeSpaceSize(isoPath);

            // Final structural read catches stale/broken directory records before the caller writes AFS data.
            _ = ReadAllFiles(isoPath);
        }

        private sealed class IsoLbaReference
        {
            public long RecordOffset { get; set; }
            public uint Lba { get; set; }
        }

        private static List<IsoLbaReference> CollectDirectoryLbaReferences(string isoPath, long shiftStart)
        {
            List<IsoLbaReference> result = new List<IsoLbaReference>();
            HashSet<string> visited = new HashSet<string>();
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] descriptor = new byte[SectorSize];

            for (int index = 16; ; index++)
            {
                long descriptorOffset = (long)index * SectorSize;
                fs.Position = descriptorOffset;
                ReadExactly(fs, descriptor, 0, descriptor.Length);
                byte type = descriptor[0];
                if (Encoding.ASCII.GetString(descriptor, 1, 5) != "CD001")
                    throw new InvalidDataException("Descritor ISO9660 inválido durante a coleta de referências.");

                if (type == 1 || type == 2)
                {
                    int rootLength = descriptor[156];
                    if (rootLength >= 34)
                    {
                        uint rootLba = BitConverter.ToUInt32(descriptor, 158);
                        uint rootSize = BitConverter.ToUInt32(descriptor, 166);
                        result.Add(new IsoLbaReference { RecordOffset = descriptorOffset + 156, Lba = rootLba });
                        CollectDirectoryLbaReferencesRecursive(fs, rootLba, rootSize, shiftStart, result, visited);
                    }
                }

                if (type == 255) break;
            }

            return result
                .GroupBy(x => x.RecordOffset)
                .Select(x => x.First())
                .ToList();
        }

        private static void CollectDirectoryLbaReferencesRecursive(
            FileStream fs, uint directoryLba, uint directorySize, long shiftStart,
            List<IsoLbaReference> result, HashSet<string> visited)
        {
            long start = (long)directoryLba * SectorSize;
            if (start >= shiftStart)
                throw new InvalidDataException("Não é seguro expandir a ISO automaticamente porque um diretório está depois do ponto de expansão.");

            string key = $"{directoryLba}:{directorySize}";
            if (!visited.Add(key)) return;

            long end = start + directorySize;
            long pos = start;
            byte[] header = new byte[34];

            while (pos < end)
            {
                fs.Position = pos;
                int length = fs.ReadByte();
                if (length < 0) break;
                if (length == 0)
                {
                    pos = ((pos / SectorSize) + 1) * SectorSize;
                    continue;
                }
                if (length < 34 || pos + length > end)
                    throw new InvalidDataException("Registro de diretório ISO9660 inválido durante a expansão.");

                header[0] = (byte)length;
                ReadExactly(fs, header, 1, 33);
                uint lba = BitConverter.ToUInt32(header, 2);
                uint size = BitConverter.ToUInt32(header, 10);
                byte flags = header[25];
                int nameLength = header[32];
                int firstIdentifier = nameLength > 0 ? header[33] : -1;
                bool special = nameLength == 1 && (firstIdentifier == 0 || firstIdentifier == 1);

                result.Add(new IsoLbaReference { RecordOffset = pos, Lba = lba });

                if ((flags & 0x02) != 0 && !special)
                    CollectDirectoryLbaReferencesRecursive(fs, lba, size, shiftStart, result, visited);

                pos += length;
            }
        }

        private static void PatchDirectoryLbaReferences(
            string isoPath, List<IsoLbaReference> references, uint shiftStartLba, uint deltaLba)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true);

            foreach (IsoLbaReference reference in references)
            {
                if (reference.Lba < shiftStartLba) continue;
                uint newLba = checked(reference.Lba + deltaLba);
                fs.Position = reference.RecordOffset + 2;
                bw.Write(newLba);
                fs.Position = reference.RecordOffset + 6;
                WriteUInt32BigEndian(bw, newLba);
            }
            fs.Flush(true);
        }

        private static void ValidateDescriptorStructuresBeforeBoundary(string isoPath, long shiftStart)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] sector = new byte[SectorSize];

            for (int index = 16; ; index++)
            {
                long descriptorOffset = (long)index * SectorSize;
                if (descriptorOffset + SectorSize > fs.Length)
                    throw new InvalidDataException("Conjunto de descritores ISO9660 incompleto.");

                fs.Position = descriptorOffset;
                ReadExactly(fs, sector, 0, sector.Length);
                byte type = sector[0];
                if (Encoding.ASCII.GetString(sector, 1, 5) != "CD001")
                    throw new InvalidDataException("Descritor ISO9660 inválido durante a expansão da ISO.");

                if (type == 1 || type == 2)
                {
                    uint pathTableSize = BitConverter.ToUInt32(sector, 132);
                    uint littlePathLba = BitConverter.ToUInt32(sector, 140);
                    uint optionalLittlePathLba = BitConverter.ToUInt32(sector, 144);
                    uint bigPathLba = ReadUInt32BigEndian(sector, 148);
                    uint optionalBigPathLba = ReadUInt32BigEndian(sector, 152);

                    foreach (uint lba in new[] { littlePathLba, optionalLittlePathLba, bigPathLba, optionalBigPathLba })
                    {
                        if (lba != 0 && pathTableSize > 0 && ((long)lba * SectorSize) >= shiftStart)
                            throw new InvalidDataException("Não é seguro expandir a ISO automaticamente porque uma Path Table está depois do ponto de expansão.");
                    }

                    int rootLength = sector[156];
                    if (rootLength >= 34)
                    {
                        uint rootLba = BitConverter.ToUInt32(sector, 158);
                        if (((long)rootLba * SectorSize) >= shiftStart)
                            throw new InvalidDataException("Não é seguro expandir a ISO automaticamente porque o diretório raiz está depois do ponto de expansão.");
                    }
                }

                if (type == 255) break;
            }
        }

        public static void UpdateFileLba(string isoPath, IsoFileEntry entry, uint newLba)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true);

            fs.Position = entry.DirectoryRecordOffset + 2;
            bw.Write(newLba);
            fs.Position = entry.DirectoryRecordOffset + 6;
            WriteUInt32BigEndian(bw, newLba);
            fs.Flush(true);
            entry.Lba = newLba;
        }

        private static void UpdateVolumeSpaceSize(string isoPath)
        {
            long length = new FileInfo(isoPath).Length;
            uint sectors = checked((uint)((length + SectorSize - 1L) / SectorSize));

            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true);
            byte[] sector = new byte[SectorSize];

            for (int index = 16; ; index++)
            {
                long offset = (long)index * SectorSize;
                fs.Position = offset;
                ReadExactly(fs, sector, 0, sector.Length);
                byte type = sector[0];
                if (Encoding.ASCII.GetString(sector, 1, 5) != "CD001")
                    throw new InvalidDataException("Descritor ISO9660 inválido ao atualizar o tamanho do volume.");

                if (type == 1 || type == 2)
                {
                    fs.Position = offset + 80;
                    bw.Write(sectors);
                    WriteUInt32BigEndian(bw, sectors);
                }

                if (type == 255) break;
            }
            fs.Flush(true);
        }

        private static uint ReadUInt32BigEndian(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24) |
                   ((uint)data[offset + 1] << 16) |
                   ((uint)data[offset + 2] << 8) |
                   data[offset + 3];
        }

        public static void UpdateFileSize(string isoPath, IsoFileEntry entry, uint newSize)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true);

            fs.Position = entry.DirectoryRecordOffset + 10;
            bw.Write(newSize);

            fs.Position = entry.DirectoryRecordOffset + 14;
            WriteUInt32BigEndian(bw, newSize);

            fs.Flush(true);
            entry.Size = newSize;
        }

        private static void WriteUInt32BigEndian(BinaryWriter bw, uint value)
        {
            bw.Write((byte)(value >> 24));
            bw.Write((byte)(value >> 16));
            bw.Write((byte)(value >> 8));
            bw.Write((byte)value);
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int read = stream.Read(buffer, offset, count);
                if (read <= 0)
                    throw new EndOfStreamException();
                offset += read;
                count -= read;
            }
        }
    }

    public sealed class BoundedFileStream : Stream
    {
        private readonly FileStream _file;
        private readonly long _baseOffset;
        private readonly long _length;
        private long _position;

        public BoundedFileStream(string path, long baseOffset, long length, FileAccess access, FileShare share)
        {
            _file = new FileStream(path, FileMode.Open, access, share);
            _baseOffset = baseOffset;
            _length = length;

            if (baseOffset < 0 || length < 0 || baseOffset + length > _file.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "A região solicitada ultrapassa o arquivo físico.");
        }

        public override bool CanRead => _file.CanRead;
        public override bool CanSeek => true;
        public override bool CanWrite => _file.CanWrite;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > _length)
                    throw new IOException("Posição fora da região AFS.");
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            count = (int)Math.Min(count, _length - _position);
            _file.Position = _baseOffset + _position;
            int read = _file.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_position + count > _length)
                throw new IOException("A escrita ultrapassaria a área reservada ao AFS dentro da ISO.");

            _file.Position = _baseOffset + _position;
            _file.Write(buffer, offset, count);
            _position += count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long next = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            Position = next;
            return _position;
        }

        public override void Flush() => _file.Flush();
        public void Flush(bool flushToDisk) => _file.Flush(flushToDisk);
        public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) _file.Dispose();
            base.Dispose(disposing);
        }
    }
}
