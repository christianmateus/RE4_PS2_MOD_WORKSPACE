using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace RE4_PS2_MOD_WORKSPACE.Core.Executable;

public enum SlusCheatState { Ready, Applied, Incompatible, Unavailable }
public sealed record SlusCheatStatus(int Id, string Name, string Type, SlusCheatState State, string Detail);
public sealed record SlusCheatInspection(IReadOnlyList<SlusCheatStatus> Options, string Detail);

/// <summary>Fixed-size, reversible SLUS patches, including a file-backed auxiliary ELF segment.</summary>
public static class SlusCheatPatches
{
    private const int RuntimeOffset = 0x2CB000;
    private const int ManifestOffset = 0x2CF000;
    private const int ReservedEnd = 0x2D3000;
    private const int SecondHeader = 84;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("RE4CGH01");
    private static readonly Catalogue Data = LoadCatalogue();
    private static readonly string[] PortugueseNames =
    {
        "Invencibilidade do personagem principal", "Invencibilidade da Ashley", "Munição e granadas infinitas",
        "PTAS infinitas", "Desbloquear todos os extras", "Desbloquear tudo na loja", "Braço do Krauser sempre disponível",
        "Maleta XL para todos os personagens", "Colete tático no modo Professional", "Vida máxima (somente jogo novo)",
        "Limite de munição por caixa: 999", "Handgun sempre com silenciador", "Punisher sempre com silenciador",
        "Blacktail sempre com silenciador", "Killer7 sempre com silenciador", "Red9 sempre com coronha",
        "Rifle sempre com mira", "Rifle semiautomático sempre com mira", "Mine Thrower sempre com mira",
        "TMP com silenciador e coronha", "TMP com coronha", "TMP com silenciador", "Carga rápida da P.R.L. 412",
        "Abrir a loja em qualquer lugar (L2 + Triângulo)", "Remover tremor ao mirar", "Facilitar QTEs de dois botões",
        "Pular logos iniciais", "Cor do laser: vermelho", "Cor do ponto do laser: vermelho claro",
        "Preservar contagem de mortes no Mercenaries", "Temporizadores infinitos (L2 + X)", "Inverter correr / andar",
        "Chute circular (L2 + Quadrado)", "Mira automática", "Balas atravessam inimigos",
        "Minas explodem ao tocar inimigos", "Modo de teste de TV em 480p", "Passos normais na armadura da Ashley",
        "Chicago Typewriter com upgrades", "Inimigos morrem com um golpe", "Inimigos sempre deixam itens",
        "Aumentar quantidade de munição deixada", "Selecionar traje no Mercenaries", "Contagem de saves sempre zero",
        "Alternar velocidade 2× (R3)", "Pular a maioria das cenas de vídeo", "Pular chamadas de rádio",
        "Abrir portas sem Ashley por perto", "Obter todas as tampinhas"
    };

    public static SlusCheatInspection Inspect(Ps2ExecutableDocument document)
    {
        byte[] bytes = document.ReadBytes(0, document.Length);
        try
        {
            RequireSlus(document, bytes);
            Installation? installed = ReadInstallation(bytes);
            if (installed != null) ValidateInstalled(bytes, installed);
            byte[] clean = Restore(bytes, installed);
            ValidateOriginalLayout(clean);
            ValidateFreeArea(clean);
            var statuses = Data.Options.Select(option =>
            {
                if (option.Kind == "runtime_continuo")
                    return Status(option, SlusCheatState.Unavailable, "O acionamento do runtime desta opção ainda não foi mapeado.");
                if (installed?.Ids.Contains(option.Id) == true)
                    return Status(option, SlusCheatState.Applied, "Instalada pelo editor; pode ser removida.");
                string? mismatch = FindMismatch(clean, option.Game);
                return Status(option, mismatch == null ? SlusCheatState.Ready : SlusCheatState.Incompatible,
                    mismatch ?? (option.Kind == "patch_direto" ? "Compatível com os bytes originais." : "Inclui rotina auxiliar carregada pelo ELF."));
            }).ToArray();
            return new(statuses, "Selecione as opções e aplique. Desmarque para remover; grave com SALVAR ALTERAÇÕES.");
        }
        catch (InvalidDataException ex)
        {
            return new(Data.Options.Select(o => Status(o, SlusCheatState.Incompatible, ex.Message)).ToArray(), ex.Message);
        }
    }

