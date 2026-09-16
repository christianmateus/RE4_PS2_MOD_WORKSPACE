namespace RE4_PS2_MOD_WORKSPACE.Core.Executable;

public enum DebugPlayerSpeedPatchState { Ready, Applied, Incompatible }

public static class DebugPlayerSpeedPatch
{
    private const int HookOffset = 0x6EAEC;
    private const int CodeOffset = 0x22E638;
    private const float NativeSpeed = 0.50f;
    private static readonly byte[] OriginalHook = { 0x6C, 0x57, 0x09, 0x0C };
    private static readonly byte[] OriginalCode = new byte[24];
    public static readonly decimal[] SupportedMultipliers = { 0.50m, 0.75m, 1.00m, 1.25m, 1.50m, 2.00m, 2.50m, 3.00m };

    public static DebugPlayerSpeedPatchState GetState(Ps2ExecutableDocument document)
    {
        if (document.Region != ExecutableRegion.DebugSlps) return DebugPlayerSpeedPatchState.Incompatible;
        byte[] hook = document.ReadBytes(HookOffset, OriginalHook.Length), code = document.ReadBytes(CodeOffset, OriginalCode.Length);
        if (hook.AsSpan().SequenceEqual(OriginalHook) && code.AsSpan().SequenceEqual(OriginalCode)) return DebugPlayerSpeedPatchState.Ready;
        if (hook.AsSpan().SequenceEqual(BuildJumpAndLink(0x0032D638)) && IsOurCode(code)) return DebugPlayerSpeedPatchState.Applied;
        return DebugPlayerSpeedPatchState.Incompatible;
    }

    public static float? GetMultiplier(Ps2ExecutableDocument document)
    {
        if (GetState(document) != DebugPlayerSpeedPatchState.Applied) return null;
        byte[] code = document.ReadBytes(CodeOffset, 8);
        uint bits = ((BitConverter.ToUInt32(code, 0) & 0xFFFFu) << 16) | (BitConverter.ToUInt32(code, 4) & 0xFFFFu);
        return BitConverter.Int32BitsToSingle((int)bits) / NativeSpeed;
    }

    public static void Apply(Ps2ExecutableDocument document, float multiplier)
    {
        if (!float.IsFinite(multiplier) || multiplier < 0.50f || multiplier > 3.0f) throw new ArgumentOutOfRangeException(nameof(multiplier), "Use uma velocidade entre 0,50× e 3,00×.");
        if (GetState(document) == DebugPlayerSpeedPatchState.Incompatible) throw new InvalidDataException("O SLPS debug não corresponde aos bytes esperados para o patch de velocidade.");
        document.WriteBytes(HookOffset, BuildJumpAndLink(0x0032D638));
        document.WriteBytes(CodeOffset, BuildCode(multiplier * NativeSpeed));
    }

    public static void Remove(Ps2ExecutableDocument document)
    {
        if (GetState(document) != DebugPlayerSpeedPatchState.Applied) throw new InvalidDataException("O patch de velocidade não corresponde à versão instalada pela ferramenta.");
        document.WriteBytes(HookOffset, OriginalHook);
        document.WriteBytes(CodeOffset, OriginalCode);
    }

    private static bool IsOurCode(byte[] code)
    {
        byte[] expected = BuildCode(1.0f);
        return code.Length == expected.Length && code.AsSpan(8).SequenceEqual(expected.AsSpan(8));
    }

    private static byte[] BuildCode(float multiplier)
    {
        uint bits = (uint)BitConverter.SingleToInt32Bits(multiplier);
        uint[] words = { 0x3C010000u | (bits >> 16), 0x34210000u | (bits & 0xFFFF), 0x44810000u, 0xE48002F8u, BuildJump(0x00257948), 0x00000000u };
        byte[] result = new byte[words.Length * 4];
        for (int i = 0; i < words.Length; i++) BitConverter.GetBytes(words[i]).CopyTo(result, i * 4);
        return result;
    }

    private static byte[] BuildJumpAndLink(uint address) => BitConverter.GetBytes(0x0C000000u | ((address >> 2) & 0x03FFFFFFu));
    private static uint BuildJump(uint address) => 0x08000000u | ((address >> 2) & 0x03FFFFFFu);
}
