namespace RE4_PS2_MOD_WORKSPACE.Core.Executable;

public enum DebugEnemyHpBarPatchState { Ready, Applied, Incompatible }

public static class DebugEnemyHpBarPatch
{
    private sealed record Segment(int Offset, byte[] Original, byte[] Patched);
    private static readonly Segment[] Segments =
    {
        S(0x1417F8, "ngBAEA==", "AAAAAA=="),
        S(0x141964, "lkQBPAAIgUQ0AKDHAAABRjQAoOc=", "AAAAAAAAAAAAAAAAAAAAAAAAAAA="),
        S(0x141A08, "//8GPEAApCcwAKUn///GNBIeBwwtOAAAhPgFDFAArMdUAKzHhPgFDPD/UCRBAAo8sAQrhi0wAAItOEAAGC5KJQgABCQMAAUkLUAAABoZCAwtSAAA", "ISAgAkAApScwAKYnvFQKDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"),
        S(0x141B24, "BABAEA==", "BAAAEA=="),
        S(0x1962F0, "0P69JzYABTwQAbB/AAGxf/AAsn/gALN/0AC0f8AAtX+wALZ/oAC3f5AAvn+AAL//IAG05wBAoowYfkKM1gBAEBABsHsAAEKM0wBAEC2gAABwAKCvLZgAALYAQBAtqAAAEAACJMhCATwAoIFEdACirxAAvicgALInMAC3J0AAticAQKKMAAAAAP//Bjx0AKiPLSCgAxh+Q4wtKMAD+kMBPAAogUT//8Y0IYhoAC04AAAHAClqAAApbg8AImoIACJuBwCpswAAqbcPAKKzCACitwcAKWoAACluDwAiaggAIm4XAKmzEACptx8AorMYAKK3BwApagAAKW4PACJqCAAibicAqbMgAKm3LwCisygAorcHAClqAAApbg8AImoIACJuNwCpszAAqbc/AKKzOACitwcAKWoAACluDwAiaggAIm5HAKmzQACpt08AorNIAKK3AAChxxAAo8ckAKTHQAgURjgAosfBGBRGSACgxwAhBUaAEBRGAACh5wEAFEYQAKPnJACk5zgAoucSHgcMSACg5///BjwtIKADLShAAv//xjQSHgcMLTgAAP//BjwtIMADLShAAv//xjQSHgcMLTgAAP//BjwtIOACLSjAAv//xjQ=", "0P+9JywAv68oALCvJACxryAAsq8hgIAAIYigACGQwAAYAAQk9vkJDCEoAACQAQiOCQAAEQAAAADUAALF5AADxcAQA0YWQwE8ACCBRMAYBEYDAAAQAAAAAJZEATwAGIFEBAAgxgAAA0YEACDmBABAxgAAA0YEAEDmNgAIPABACI3hQwE8ACiBRGABAsVCEAVGAAAgxsAAAUYAACPmwQABRgAAQ+ZkAQLFQhAFRgQAIMbAAAFGBAAj5sEAAUYEAEPmaAECxUIQBUYIACDGwAABRggAI+bBAAFGCABD5iEgIAIhKEACYIAGPGBgxjQSHgcMITgAALAEAoayBAOGKwBgGAAAAAAmAEAEAAAAACpAYgACAAARAAAAACEQYAAACIJEYAiARgAQg0SgEIBGQwgCRmFEATwAEIFEQggCRjYACDwAQAiNYAECxYIQAUYAACDGAQACRgAAQOZkAQLFghABRgQAIMYBAAJGBABA5mgBAsWCEAFGCAAgxgEAAkYIAEDmISAgAiEoQAIAgAY8AP/GNBIeBwwhOAAABAAAEAAAAAAhEAAA3f8AEAAAAAAGAAQk9vkJDCEoAAAsAL+PKACwjyQAsY8gALKPCADgAzAAvSc=")
    };

    public static DebugEnemyHpBarPatchState GetState(Ps2ExecutableDocument document)
    {
        if (document.Region != ExecutableRegion.DebugSlps) return DebugEnemyHpBarPatchState.Incompatible;
        bool original = Segments.All(x => document.ReadBytes(x.Offset, x.Original.Length).AsSpan().SequenceEqual(x.Original));
        if (original) return DebugEnemyHpBarPatchState.Ready;
        bool patched = Segments.All(x => document.ReadBytes(x.Offset, x.Patched.Length).AsSpan().SequenceEqual(x.Patched));
        return patched ? DebugEnemyHpBarPatchState.Applied : DebugEnemyHpBarPatchState.Incompatible;
    }

    public static void Apply(Ps2ExecutableDocument document) => Change(document, DebugEnemyHpBarPatchState.Ready, true);
    public static void Remove(Ps2ExecutableDocument document) => Change(document, DebugEnemyHpBarPatchState.Applied, false);

    private static void Change(Ps2ExecutableDocument document, DebugEnemyHpBarPatchState required, bool apply)
    {
        if (GetState(document) != required)
            throw new InvalidDataException(apply ? "O SLPS debug não corresponde aos bytes originais esperados." : "O patch de HP não corresponde à versão instalada pela ferramenta.");
        foreach (var segment in Segments) document.WriteBytes(segment.Offset, apply ? segment.Patched : segment.Original);
    }

    private static Segment S(int offset, string original, string patched)
    {
        byte[] a = Convert.FromBase64String(original), b = Convert.FromBase64String(patched);
        if (a.Length != b.Length) throw new InvalidOperationException($"Patch inválido em 0x{offset:X}.");
        return new(offset, a, b);
    }
}