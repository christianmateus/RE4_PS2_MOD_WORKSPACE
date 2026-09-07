using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE.Core.Messages;

public sealed class MdtDocument
{
    public string SourcePath { get; private set; } = "";
    public long OriginalSize { get; private set; }
    public bool IsMultiLanguage { get; private set; }
    public List<MdtLanguage> Languages { get; } = new();

    public static MdtDocument Load(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 8) throw new InvalidDataException("MDT muito pequeno.");
        var document = new MdtDocument { SourcePath = path, OriginalSize = data.Length };
        uint marker = BitConverter.ToUInt32(data, 0);
        if (marker == 6)
        {
            document.IsMultiLanguage = true;
            if (data.Length < 28) throw new InvalidDataException("Cabeçalho MDT MULTI incompleto.");
            uint[] offsets = Enumerable.Range(0, 6).Select(i => ReadU32(data, 4 + i * 4)).ToArray();
            for (int i = 0; i < offsets.Length; i++)
            {
                int start = CheckedOffset(offsets[i], data.Length, "idioma");
                int end = i + 1 < offsets.Length ? CheckedOffset(offsets[i + 1], data.Length, "idioma") : data.Length;
                if (end < start) throw new InvalidDataException("Offsets de idiomas fora de ordem.");
                document.Languages.Add(ReadLanguage(data, start, end, i));
            }
        }
        else document.Languages.Add(ReadLanguage(data, 0, data.Length, 1));
        return document;
    }

    public void Save(string path)
    {
        byte[] result = IsMultiLanguage ? BuildMulti() : BuildLanguage(Languages.Single());
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Destino MDT inválido.");
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporary, result);
            File.Move(temporary, path, true);
            SourcePath = path;
            OriginalSize = result.Length;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public int CalculateSize() => IsMultiLanguage ? BuildMulti().Length : BuildLanguage(Languages.Single()).Length;

    private byte[] BuildMulti()
    {
        if (Languages.Count != 6) throw new InvalidOperationException("MDT MULTI PS2 deve possuir seis idiomas.");
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(6u);
        for (int i = 0; i < 6; i++) writer.Write(0u);
        var offsets = new uint[6];
        for (int i = 0; i < Languages.Count; i++)
        {
            offsets[i] = checked((uint)stream.Position);
            writer.Write(BuildLanguage(Languages[i]));
            if (i + 1 < Languages.Count) PadToMultiple(writer, 32, offsets[i]);
        }
        PadToMultiple(writer, 32, 0);
        long end = stream.Position;
        stream.Position = 4;
        foreach (uint offset in offsets) writer.Write(offset);
        stream.Position = end;
        return stream.ToArray();
    }

    private static byte[] BuildLanguage(MdtLanguage language)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(language.Identifier);
        writer.Write(language.Messages.Count);
        long table = stream.Position;
        for (int i = 0; i < language.Messages.Count; i++) writer.Write(0u);
        var offsets = new uint[language.Messages.Count];
        for (int i = 0; i < language.Messages.Count; i++)
        {
            offsets[i] = checked((uint)stream.Position);
            foreach (ushort unit in MdtCodec.Encode(language.Messages[i].Text)) writer.Write(unit);
        }
        writer.Write(new byte[16]);
        long end = stream.Position;
        stream.Position = table;
        foreach (uint offset in offsets) writer.Write(offset);
        stream.Position = end;
        return stream.ToArray();
    }

    private static MdtLanguage ReadLanguage(byte[] data, int start, int end, int languageIndex)
    {
        if (end - start < 8) throw new InvalidDataException("Bloco de idioma MDT incompleto.");
        uint identifier = ReadU32(data, start);
        int count = checked((int)ReadU32(data, start + 4));
        if (count < 0 || count > 100000 || 8L + count * 4L > end - start) throw new InvalidDataException("Quantidade de mensagens MDT inválida.");
        uint[] offsets = Enumerable.Range(0, count).Select(i => ReadU32(data, start + 8 + i * 4)).ToArray();
        var language = new MdtLanguage(languageIndex, identifier);
        for (int i = 0; i < count; i++)
        {
            int messageStart = start + CheckedRelative(offsets[i], end - start);
            int messageEnd = i + 1 < count ? start + CheckedRelative(offsets[i + 1], end - start) : FindLastMessageEnd(data, messageStart, end);
            if (messageEnd < messageStart || ((messageEnd - messageStart) & 1) != 0) throw new InvalidDataException($"Ponteiro da mensagem {i + 1} inválido.");
            ushort[] units = new ushort[(messageEnd - messageStart) / 2];
            for (int j = 0; j < units.Length; j++) units[j] = BitConverter.ToUInt16(data, messageStart + j * 2);
            language.Messages.Add(new MdtMessage(i, MdtCodec.Decode(units)));
        }
        return language;
    }

    private static int FindLastMessageEnd(byte[] data, int start, int end)
    {
        int cursor = end - ((end - start) & 1);
        while (cursor - 2 >= start && BitConverter.ToUInt16(data, cursor - 2) == 0) cursor -= 2;
        return Math.Max(start, cursor);
    }

    private static void PadToMultiple(BinaryWriter writer, int alignment, long origin)
    {
        long size = writer.BaseStream.Position - origin;
        int padding = (int)((alignment - size % alignment) % alignment);
        if (padding > 0) writer.Write(new byte[padding]);
    }

    private static uint ReadU32(byte[] data, int offset) => offset >= 0 && offset + 4 <= data.Length ? BitConverter.ToUInt32(data, offset) : throw new InvalidDataException("Leitura fora do MDT.");
    private static int CheckedOffset(uint value, int length, string field) => value <= length ? checked((int)value) : throw new InvalidDataException($"Offset de {field} fora do arquivo.");
    private static int CheckedRelative(uint value, int length) => value <= length ? checked((int)value) : throw new InvalidDataException("Ponteiro de mensagem fora do bloco.");
}

