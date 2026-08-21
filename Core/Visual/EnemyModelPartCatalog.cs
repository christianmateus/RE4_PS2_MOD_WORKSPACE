namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>
/// Automatic core-model composition for emXX.dat enemy packages.
///
/// Sources used for the known mappings:
/// - Enemy type/subtype catalog from the legacy ESL editor data.
/// - Hevilz / RE4 UHD "List For All Enemies - Em UDAS - BIN List".
///
/// The editor intentionally renders only the essential character/body model.
/// Weapons, loose equipment, hats, Plagas, projectiles, effects and other
/// accessories remain disabled in AUTO mode.
///
/// IDs below are DAT table entry IDs (the original xxx.bin names), not the
/// zero-based BIN ordinal used by the debug list.
/// </summary>
public static class EnemyModelPartCatalog
{
    private static IReadOnlySet<int> P(params int[] ids) => new HashSet<int>(ids);

    // Common core pieces in the mixed Ganado packages.
    private static readonly IReadOnlySet<int> VillageFirst = P(440, 442, 443, 444, 449);
    private static readonly IReadOnlySet<int> VillageSlim  = P(468, 470, 471, 444, 449);
    private static readonly IReadOnlySet<int> VillageOld   = P(472, 474, 475, 444, 449);
    private static readonly IReadOnlySet<int> VillageFat   = P(476, 478, 479, 444, 449);
    private static readonly IReadOnlySet<int> Female1      = P(453, 455, 456, 444, 449);
    private static readonly IReadOnlySet<int> Female2      = P(457, 459, 460, 444, 449);
    private static readonly IReadOnlySet<int> Merchant     = P(483, 485, 489, 491, 492, 496, 500);

    // Zealot / Island packages.
    private static readonly IReadOnlySet<int> ZealotBase   = P(504, 506, 507, 512, 517);
    private static readonly IReadOnlySet<int> ZealotBlue   = P(537, 539, 507, 512, 517);
    private static readonly IReadOnlySet<int> IslandBase   = P(555, 557, 559, 560, 565);

