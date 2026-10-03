using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class WeaponPreviewModel
{
    public static EnemyModelScene MergeLeonAndFirstWeaponModel(EnemyModelScene leon, EnemyModelScene weapon, string sourcePath, out int weaponPartBinIndex)
    {
        EnemyModelPart source = weapon.Parts.Where(x => x.Triangles.Count > 0).OrderBy(x => x.DatEntryIndex).FirstOrDefault()
            ?? throw new InvalidDataException("O primeiro modelo BIN da arma não contém geometria renderizável.");
        int tplKey = source.TplEntryIndex;
        int mergedTplKey = leon.TexturePackages.Keys.DefaultIfEmpty(-1).Max() + 1;
        var triangles = source.Triangles.Select(x => x with { TplEntryIndex = tplKey >= 0 ? mergedTplKey : -1 }).ToArray();
        weaponPartBinIndex = leon.Parts.Select(x => x.BinIndex).DefaultIfEmpty(-1).Max() + 1;
        var added = new EnemyModelPart
        {
            BinIndex = weaponPartBinIndex, DatEntryIndex = source.DatEntryIndex,
            TplEntryIndex = tplKey >= 0 ? mergedTplKey : -1, TplResolution = source.TplResolution,
            DiffuseMaps = source.DiffuseMaps, Triangles = triangles, BoundsMin = source.BoundsMin, BoundsMax = source.BoundsMax
        };
        var packages = leon.TexturePackages.ToDictionary(x => x.Key, x => x.Value);
        if (tplKey >= 0 && weapon.TexturePackages.TryGetValue(tplKey, out EnemyTexturePackage? package))
            packages[mergedTplKey] = new EnemyTexturePackage { DatEntryIndex = mergedTplKey, Data = package.Data };
        return new EnemyModelScene
        {
            Skeleton = leon.Skeleton, SkeletonSourceDatEntryIndex = leon.SkeletonSourceDatEntryIndex,
            EnemyType = leon.EnemyType, SourcePath = sourcePath,
            DatEntryCount = leon.DatEntryCount + weapon.DatEntryCount,
            BinCount = leon.BinCount + 1, LoadedBinCount = leon.LoadedBinCount + 1,
            Warnings = leon.Warnings.Concat(weapon.Warnings).ToArray(),
            Parts = leon.Parts.Append(added).ToArray(), TexturePackages = packages,
            Triangles = leon.Triangles.Concat(triangles).ToArray(),
            BoundsMin = Vector3.Min(leon.BoundsMin, source.BoundsMin),
            BoundsMax = Vector3.Max(leon.BoundsMax, source.BoundsMax)
        };
    }
}
