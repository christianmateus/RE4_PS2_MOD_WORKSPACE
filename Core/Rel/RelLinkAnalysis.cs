using System.Buffers.Binary;
using System.Security.Cryptography;

namespace RE4_PS2_MOD_WORKSPACE.Core.Rel;

public enum RelImportStatus { MainExecutable, LoadedRel, ProviderAvailable, Missing }

public sealed record RelLoadedModule(RelModule Module, uint Base);
public sealed record RelCatalogIssue(string Path, string Message);
public sealed record RelCatalog(IReadOnlyList<RelModule> Modules, IReadOnlyList<RelCatalogIssue> Issues)
{
    public static RelCatalog Scan(string directory, string? excludePath = null)
    {
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var modules = new List<RelModule>();
        var issues = new List<RelCatalogIssue>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? excluded = excludePath is null ? null : Path.GetFullPath(excludePath);
        if (excluded is not null && File.Exists(excluded))
            seen.Add(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(excluded))));
        foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                     .Where(p => p.EndsWith(".rel", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (excluded is not null && Path.GetFullPath(path).Equals(excluded, StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                string digest = Convert.ToHexString(SHA256.HashData(data));
                if (!seen.Add(digest)) continue;
                modules.Add(RelModule.Parse(path, data));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException)
            {
                issues.Add(new RelCatalogIssue(path, ex.Message));
            }
        }
        return new RelCatalog(modules, issues);
    }
}

public sealed record RelImportResult(RelSymbol Symbol, int RelocationUses, RelImportStatus Status,
    uint? Address, string Origin, IReadOnlyList<string> ProviderCandidates);

public sealed record RelocationResult(RelRelocation Relocation, string SymbolName,
    uint OriginalWord, uint? PatchedWord, uint? Target, string Origin, string? Error);

public sealed record RelLinkReport(RelModule Module, uint Base,
    IReadOnlyList<RelImportResult> Imports, IReadOnlyList<RelocationResult> Relocations)
{
    public int PatchedCount => Relocations.Count(r => r.Error is null);
    public bool IsReady => PatchedCount == Relocations.Count;
}

public static class RelLinkAnalysis
{
    public static RelLinkReport Analyze(RelModule module, RelMainSymbols main, uint baseAddress,
        IReadOnlyList<RelLoadedModule>? loaded = null, RelCatalog? catalog = null)
    {
        if ((baseAddress & (module.Header.Alignment - 1)) != 0 ||
            (ulong)baseAddress + (uint)module.Data.Length > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(baseAddress),
                $"Base 0x{baseAddress:X} não comporta o REL ou não respeita alinhamento 0x{module.Header.Alignment:X}.");
        loaded ??= Array.Empty<RelLoadedModule>();
        var uses = module.Relocations.GroupBy(r => r.SymbolIndex)
            .ToDictionary(g => g.Key, g => g.Count());
        var imports = new Dictionary<int, RelImportResult>();
        foreach (RelSymbol symbol in module.Symbols.Where(s => s.IsImport))
        {
            RelMainSymbol? direct = main.Find(symbol.Name, symbol.Metadata);
            RelLoadedModule? provider = null;
            RelSymbol? exported = null;
            if (direct is null)
            {
                foreach (RelLoadedModule entry in loaded)
                {
                    exported = entry.Module.Symbols.FirstOrDefault(s => s.IsExport &&
                        s.Name == symbol.Name && s.Metadata == symbol.Metadata);
                    if (exported is null) continue;
                    provider = entry;
                    break;
                }
            }
            string[] candidates = catalog?.Modules
                .Where(m => m.Symbols.Any(s => s.IsExport && s.Name == symbol.Name &&
                    s.Metadata == symbol.Metadata))
                .Select(m => m.SourcePath).ToArray() ?? Array.Empty<string>();
            RelImportResult result;
            if (direct is not null)
                result = new(symbol, uses.GetValueOrDefault(symbol.Index), RelImportStatus.MainExecutable,
                    direct.Address, Path.GetFileName(main.SourcePath), candidates);
            else if (provider is not null && exported is not null)
            {
                if ((ulong)provider.Base + exported.Address > uint.MaxValue)
                    throw new InvalidDataException($"Export de {provider.Module.SourcePath} ultrapassa 32 bits.");
                result = new(symbol, uses.GetValueOrDefault(symbol.Index), RelImportStatus.LoadedRel,
                    provider.Base + exported.Address, Path.GetFileName(provider.Module.SourcePath), candidates);
            }
            else
                result = new(symbol, uses.GetValueOrDefault(symbol.Index),
                    candidates.Length > 0 ? RelImportStatus.ProviderAvailable : RelImportStatus.Missing,
                    null, candidates.Length > 0 ? "REL não informado como carregado" : "Não encontrado",
                    candidates);
            imports.Add(symbol.Index, result);
        }
        var relocations = new List<RelocationResult>(module.Relocations.Count);
        foreach (RelRelocation rel in module.Relocations)
        {
            RelSymbol symbol = module.Symbols[rel.SymbolIndex];
            string name = symbol.Name.Length > 0 ? symbol.Name : $"<local:{symbol.Index}>";
            uint? address = symbol.IsImport ? imports[symbol.Index].Address : baseAddress + symbol.Address;
            string origin = symbol.IsImport ? imports[symbol.Index].Origin : "REL local";
            uint old = rel.Offset <= module.Data.Length - 4
                ? BinaryPrimitives.ReadUInt32LittleEndian(module.Data.AsSpan((int)rel.Offset, 4)) : 0;
            if (address is null)
            {
                relocations.Add(new(rel, name, old, null, null, origin, "Importação sem endereço; carregue o REL provedor."));
                continue;
            }
            long targetLong = (long)address.Value + rel.Addend;
            if (targetLong is < 0 or > uint.MaxValue || rel.Offset % 4 != 0 || rel.Offset > module.Data.Length - 4)
            {
                relocations.Add(new(rel, name, old, null, null, origin, "Alvo ou offset da realocação inválido."));
                continue;
            }
            uint target = (uint)targetLong;
            uint patched = old;
            string? error = null;
            switch (rel.Type)
            {
                case 2: patched = target; break;
                case 4:
                    if ((old >> 26) is not (2 or 3) || (target & 3) != 0 ||
                        (((ulong)baseAddress + rel.Offset + 4) & 0xF0000000) != (target & 0xF0000000))
                        error = "R_MIPS_26 fora de J/JAL ou da região de 256 MB.";
                    else patched = (old & 0xFC000000) | ((target >> 2) & 0x03FFFFFF);
                    break;
                case 5:
                    if (old >> 26 != 15 || (old & 0xFFFF) != (uint)(((long)rel.Addend + 0x8000) >> 16 & 0xFFFF))
                        error = "R_MIPS_HI16 não corresponde ao addend original.";
                    else patched = (old & 0xFFFF0000) | (uint)(((ulong)target + 0x8000) >> 16 & 0xFFFF);
                    break;
                case 6:
                    if ((old & 0xFFFF) != (uint)(rel.Addend & 0xFFFF))
                        error = "R_MIPS_LO16 não corresponde ao addend original.";
                    else patched = (old & 0xFFFF0000) | (target & 0xFFFF);
                    break;
                default: error = $"Tipo de realocação {rel.Type} não implementado."; break;
            }
            relocations.Add(new(rel, name, old, error is null ? patched : null, target, origin, error));
        }
        return new RelLinkReport(module, baseAddress, imports.Values.OrderBy(x => x.Symbol.Index).ToArray(), relocations);
    }
}