    /// <summary>Build and verify the entire selection before replacing any document bytes.</summary>
    public static void SetSelection(Ps2ExecutableDocument document, IEnumerable<int> selectedIds)
    {
        int[] ids = selectedIds.Distinct().Order().ToArray();
        var selected = ids.Select(id => Data.Options.SingleOrDefault(o => o.Id == id)
            ?? throw new InvalidDataException($"Trapaça desconhecida: {id}.")).ToArray();
        if (selected.Any(o => o.Kind == "runtime_continuo"))
            throw new InvalidDataException("Esta seleção inclui uma opção cujo runtime ainda não foi mapeado.");
        if (ids.Count(id => id is 19 or 20 or 21) > 1)
            throw new InvalidDataException("Escolha somente uma configuração de acessórios da TMP.");

        byte[] source = document.ReadBytes(0, document.Length);
        RequireSlus(document, source);
        Installation? previous = ReadInstallation(source);
        if (previous != null) ValidateInstalled(source, previous);
        byte[] result = Restore(source, previous);
        ValidateOriginalLayout(result);
        if (ids.Length == 0)
        {
            document.WriteBytes(0, result);
            return;
        }
        ValidateFreeArea(result);

        Dictionary<uint, GameWord> patches = ComposeGame(selected);
        string? mismatch = FindMismatch(result, patches.Values);
        if (mismatch != null) throw new InvalidDataException(mismatch);
        var original = patches.Values.OrderBy(w => w.Address)
            .Select(w => new OriginalWord { Address = w.Address, Value = Read32(result, Map(result, w.Address)) }).ToArray();
        foreach (GameWord patch in patches.Values) Write32(result, Map(result, patch.Address), patch.Value);
        bool auxiliary = selected.Any(o => o.Kind == "hook_com_auxiliar");
        if (auxiliary)
        {
            BuildRuntime(selected).CopyTo(result, RuntimeOffset);
            BuildProgramHeader().CopyTo(result, SecondHeader);
            Write16(result, 44, 2);
        }
        var installation = new Installation { Version = 1, Ids = ids, Original = original, Auxiliary = auxiliary };
        byte[] manifest = JsonSerializer.SerializeToUtf8Bytes(installation);
        if (manifest.Length > ReservedEnd - ManifestOffset - 12)
            throw new InvalidDataException("O registro de restauração ultrapassa a área reservada.");
        Magic.CopyTo(result, ManifestOffset);
        Write32(result, ManifestOffset + 8, (uint)manifest.Length);
        manifest.CopyTo(result, ManifestOffset + 12);
        ValidateInstalled(result, installation);
        document.WriteBytes(0, result);
    }

    public static void Validate(Ps2ExecutableDocument document)
    {
        byte[] bytes = document.ReadBytes(0, document.Length);
        Installation? installation = ReadInstallation(bytes);
        if (installation != null) ValidateInstalled(bytes, installation);
    }

    private static SlusCheatStatus Status(Option option, SlusCheatState state, string detail) =>
        new(option.Id, PortugueseNames[option.Id], option.Kind == "runtime_continuo" ? "Runtime pendente" :
            option.Kind == "hook_com_auxiliar" ? "Com rotina auxiliar" : "Patch direto", state, detail);

    private static void RequireSlus(Ps2ExecutableDocument document, byte[] bytes)
    {
        if (document.Region != ExecutableRegion.Slus)
            throw new InvalidDataException("Estas trapaças foram mapeadas para SLUS-21134. SLES e SLPS ainda não são compatíveis.");
        if (bytes.Length < ReservedEnd || bytes[4] != 1 || bytes[5] != 1 || Read16(bytes, 18) != 8)
            throw new InvalidDataException("O executável não corresponde ao layout SLUS suportado.");
    }

    private static void ValidateOriginalLayout(byte[] bytes)
    {
        if (Read32(bytes, 24) != 0x100008 || Read32(bytes, 28) != 52 || Read16(bytes, 42) != 32 || Read16(bytes, 44) != 1 ||
            Read32(bytes, 52) != 1 || Read32(bytes, 56) != 0x1000 || Read32(bytes, 60) != 0x100000 ||
            Read32(bytes, 64) != 0x100000 || Read32(bytes, 68) != 2924328 || Read32(bytes, 72) != 3803736)
            throw new InvalidDataException("O layout deste ELF não é compatível. O SLUS compactado do mod menu não pode ser editado por este instalador.");
    }

