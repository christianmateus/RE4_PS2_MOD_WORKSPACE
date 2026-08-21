namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>
/// Experimental ESL equipment decoder for Village Ganado packages.
/// The three legacy UI bytes Equip1/Equip2/Weapon are bytes 5/6/7 of the ESL flag_4 field.
/// They are bit flags, not simple item IDs. The mappings below are intentionally limited to
/// values documented/tested by the RE4 modding community and to BINs listed in the enemy DAT catalog.
/// </summary>
public static class EnemyEquipmentCatalog
{
    public sealed record Item(string Name, int? DatEntry, string SourceByte, byte Mask);

    public static bool IsVillageGanadoPackage(byte t) => t is 0x12 or 0x13 or 0x15 or 0x16 or 0x17 or 0x42 or 0x44 or 0x45 or 0x46 or 0x56 or 0x66 or 0xA2 or 0xA3 or 0xA5 or 0xA7;

    public static IReadOnlyList<Item> Decode(EslEnemyEntry e)
    {
        if (!IsVillageGanadoPackage(e.EnemyType)) return Array.Empty<Item>();
        var r = new List<Item>();
        void Add(byte value, byte mask, string name, int? bin, string field) { if ((value & mask) != 0) r.Add(new Item(name, bin, field, mask)); }

        Add(e.Equip1, 0x01, "Patchwork farm hat", 609, "Equip1");
        Add(e.Equip1, 0x02, "Straw farm hat", 609, "Equip1");
        Add(e.Equip1, 0x08, "Torch", 620, "Equip1");
        Add(e.Equip1, 0x20, "Infinite/throwable weapon behavior", null, "Equip1");
        Add(e.Equip1, 0x40, "Knife", 466, "Equip1");
        Add(e.Equip1, 0x80, "Spawn unarmed", null, "Equip1");

        Add(e.Equip2, 0x01, "Can throw dynamite / aggressive throwable behavior", null, "Equip2");
        Add(e.Equip2, 0x02, "Spawn with dynamite", 158, "Equip2");
        Add(e.Equip2, 0x04, "Green beanie", 606, "Equip2");
        Add(e.Equip2, 0x08, "Brown cap", 603, "Equip2");
        Add(e.Equip2, 0x10, "Can become Plaga", null, "Equip2");
        Add(e.Equip2, 0x20, "Sunglasses", 612, "Equip2");
        Add(e.Equip2, 0x40, "Blue beanie", 606, "Equip2");
        Add(e.Equip2, 0x80, "Green cap", 603, "Equip2");

        Add(e.Weapon, 0x08, "Sickle", 614, "Weapon");
        Add(e.Weapon, 0x10, "Chainsaw", 256, "Weapon");
        Add(e.Weapon, 0x20, "Axe", 616, "Weapon");
        Add(e.Weapon, 0x40, "Bucket", 594, "Weapon");
        Add(e.Weapon, 0x80, "Pitchfork", 592, "Weapon");
        return r;
    }


    public static bool IsHandHeldPart(EslEnemyEntry e, int datEntry)
    {
        // Only attach a BIN when the current ESL flags actually request that documented item.
        // Village Ganados use the left hand (bone 16) for the weapons tested so far.
        return Decode(e).Any(x => x.DatEntry == datEntry && x.Name is
            "Axe" or "Pitchfork" or "Sickle" or "Chainsaw" or "Bucket" or "Knife" or "Torch" or "Spawn with dynamite");
    }

    public static IReadOnlySet<int> GetRenderableParts(EslEnemyEntry e, EnemyModelScene model)
    {
        HashSet<int> present = model.Parts.Select(x => x.DatEntryIndex).ToHashSet();
        return Decode(e).Where(x => x.DatEntry.HasValue && present.Contains(x.DatEntry.Value)).Select(x => x.DatEntry!.Value).ToHashSet();
    }

    public static string GetSummary(EslEnemyEntry e)
    {
        var items = Decode(e);
        string flags = $"E1={e.Equip1:X2} E2={e.Equip2:X2} W={e.Weapon:X2}";
        return items.Count == 0 ? flags : flags + " • " + string.Join(", ", items.Select(x => x.Name));
    }
}
