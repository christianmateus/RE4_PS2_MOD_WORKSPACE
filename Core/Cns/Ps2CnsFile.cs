using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Cns;

public sealed class Ps2CnsFile
{
    public const int HeaderSize = 8;
    public const int KnownValueCount = 12;

    public static readonly string[] KnownNames =
    {
        "Inimigos", "Objetos", "Efeitos", "Geradores de efeitos",
        "Controles", "Luzes", "Partes / bones", "Informações de modelos",
        "Primitivas", "Eventos", "Colisão SAT", "Colisão EAT"
    };

    public static readonly string[] KnownCodes =
    {
        "ENEMY_NUM", "OBJ_NUM", "ESP_NUM", "ESPGEN_NUM", "CTRL_NUM", "LIGHT_NUM",
        "PARTS_NUM", "MODEL_INFO_NUM", "PRIM_NUM", "EVT_NUM", "SAT_NUM", "EAT_NUM"
    };

    public uint Count { get; private set; }
    public uint Flags { get; set; }
    public uint[] Values { get; private set; } = Array.Empty<uint>();
    public int OriginalLength => originalBytes.Length;
    public uint UnknownFlags => Flags & ~0xFFFu;

    private byte[] originalBytes = Array.Empty<byte>();

    public bool IsAvailable(int index) => index >= 0 && index < Values.Length;
    public bool IsEnabled(int index) => index is >= 0 and < 32 && (Flags & (1u << index)) != 0;

    public void SetEnabled(int index, bool enabled)
    {
        if (index is < 0 or >= 32) throw new ArgumentOutOfRangeException(nameof(index));
        uint bit = 1u << index;
        Flags = enabled ? Flags | bit : Flags & ~bit;
    }

    public static Ps2CnsFile Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length < HeaderSize) throw new InvalidDataException("O arquivo CNS é menor que o cabeçalho de 8 bytes.");
        if ((bytes.Length & 0x1F) != 0) throw new InvalidDataException("O tamanho do CNS não está alinhado em blocos de 0x20 bytes.");

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4));
        if (count > 32) throw new InvalidDataException($"CNS com count não suportado: {count}. O limite seguro é 32.");
        long declaredEnd = HeaderSize + count * 4L;
        if (declaredEnd > bytes.Length) throw new InvalidDataException("O count do CNS aponta para além do fim do arquivo.");

        var values = new uint[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(HeaderSize + i * 4, 4));

        return new Ps2CnsFile
        {
            Count = count,
            Flags = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4)),
            Values = values,
            originalBytes = bytes
        };
    }

    public void Save(string path)
    {
        if (Values.Length != Count) throw new InvalidOperationException("A quantidade de valores CNS foi alterada.");
        if (originalBytes.Length < HeaderSize + Values.Length * 4) throw new InvalidOperationException("O buffer original do CNS é inválido.");

        byte[] output = (byte[])originalBytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(0, 4), Count);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(4, 4), Flags);
        for (int i = 0; i < Values.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(HeaderSize + i * 4, 4), Values[i]);

        string backup = path + ".bak";
        if (!File.Exists(backup)) File.Copy(path, backup, overwrite: false);
        File.WriteAllBytes(path, output);

        Ps2CnsFile check = Read(path);
        if (check.Count != Count || check.Flags != Flags || !check.Values.SequenceEqual(Values) || check.OriginalLength != output.Length)
            throw new InvalidDataException("A validação do CNS falhou depois da gravação.");
        originalBytes = output;
    }
}