    private static Installation? ReadInstallation(byte[] bytes)
    {
        if (bytes.Length < ReservedEnd || !bytes.AsSpan(ManifestOffset, 8).SequenceEqual(Magic)) return null;
        uint length = Read32(bytes, ManifestOffset + 8);
        if (length == 0 || length > ReservedEnd - ManifestOffset - 12)
            throw new InvalidDataException("O registro das trapaças está danificado.");
        try
        {
            Installation? installation = JsonSerializer.Deserialize<Installation>(bytes.AsSpan(ManifestOffset + 12, (int)length));
            if (installation == null || installation.Version != 1 || installation.Ids == null || installation.Original == null || installation.Ids.Length == 0 ||
                installation.Ids.Distinct().Count() != installation.Ids.Length || installation.Ids.Any(id => !Data.Options.Any(o => o.Id == id && o.Kind != "runtime_continuo")) ||
                installation.Ids.Count(id => id is 19 or 20 or 21) > 1)
                throw new InvalidDataException("O registro das trapaças não é reconhecido.");
            return installation;
        }
        catch (JsonException ex) { throw new InvalidDataException("O registro das trapaças está danificado.", ex); }
    }

    private static void ValidateFreeArea(byte[] bytes)
    {
        if (bytes.AsSpan(RuntimeOffset, ReservedEnd - RuntimeOffset).IndexOfAnyExcept((byte)0) >= 0)
            throw new InvalidDataException("A área reservada às rotinas já contém dados de outra modificação.");
        if (bytes.AsSpan(SecondHeader, 32).IndexOfAnyExcept((byte)0) >= 0)
            throw new InvalidDataException("O espaço para o segundo segmento ELF está ocupado.");
    }

    private static void ValidateInstalled(byte[] bytes, Installation installation)
    {
        Option[] selected = installation.Ids.Select(id => Data.Options.Single(o => o.Id == id)).ToArray();
        var expected = ComposeGame(selected);
        if (installation.Original.Length != expected.Count || installation.Original.Select(w => w.Address).Distinct().Count() != expected.Count ||
            installation.Original.Any(w => !expected.TryGetValue(w.Address, out GameWord? patch) || patch.Original != w.Value))
            throw new InvalidDataException("O registro de restauração não corresponde às trapaças instaladas.");
        foreach (GameWord word in expected.Values)
            if (Read32(bytes, Map(bytes, word.Address)) != word.Value)
                throw new InvalidDataException($"Uma trapaça instalada foi alterada em 0x{word.Address:X8}. Desfaça a edição conflitante antes de remover ou salvar.");
        bool auxiliary = selected.Any(o => o.Kind == "hook_com_auxiliar");
        if (installation.Auxiliary != auxiliary || Read16(bytes, 44) != (auxiliary ? 2 : 1))
            throw new InvalidDataException("Os segmentos ELF da instalação foram alterados.");
        if (!bytes.AsSpan(SecondHeader, 32).SequenceEqual(auxiliary ? BuildProgramHeader() : new byte[32]))
            throw new InvalidDataException("O segmento das rotinas auxiliares foi alterado.");
        byte[] runtimeArea = new byte[ManifestOffset - RuntimeOffset];
        if (auxiliary) BuildRuntime(selected).CopyTo(runtimeArea, 0);
        if (!bytes.AsSpan(RuntimeOffset, runtimeArea.Length).SequenceEqual(runtimeArea))
            throw new InvalidDataException("Uma rotina auxiliar instalada foi alterada por outra modificação.");
        int manifestLength = checked((int)Read32(bytes, ManifestOffset + 8));
        int tail = ManifestOffset + 12 + manifestLength;
        if (bytes.AsSpan(tail, ReservedEnd - tail).IndexOfAnyExcept((byte)0) >= 0)
            throw new InvalidDataException("A área de restauração contém dados externos à instalação.");
        byte[] clean = Restore(bytes, installation);
        ValidateOriginalLayout(clean);
    }

    private static byte[] Restore(byte[] bytes, Installation? installation)
    {
        byte[] result = (byte[])bytes.Clone();
        if (installation == null) return result;
        foreach (OriginalWord word in installation.Original) Write32(result, Map(result, word.Address), word.Value);
        result.AsSpan(RuntimeOffset, ReservedEnd - RuntimeOffset).Clear();
        result.AsSpan(SecondHeader, 32).Clear();
        Write16(result, 44, 1);
        return result;
    }