    /// <summary>
    /// Returns the DAT entries that make up the safe, equipment-free core
    /// model for a documented enemy/subtype. Null means there is not enough
    /// documentation to choose a single correct model automatically.
    /// </summary>
    public static IReadOnlySet<int>? GetAutomaticCoreParts(byte enemyType, byte subtype)
    {
        // ------------------------------------------------------------
        // Mixed Village Ganado packages (legacy ESL subtype names)
        // ------------------------------------------------------------
        switch (enemyType)
        {
            case 0x12: // em12
            case 0xA2: // ema2
                return VillageMale(subtype);

            case 0x13: // em13
            case 0xA3: // ema3
                if (subtype == 0x06) return Merchant;
                return VillageMale(subtype);

            case 0x15: // em15
            case 0xA5: // ema5
                if (subtype == 0x0B) return Female1; // Maria
                return VillageMale(subtype);

            case 0x16: // em16
                if (subtype == 0x0B) return Female1; // Maria
                if (subtype == 0x0C) return Female2; // Isabel
                return VillageMale(subtype);

            case 0x17: // em17
            case 0xA7: // ema7
                if (subtype == 0x0C) return Female2; // Isabel
                return VillageMale(subtype);

            case 0x42: // em42
                if (subtype == 0x0B) return Female1;
                if (subtype == 0x0C) return Female2;
                return VillageMale(subtype);

            case 0x44: // em44
            case 0x45: // em45
                if (subtype == 0x0B) return Female1;
                if (subtype == 0x0C) return Female2;
                // The old ESL mapping labels 0x04 differently for em44;
                // geometry is FAT in the documented package, so only use
                // the canonical male subtype mapping where the parts exist.
                return VillageMale(subtype);

            case 0x46: // em46
            case 0x56: // em56
            case 0x66: // em66
                if (subtype == 0x0B) return Female1;
                if (subtype == 0x0C) return Female2;
                return VillageMale(subtype);
        }

        // ------------------------------------------------------------
        // Merchant
        // ------------------------------------------------------------
        if (enemyType == 0x18) // em18 uses a smaller, dedicated numbering
            return P(1, 3, 7, 9, 10, 14, 15);

        // ------------------------------------------------------------
        // Castle Zealots / Garradors
        // ------------------------------------------------------------
        if (IsZealotPackage(enemyType))
        {
            if (subtype == 0x06) return Merchant;
            if (subtype == 0x09) return ZealotBlue;
            if (subtype == 0x0A) return P(541); // Garrador
            if (subtype == 0x0D) return P(543); // Armored/Iron Garrador
            if (subtype == 0x07 || subtype == 0x08) return ZealotBase;
            return null;
        }

        // ------------------------------------------------------------
        // Island Ganados / Soldiers
        // ------------------------------------------------------------
        if (IsIslandPackage(enemyType))
        {
            if (subtype == 0x02 && (enemyType == 0x1D || enemyType == 0x1F || enemyType == 0x4D || enemyType == 0x5D || enemyType == 0x5F || enemyType == 0x6D || enemyType == 0x6F))
                return P(551); // J.J. is a complete body model.
            if (subtype == 0x06) return Merchant;
            if (subtype == 0x16 && (enemyType == 0x20 || enemyType == 0x50 || enemyType == 0x60 || enemyType == 0x70))
                return P(476); // Mega Salvador
            return IslandBase;
        }

        // ------------------------------------------------------------
        // Animals / simple enemies
        // ------------------------------------------------------------
        switch (enemyType)
        {
            case 0x21: return P(0); // Dog
            case 0x22: return P(0); // Colmillo / Wolf
            case 0x23: return P(1, 3); // Crow body + resting wings
            case 0x24: return P(1); // Snake (egg excluded)
            case 0x25: return P(0); // Parasite
            case 0x26: return P(0); // Cow
            case 0x27: return P(0); // Fish
            case 0x28: return P(0); // Chicken (egg excluded)
            case 0x29: return P(0); // Bat
        }

        // ------------------------------------------------------------
        // Traps
        // ------------------------------------------------------------
        if (enemyType == 0x2A)
        {
            if (subtype == 0x00) return P(0); // Metal/Bear trap
            if (subtype == 0x02) return P(2); // Mine trap
            return null;
        }

        // ------------------------------------------------------------
        // El Gigante / em2b and em71
        // ------------------------------------------------------------
        if (enemyType == 0x2B || enemyType == 0x71)
        {
            return subtype switch
            {
                0x00 => P(1, 2),   // Normal
                0x01 => P(4, 5),   // Mask
                0x02 => P(7, 8),   // Normal body #2
                0x03 => P(10, 11), // Belts (iron/chain excluded)
                _ => null
            };
        }

        // Verdugo
        if (enemyType == 0x2C) return P(1);

        // Novistador: choose the first documented mesh for each family.
        if (enemyType == 0x2D)
        {
            return subtype switch
            {
                0x00 => P(1), // normal type 1
                0x02 => P(5), // alternate/white normal body
                0x04 => P(3), // first winged body
                _ => null
            };
        }

        if (enemyType == 0x2E) return P(0); // Spider
        if (enemyType == 0x2F) return P(0); // Del Lago (baby/FX excluded)

        // Saddler human-like package em30.
        if (enemyType == 0x30)
            return P(0, 2, 4, 5, 8, 9); // body, head, hair, coat, both hands

        // Saddler mutated: main body + legs. Attack/projectile pieces excluded.
        if (enemyType == 0x31)
            return P(0, 1);

        // U3 (legacy catalog had this as Unknown). Plaga and Leon death pieces excluded.
        if (enemyType == 0x32)
            return P(0, 2);

        // em34 contains THREE different characters and the legacy ESL map does
        // not document which subtype selects Mendez/Salazar/Verdugo. Do not
        // guess and accidentally render all three simultaneously.
        if (enemyType == 0x34) return null;

        // Mendez mutated.
        if (enemyType == 0x35)
        {
            return subtype switch
            {
                0x00 => P(0, 2), // intact upper + lower body
                0x01 => P(3, 4), // cut-in-half variants
                0x02 => P(4),    // lower body / legs
                _ => null
            };
        }

        // Regenerator / Iron Maiden. Use intact body segments only; holes,
        // slices and limb stumps are damage-state alternatives and excluded.
        if (enemyType == 0x36)
        {
            if (subtype == 0x00 || subtype == 0x01)
                return P(0, 1, 3, 5, 7, 9, 11, 13, 14);
            if (subtype == 0x02 || subtype == 0x03)
                return P(17, 18, 20, 22, 24, 26, 28, 30, 31);
            return null;
        }

        // Salazar mutated.
        if (enemyType == 0x38)
        {
            return subtype switch
            {
                0x00 => P(0, 1, 3), // boss body + main tentacle + cover
                0x01 => P(1),
                0x02 => P(1),
                0x03 => P(2), // exposed Salazar body
                _ => null
            };
        }

        // Krauser. Weapons, grenade, arrow and knife are intentionally omitted.
        if (enemyType == 0x39)
        {
            if (subtype == 0x00 || subtype == 0x01)
                return P(1, 2, 3, 6, 10, 17);
            if (subtype == 0x02 || subtype == 0x03)
                return P(302, 303, 304);
            return null;
        }

        // Robots
        if (enemyType == 0x3A)
        {
            if (subtype == 0x00 || subtype == 0x01) return P(1);
            if (subtype == 0x02) return P(7);
            return null;
        }

        // Truck / trolley
        if (enemyType == 0x3B)
        {
            if (subtype == 0x00) return P(0);
            if (subtype == 0x01 || subtype == 0x02) return P(7);
            return null;
        }

        // Armadura: body + inside parts, weapon omitted.
        if (enemyType == 0x3C)
        {
            if (subtype == 0x00 || subtype == 0x01) return P(1, 3);
            if (subtype == 0x02 || subtype == 0x03) return P(5, 7);
            return null;
        }

        if (enemyType == 0x3D) return P(1); // Helicopter body

        // Shooting-range wooden character. Only one default subtype is known;
        // use the first complete body rather than all broken alternatives.
        if (enemyType == 0x3E) return P(1);

        // Saddler (human) in Separate Ways.
        if (enemyType == 0x3F) return P(4, 5, 6, 7);

        // Shipyard cannon package. RPG is equipment and is excluded.
        if (enemyType == 0x4E)
        {
            if (subtype == 0x00) return P(0);
            if (subtype == 0x01) return P(2);
            return null;
        }

        return null;
    }

