namespace RE4_PS2_MOD_WORKSPACE.Core.Executable;

public enum DebugSkipIntroLogosPatchState { Ready, Applied, Incompatible }

/// <summary>
/// Activates the game's own cancel/fade path as soon as each startup screen
/// begins. This keeps resource cleanup intact before the title menu is shown.
/// </summary>
public static class DebugSkipIntroLogosPatch
{
    private sealed record Site(int Offset, byte[] Original, byte[] Patched);

    private static readonly Site[] CancelSites =
    {
        new(0x1B2588, new byte[] { 0x27, 0x00, 0x40, 0x10 }, new byte[4]),
        new(0x1B26E4, new byte[] { 0x94, 0x00, 0x40, 0x10 }, new byte[4]),
        new(0x1B27AC, new byte[] { 0x62, 0x00, 0x40, 0x10 }, new byte[4]),
        new(0x1B2874, new byte[] { 0x30, 0x00, 0x40, 0x10 }, new byte[4]),
        new(0x26C0D8, new byte[] { 0x2D, 0x00, 0x00, 0x00 }, new byte[4]),
        new(0x26C0DC, new byte[] { 0x87, 0x00, 0x00, 0x00 }, new byte[4]),
        new(0x26C0E0, new byte[] { 0xF5, 0x00, 0x00, 0x00 }, new byte[4]),
        new(0x26C0E4, new byte[] { 0x59, 0x01, 0x00, 0x00 }, new byte[4])
    };
    private static readonly Site[] FadeSites =
    {
        new(0x1B25C4, new byte[] { 0x0F, 0x00, 0x07, 0x24 }, new byte[] { 0x01, 0x00, 0x07, 0x24 }),
        new(0x1B2720, new byte[] { 0x0F, 0x00, 0x07, 0x24 }, new byte[] { 0x01, 0x00, 0x07, 0x24 }),
        new(0x1B27E8, new byte[] { 0x0F, 0x00, 0x07, 0x24 }, new byte[] { 0x01, 0x00, 0x07, 0x24 }),
        new(0x1B28B0, new byte[] { 0x0F, 0x00, 0x07, 0x24 }, new byte[] { 0x01, 0x00, 0x07, 0x24 })
    };

    // Bytes used by the two experimental versions, retained only so the editor
    // can migrate an ISO already patched by an earlier build.
    private static readonly Site LegacyTransition = new(0x1B2280,
        new byte[] { 0x03, 0x00, 0x02, 0x24 }, new byte[] { 0x05, 0x00, 0x02, 0x24 });
    private static readonly Site[] LegacyTimers =
    {
        new(0x1B254C, new byte[] { 0x6A, 0x00, 0x82, 0x28 }, new byte[] { 0x01, 0x00, 0x82, 0x28 }),
        new(0x1B26AC, new byte[] { 0xE7, 0x00, 0x82, 0x28 }, new byte[] { 0x01, 0x00, 0x82, 0x28 }),
        new(0x1B2774, new byte[] { 0x4B, 0x01, 0x82, 0x28 }, new byte[] { 0x01, 0x00, 0x82, 0x28 }),
        new(0x1B283C, new byte[] { 0x4A, 0x02, 0x82, 0x28 }, new byte[] { 0x01, 0x00, 0x82, 0x28 }),
        new(0x1B2920, new byte[] { 0x68, 0x02, 0x42, 0x28 }, new byte[] { 0x01, 0x00, 0x42, 0x28 })
    };

    public static DebugSkipIntroLogosPatchState GetState(Ps2ExecutableDocument document)
    {
        if (document.Region != ExecutableRegion.DebugSlps) return DebugSkipIntroLogosPatchState.Incompatible;
        bool transitionOriginal = Matches(document, LegacyTransition, false);
        bool transitionLegacy = Matches(document, LegacyTransition, true);
        bool timersOriginal = LegacyTimers.All(x => Matches(document, x, false));
        bool timersLegacy = LegacyTimers.All(x => Matches(document, x, true));
        bool cancelOriginal = CancelSites.All(x => Matches(document, x, false));
        bool cancelPatched = CancelSites.All(x => Matches(document, x, true));
        bool fadeOriginal = FadeSites.All(x => Matches(document, x, false));
        bool fadePatched = FadeSites.All(x => Matches(document, x, true));

        if (transitionOriginal && timersOriginal && cancelOriginal && fadeOriginal) return DebugSkipIntroLogosPatchState.Ready;
        if (transitionOriginal && timersOriginal && cancelPatched && (fadeOriginal || fadePatched)) return DebugSkipIntroLogosPatchState.Applied;
        if (cancelOriginal && fadeOriginal && ((transitionLegacy && timersOriginal) || (transitionOriginal && timersLegacy)))
            return DebugSkipIntroLogosPatchState.Applied;
        return DebugSkipIntroLogosPatchState.Incompatible;
    }

    public static void Apply(Ps2ExecutableDocument document)
    {
        if (GetState(document) == DebugSkipIntroLogosPatchState.Incompatible)
            throw new InvalidDataException("O SLPS debug não corresponde aos bytes esperados para pular os logos.");
        Write(document, LegacyTransition, false);
        foreach (var timer in LegacyTimers) Write(document, timer, false);
        foreach (var site in CancelSites) Write(document, site, true);
        foreach (var site in FadeSites) Write(document, site, true);
    }

    public static void Remove(Ps2ExecutableDocument document)
    {
        if (GetState(document) != DebugSkipIntroLogosPatchState.Applied)
            throw new InvalidDataException("O patch de logos não corresponde à versão instalada pela ferramenta.");
        Write(document, LegacyTransition, false);
        foreach (var timer in LegacyTimers) Write(document, timer, false);
        foreach (var site in CancelSites) Write(document, site, false);
        foreach (var site in FadeSites) Write(document, site, false);
    }

    private static bool Matches(Ps2ExecutableDocument document, Site site, bool patched) =>
        document.ReadBytes(site.Offset, 4).AsSpan().SequenceEqual(patched ? site.Patched : site.Original);
    private static void Write(Ps2ExecutableDocument document, Site site, bool patched) =>
        document.WriteBytes(site.Offset, patched ? site.Patched : site.Original);
}
