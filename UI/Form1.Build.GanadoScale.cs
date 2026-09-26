using RE4_PS2_MOD_WORKSPACE.Core.Patching;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private static readonly decimal[] GanadoScaleValues = { 0.50m, 0.75m, 1.00m, 1.25m, 1.50m, 2.00m, 2.50m, 3.00m };

    private async Task ApplyGanadoScalePatchAsync(string buildIso)
    {
        decimal value = GanadoScaleValues.Contains(project.GanadoScaleMultiplier) ? project.GanadoScaleMultiplier : 1.00m;
        SetBuildBusy(true, $"Aplicando tamanho dos Ganados ({value:0.00}×)...");
        GanadoScalePatchResult result = await Task.Run(() => GanadoScalePatch.Apply(buildIso, (float)value));
        string action = $"multiplicador global {value:0.00}× e tamanhos individuais habilitados";
        WriteLog($"Inimigos em09 e em10–em4F: {action} em {result.EntryCount} módulo(s) REL ({result.ChangedCount} bloco(s) alterado(s)).");
    }
}