    private static IReadOnlySet<int>? VillageMale(byte subtype) => subtype switch
    {
        0x00 => VillageFirst, // FIRST = Don Jose
        0x01 => VillageSlim,  // SLIM = Don Manuel
        0x03 => VillageOld,   // OLD = Don Esteban
        0x04 => VillageFat,   // FAT = Don Diego
        _ => null
    };

    private static bool IsZealotPackage(byte t) => t == 0x11 || t == 0x14 || t == 0x19 || t == 0x1A || t == 0x1B || t == 0x1C || t == 0x40 || t == 0x41 || t == 0x4C || t == 0x5C || t == 0x6C || t == 0xAA || t == 0xAB || t == 0xA1 || t == 0xA4 || t == 0xA9;

    private static bool IsIslandPackage(byte t) => t == 0x1D || t == 0x1E || t == 0x1F || t == 0x20 || t == 0x43 || t == 0x4D || t == 0x4F || t == 0x50 || t == 0x5D || t == 0x5F || t == 0x60 || t == 0x6D || t == 0x6F || t == 0x70 || t == 0xAE;

    public static bool CanApplyAutomaticCoreParts(EnemyModelScene model, byte enemyType, byte subtype)
    {
        IReadOnlySet<int>? wanted = GetAutomaticCoreParts(enemyType, subtype);
        if (wanted == null || wanted.Count == 0) return false;
        HashSet<int> present = model.Parts.Select(x => x.DatEntryIndex).ToHashSet();
        return wanted.All(present.Contains);
    }