public sealed class MdtLanguage
{
    private static readonly string[] Names = { "Japonês", "Inglês", "Francês", "Alemão", "Italiano", "Espanhol" };
    public int Index { get; }
    public uint Identifier { get; }
    public string Name => Index >= 0 && Index < Names.Length ? Names[Index] : "Idioma único";
    public List<MdtMessage> Messages { get; } = new();
    public MdtLanguage(int index, uint identifier) { Index = index; Identifier = identifier; }
    public override string ToString() => $"{Name}  •  {Messages.Count:N0} mensagens";
}

public sealed class MdtMessage
{
    public int Index { get; }
    public string Text { get; set; }
    public MdtMessage(int index, string text) { Index = index; Text = text; }
    public override string ToString()
    {
        string preview = MdtCodec.PlainPreview(Text);
        return $"{Index + 1:D3}   {(string.IsNullOrWhiteSpace(preview) ? "(mensagem sem texto)" : preview)}";
    }
}

public static partial class MdtCodec
{
    private static readonly string[] CommandNames = { "message-start", "message-end", "message-change", "line-break", "new-page", "print-speed", "color", "option", "pause", "sleep", "item-quantity", "align-left", "align-top", "unknown-13", "return", "unknown-15", "file-name", "item-id", "character", "unknown-19" };
    private static readonly bool[] HasArgument = { false, false, true, false, false, true, true, false, false, true, false, true, true, true, false, true, false, true, true, false };
    private static readonly Dictionary<ushort, string> DecodeChars = BuildCharacters();
    private static readonly Dictionary<string, ushort> EncodeChars = DecodeChars.GroupBy(x => x.Value, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First().Key, StringComparer.Ordinal);
    private static readonly Dictionary<string, ushort> CommandCodes = CommandNames.Select((name, index) => (name, code: (ushort)index)).ToDictionary(x => x.name, x => x.code, StringComparer.OrdinalIgnoreCase);

    public static string Decode(IReadOnlyList<ushort> units)
    {
        var result = new StringBuilder();
        for (int i = 0; i < units.Count; i++)
        {
            ushort value = units[i];
            if (value < CommandNames.Length)
            {
                result.Append('[').Append(CommandNames[value]);
                if (HasArgument[value] && i + 1 < units.Count) result.Append(':').Append(units[++i].ToString(CultureInfo.InvariantCulture));
                result.Append(']');
            }
            else if (DecodeChars.TryGetValue(value, out string? character))
            {
                // Literal brackets would be ambiguous with the editable command syntax.
                if (character is "[" or "]") result.Append("[0x").Append(value.ToString("X4")).Append(']');
                else result.Append(character);
            }
            else result.Append("[0x").Append(value.ToString("X4")).Append(']');
        }
        return result.ToString();
    }