    private static Dictionary<uint, GameWord> ComposeGame(IEnumerable<Option> selected)
    {
        var result = new Dictionary<uint, GameWord>();
        foreach (GameWord word in selected.SelectMany(o => o.Game))
        {
            if (result.TryGetValue(word.Address, out GameWord? previous) && (previous.Value != word.Value || previous.Original != word.Original))
                throw new InvalidDataException($"As opções selecionadas conflitam em 0x{word.Address:X8}.");
            result[word.Address] = word;
        }
        return result;
    }

    private static byte[] BuildRuntime(IEnumerable<Option> options)
    {
        Option[] selected = options.ToArray();
        var common = Data.Common.ToDictionary(w => w.Address, w => w.Value);
        var custom = new Dictionary<uint, uint>();
        foreach (Word word in selected.SelectMany(o => o.Auxiliary))
        {
            if (custom.TryGetValue(word.Address, out uint value) && value != word.Value)
                throw new InvalidDataException($"As rotinas selecionadas conflitam em 0x{word.Address:X8}.");
            custom[word.Address] = word.Value;
        }
        foreach (var word in custom) common[word.Key] = word.Value;
        common[0xF0000] = selected.Where(o => o.Id < 32).Aggregate(0u, (mask, o) => mask | (1u << o.Id));
        common[0xF0004] = selected.Where(o => o.Id >= 32).Aggregate(0u, (mask, o) => mask | (1u << (o.Id - 32)));
        byte[] runtime = new byte[Data.RuntimeSize];
        foreach (var word in common)
        {
            if (word.Key < Data.RuntimeBase || word.Key + 4 > Data.RuntimeBase + runtime.Length)
                throw new InvalidDataException("Endereço de rotina auxiliar inválido.");
            Write32(runtime, checked((int)(word.Key - Data.RuntimeBase)), word.Value);
        }
        return runtime;
    }

    private static byte[] BuildProgramHeader()
    {
        uint[] fields = { 1, RuntimeOffset, Data.RuntimeBase, Data.RuntimeBase, (uint)Data.RuntimeSize, (uint)Data.RuntimeSize, 7, 16 };
        byte[] result = new byte[32];
        for (int i = 0; i < fields.Length; i++) Write32(result, i * 4, fields[i]);
        return result;
    }

    private static string? FindMismatch(byte[] bytes, IEnumerable<GameWord> words)
    {
        foreach (GameWord word in words)
        {
            int offset = Map(bytes, word.Address);
            if (Read32(bytes, offset) != word.Original)
                return $"Bytes incompatíveis em 0x{word.Address:X8}; há outra modificação nesta opção.";
        }
        return null;
    }

    private static int Map(byte[] bytes, uint address)
    {
        if (Read32(bytes, 28) != 52 || Read16(bytes, 42) != 32) throw new InvalidDataException("Tabela de segmentos inválida.");
        uint start = Read32(bytes, 60), count = Read32(bytes, 68), offset = Read32(bytes, 56);
        if (address < start || (ulong)address + 4 > (ulong)start + count || (ulong)offset + address - start + 4 > (ulong)bytes.Length)
            throw new InvalidDataException($"O endereço 0x{address:X8} não está no segmento original do jogo.");
        return checked((int)(offset + address - start));
    }

    private static uint Read32(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
    private static ushort Read16(byte[] bytes, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
    private static void Write32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
    private static void Write16(byte[] bytes, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), value);

    private static Catalogue LoadCatalogue()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SlusCheats.json")
            ?? throw new InvalidOperationException("O catálogo de trapaças não foi incluído no aplicativo.");
        return JsonSerializer.Deserialize<Catalogue>(stream) ?? throw new InvalidOperationException("Catálogo inválido.");
    }

    private sealed class Catalogue
    {
        public uint RuntimeBase { get; set; }
        public int RuntimeSize { get; set; }
        public Word[] Common { get; set; } = Array.Empty<Word>();
        public Option[] Options { get; set; } = Array.Empty<Option>();
    }
    private sealed class Option
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public GameWord[] Game { get; set; } = Array.Empty<GameWord>();
        public Word[] Auxiliary { get; set; } = Array.Empty<Word>();
    }
    private class Word { public uint Address { get; set; } public uint Value { get; set; } }
    private sealed class GameWord : Word { public uint Original { get; set; } }
    private sealed class OriginalWord : Word { }
    private sealed class Installation
    {
        public int Version { get; set; }
        public int[] Ids { get; set; } = Array.Empty<int>();
        public OriginalWord[] Original { get; set; } = Array.Empty<OriginalWord>();
        public bool Auxiliary { get; set; }
    }
}