    public static string? GetKnownPartName(int id) => id switch
    {
        0 => "Body / Primary Part",
        1 => "Body / Primary Part",
        2 => "Secondary Body Part",
        3 => "Secondary / Inside Part",
        4 => "Body Variant / Secondary Part",
        5 => "Body / Head Variant",
        6 => "Body Part / Hand",
        7 => "Body Variant / Secondary Part",
        8 => "Body Part / Hand",
        9 => "Body Part / Hand",
        10 => "Body Part / Hand",
        11 => "Body Part",
        13 => "Body Part",
        14 => "Body Part",
        15 => "Body Part",
        17 => "Body / Variant Part",
        18 => "Body / Variant Part",
        20 => "Body / Variant Part",
        22 => "Body / Variant Part",
        23 => "Body / Variant Part",
        24 => "Body / Variant Part",
        26 => "Body / Variant Part",
        28 => "Body / Variant Part",
        30 => "Body / Variant Part",
        31 => "Body / Variant Part",
        158 => "Dynamite",
        256 => "Chainsaw",
        258 => "Chainsaw #2",
        302 => "Krauser Mutated Body",
        303 => "Krauser Head",
        304 => "Krauser Mutated Left Arm",
        351 => "Shield",
        392 => "RPG",
        394 => "Right Hand",
        440 => "FIRST Body (Don Jose)",
        442 => "FIRST Head (Don Jose)",
        443 => "FIRST Neck (Don Jose)",
        444 => "Right Hand #1",
        445 => "Right Hand #2",
        446 => "Right Hand #3",
        447 => "Right Hand #4",
        448 => "Right Hand #5",
        449 => "Left Hand #1",
        450 => "Left Hand #2",
        451 => "Left Hand #3",
        452 => "Left Hand #4",
        453 => "Female Body #1",
        455 => "Female Head #1",
        456 => "Female Neck #1",
        457 => "Female Body #2",
        459 => "Female Head #2",
        460 => "Female Neck #2",
        461 => "Female Salvador Head",
        463 => "Female Head Cover",
        466 => "Knife",
        468 => "SLIM Body (Don Manuel)",
        470 => "SLIM Head (Don Manuel)",
        471 => "SLIM Neck (Don Manuel)",
        472 => "OLD Body (Don Esteban)",
        474 => "OLD Head (Don Esteban)",
        475 => "OLD Neck (Don Esteban)",
        476 => "FAT / Mega Salvador Body",
        478 => "FAT Head (Don Diego)",
        479 => "FAT Neck (Don Diego)",
        480 => "Salvador Head",
        483 => "Merchant Mask / Bag / Boots",
        485 => "Merchant Head",
        489 => "Merchant Coat",
        490 => "Merchant Coat #2",
        491 => "Merchant Inside Body",
        492 => "Merchant Outside Body",
        496 => "Right Hand #1",
        497 => "Right Hand #2",
        498 => "Right Hand #3",
        499 => "Right Hand #4",
        500 => "Left Hand #1",
        501 => "Left Hand #2",
        502 => "Left Hand #3",
        503 => "Left Hand #4",
        504 => "Zealot Body",
        506 => "Zealot Head",
        507 => "Zealot Neck",
        512 => "Zealot Right Hand #1",
        517 => "Zealot Left Hand #1",
        537 => "Blue Zealot Body",
        539 => "Blue Zealot Head",
        541 => "Garrador",
        543 => "Iron Garrador",
        545 => "Garrador Chain",
        547 => "Garrador Plaga",
        551 => "J.J.",
        553 => "J.J. Ammo",
        555 => "Island Ganado Body",
        557 => "Island Ganado Head",
        559 => "Island Ganado Neck",
        560 => "Island Right Hand #1",
        565 => "Island Left Hand #1",
        569 => "Island Head / Mask",
        571 => "Island Headband",
        573 => "Island Neck Variant",
        575 => "Island Body Cover",
        577 => "Island Body Cover #2",
        579 => "Island Ganado Body Variant",
        581 => "Island Ganado Head Variant",
        583 => "Island Ganado Head Variant #2",
        585 => "Island Ganado Head Variant #3",
        587 => "Island Ganado Full Head",
        589 => "Plaga Streaks",
        592 => "Pitchfork",
        594 => "Bucket",
        599 => "Trolley",
        603 => "Cap / Hat",
        606 => "Winter Hat / Head Cover",
        609 => "Hat / Cap",
        612 => "Sunglasses",
        614 => "Sickle / Electronic Stick",
        616 => "Axe",
        618 => "Scythe",
        620 => "Torch / Big Hammer",
        622 => "Crossbow",
        624 => "Arrow",
        626 => "Flail",
        629 => "Plaga",
        640 => "Castle Plaga",
        _ => null
    };
}