    public static ushort[] Encode(string text)
    {
        var result = new List<ushort>();
        foreach (Match match in TokenRegex().Matches(text ?? ""))
        {
            string token = match.Value;
            if (token[0] == '{' && EncodeChars.TryGetValue(token, out ushort symbol)) { result.Add(symbol); continue; }
            if (token[0] != '[')
            {
                foreach (Rune rune in token.EnumerateRunes())
                {
                    string character = rune.ToString();
                    if (!EncodeChars.TryGetValue(character, out ushort value)) throw new InvalidDataException($"Caractere não suportado pelo MDT: {character}");
                    result.Add(value);
                }
                continue;
            }
            string body = token[1..^1];
            if (body.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && ushort.TryParse(body[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort raw)) { result.Add(raw); continue; }
            string[] parts = body.Split(':', 2);
            if (!CommandCodes.TryGetValue(parts[0], out ushort command)) throw new InvalidDataException($"Comando MDT desconhecido: {token}");
            result.Add(command);
            if (HasArgument[command])
            {
                if (parts.Length != 2 || !ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort argument)) throw new InvalidDataException($"O comando {parts[0]} precisa de um argumento entre 0 e 65535.");
                result.Add(argument);
            }
            else if (parts.Length != 1) throw new InvalidDataException($"O comando {parts[0]} não aceita argumento.");
        }
        return result.ToArray();
    }

    public static string PlainPreview(string text)
    {
        string value = TokenRegex().Replace(text ?? "", match =>
        {
            string token = match.Value;
            if (!token.StartsWith('[')) return token;
            string body = token[1..^1];
            return body.Equals("line-break", StringComparison.OrdinalIgnoreCase) || body.Equals("new-page", StringComparison.OrdinalIgnoreCase) ? " / " : "";
        });
        value = Regex.Replace(value, "\\s+", " ").Trim();
        return value.Length > 72 ? value[..69] + "…" : value;
    }

    public static string ToFriendly(string raw)
    {
        return TokenRegex().Replace(raw ?? "", match =>
        {
            string token = match.Value;
            if (!token.StartsWith('[')) return token;
            string body = token[1..^1];
            return body.ToLowerInvariant() switch
            {
                "message-start" => "〈INÍCIO〉",
                "message-end" => "〈FIM〉",
                "line-break" => Environment.NewLine,
                "new-page" => Environment.NewLine + "──── NOVA PÁGINA ────" + Environment.NewLine,
                "pause" => "〈PAUSA〉",
                "option" => "〈OPÇÃO〉",
                _ => "〈" + body + "〉"
            };
        });
    }

    public static string FromFriendly(string friendly)
    {
        string value = (friendly ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        value = value.Replace("\n──── NOVA PÁGINA ────\n", "[new-page]", StringComparison.Ordinal)
                     .Replace("──── NOVA PÁGINA ────", "[new-page]", StringComparison.Ordinal);
        value = FriendlyTagRegex().Replace(value, match =>
        {
            string body = match.Groups[1].Value;
            return body.ToUpperInvariant() switch
            {
                "INÍCIO" => "[message-start]",
                "FIM" => "[message-end]",
                "PAUSA" => "[pause]",
                "OPÇÃO" => "[option]",
                _ => "[" + body + "]"
            };
        });
        return value.Replace("\n", "[line-break]", StringComparison.Ordinal);
    }

    private static Dictionary<ushort, string> BuildCharacters()
    {
        var map = new Dictionary<ushort, string>();
        string basic = " ►▼0123456789:%&+-/=,.˙…()!?“”~☼▬<>[]①②ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        for (int i = 0; i < basic.Length; i++) map[(ushort)(0x80 + i)] = basic[i].ToString();
        string accents = "âêîôûÂÊÎÔÛàèìòùÀÈÌÒÙáéíóúýÁÉÍÓÚÝäëïöüÿÄËÏÖÜŸãõÃÕñÑåÅçÇøØÞþšŠßÐƒµðæœÆŒ";
        for (int i = 0; i < accents.Length; i++) map[(ushort)(0xDB + i)] = accents[i].ToString();
        string symbols = "°¡¿'™;#@→←↑↓";
        for (int i = 0; i < symbols.Length; i++) map[(ushort)(0x120 + i)] = symbols[i].ToString();
        map[0x12C] = "{ptas}"; map[0x12D] = "\""; map[0x12E] = "„"; map[0x12F] = "®";
        map[0x130] = "{X}"; map[0x131] = "{SQR}"; map[0x132] = "{O}"; map[0x133] = "{TRI}"; map[0x134] = "*"; map[0x135] = "x"; map[0x136] = "{R1}"; map[0x137] = "{R2}"; map[0x138] = "{L1}";
        return map;
    }

    [GeneratedRegex(@"\[[^\]\r\n]+\]|\{[^\}\r\n]+\}|[^\[\{]+|[\[\{]")]
    private static partial Regex TokenRegex();
    [GeneratedRegex("〈([^〉]+)〉")]
    private static partial Regex FriendlyTagRegex();
}
