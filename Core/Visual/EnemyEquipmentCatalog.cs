namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>
/// Experimental ESL equipment decoder for Village Ganado packages.
/// The three legacy UI bytes Equip1/Equip2/Weapon are bytes 5/6/7 of the ESL flag_4 field.
/// They are bit flags, not simple item IDs. The mappings below are intentionally limited to
/// values documented/tested by the RE4 modding community and to BINs listed in the enemy DAT catalog.
/// </summary>
public static class EnemyEquipmentCatalog
{
    public static readonly IReadOnlyList<int> KnownEquipmentIdleEntries = new[] { 226, 288, 326, 353, 376 };
    public enum AttachmentPoint { Model, Head, RightHand, LeftHand }

    public sealed record Item(string Name, int? DatEntry, string SourceByte, byte Value, AttachmentPoint Attachment = AttachmentPoint.Model);
    public sealed record Preset(string Name, byte Equip1, byte Equip2, byte Weapon);

    private static readonly Preset[] VillageWeapons =
    {
        new("Nenhuma • 0x00",0,0,0), new("Tocha",0x08,0,0), new("Faca",0x40,0,0), new("Dinamite",0,0x02,0),
        new("Foice / enxada — mão direita • 0x08",0,0,0x08), new("Foice / enxada — mão esquerda • 0x09",0,0,0x09),
        new("Motosserra • 0x10",0,0,0x10), new("Machado • 0x20",0,0,0x20), new("Balde • 0x40",0,0,0x40), new("Tridente • 0x80",0,0,0x80)
    };
    private static readonly Preset[] ZealotWeapons =
    {
        new("Nenhuma • 0x00",0,0,0), new("Tocha",0x08,0,0), new("Besta",0x80,0,0), new("Foice longa • 0x04",0,0,0x04),
        new("Mangual • 0x20",0,0,0x20), new("Bazuca • 0x40",0,0,0x40), new("Escudo • 0x80",0,0,0x80),
        new("Escudo + foice • 0x84",0,0,0x84), new("Escudo + mangual • 0xA0",0,0,0xA0), new("Escudo + bazuca • 0xC0",0,0,0xC0)
    };
    private static readonly Preset[] IslandWeapons =
    {
        new("Nenhuma • 0x00",0,0,0), new("Dinamite",0,0x02,0), new("Bastão elétrico • 0x04",0,0,0x04),
        new("Machado • 0x08",0,0,0x08), new("Motosserra • 0x10",0,0,0x10), new("Mangual • 0x20",0,0,0x20),
        new("Bazuca • 0x40",0,0,0x40), new("Escudo • 0x80",0,0,0x80), new("Escudo + bastão • 0x84",0,0,0x84),
        new("Escudo + machado • 0x8A",0,0,0x8A), new("Escudo + motosserra • 0x90",0,0,0x90),
        new("Escudo + besta • 0x92",0,0,0x92), new("Escudo + mangual • 0xA2",0,0,0xA2)
    };
    private static readonly Preset[] VillageEquipment =
    {
        new("Nenhum acessório",0,0,0), new("Chapéu remendado",0x01,0,0), new("Chapéu de palha",0x02,0,0),
        new("Touca verde",0,0x04,0), new("Boné marrom",0,0x08,0), new("Óculos escuros",0,0x20,0),
        new("Touca azul",0,0x40,0), new("Boné verde",0,0x80,0)
    };
    private static readonly Preset[] ZealotEquipment =
    {
        new("Nenhum acessório",0,0,0), new("Máscara animal",0x01,0,0), new("Capacete de metal",0x02,0,0),
        new("Capuz",0x10,0,0), new("Tatuagem na cabeça",0,0x20,0), new("Colar dourado",0,0x40,0),
        new("Colar prateado",0,0x80,0), new("Capuz com fita branca",0,0x04,0), new("Capuz sem fita",0,0x08,0)
    };
    private static readonly Preset[] IslandEquipment =
    {
        new("Nenhum acessório",0,0,0), new("Capuz preto",0,0x04,0), new("Capacete de minerador • 0x08",0,0x08,0),
        new("Capacete de minerador • 0x20",0,0x20,0), new("Chapéu de aba redonda",0,0x80,0)
    };

    public static bool IsVillageGanadoPackage(byte t) => t is 0x12 or 0x13 or 0x15 or 0x16 or 0x17 or 0x42 or 0x44 or 0x45 or 0x46 or 0x56 or 0x66 or 0xA2 or 0xA3 or 0xA5 or 0xA7;

    public static IReadOnlyList<Item> Decode(EslEnemyEntry e)
    {
        if (IsZealotPackage(e.EnemyType)) return DecodeZealot(e);
        if (IsIslandPackage(e.EnemyType)) return DecodeIsland(e);
        if (!IsVillageGanadoPackage(e.EnemyType)) return Array.Empty<Item>();
        var r = new List<Item>();
        void AddFlag(byte value, byte mask, string name, int? bin, string field, AttachmentPoint attachment = AttachmentPoint.Model)
        { if ((value & mask) != 0) AddUnique(r, new Item(name, bin, field, mask, attachment)); }

        AddFlag(e.Equip1, 0x01, "Patchwork farm hat", 609, "Equip1", AttachmentPoint.Head);
        AddFlag(e.Equip1, 0x02, "Straw farm hat", 609, "Equip1", AttachmentPoint.Head);
        AddFlag(e.Equip1, 0x08, "Torch", 620, "Equip1", AttachmentPoint.RightHand);
        AddFlag(e.Equip1, 0x20, "Infinite/throwable weapon behavior", null, "Equip1");
        AddFlag(e.Equip1, 0x40, "Knife", 466, "Equip1", AttachmentPoint.RightHand);
        AddFlag(e.Equip1, 0x80, "Spawn unarmed", null, "Equip1");

        AddFlag(e.Equip2, 0x01, "Can throw dynamite / aggressive throwable behavior", null, "Equip2");
        AddFlag(e.Equip2, 0x02, "Spawn with dynamite", 158, "Equip2", AttachmentPoint.RightHand);
        AddFlag(e.Equip2, 0x04, "Green beanie", 606, "Equip2", AttachmentPoint.Head);
        AddFlag(e.Equip2, 0x08, "Brown cap", 603, "Equip2", AttachmentPoint.Head);
        AddFlag(e.Equip2, 0x10, "Can become Plaga", null, "Equip2");
        AddFlag(e.Equip2, 0x20, "Sunglasses", 612, "Equip2", AttachmentPoint.Head);
        AddFlag(e.Equip2, 0x40, "Blue beanie", 606, "Equip2", AttachmentPoint.Head);
        AddFlag(e.Equip2, 0x80, "Green cap", 603, "Equip2", AttachmentPoint.Head);

        int high = e.Weapon >> 4, low = e.Weapon & 0x0F;
        if (high is 1 or 3 or 5 or 7 or 9 or 0xB or 0xD or 0xF)
            AddUnique(r, new Item("Chainsaw", 256, "Weapon", (byte)(high << 4), AttachmentPoint.RightHand));
        else if (high is 2 or 6 or 0xA or 0xE)
            AddUnique(r, new Item("Axe", 616, "Weapon", (byte)(high << 4), AttachmentPoint.RightHand));
        else if (high == 4)
            AddUnique(r, new Item("Bucket", 594, "Weapon", 0x40, AttachmentPoint.RightHand));
        else if (high is 8 or 0xC)
            AddUnique(r, new Item("Pitchfork", 592, "Weapon", (byte)(high << 4), AttachmentPoint.RightHand));

        // The reference table explicitly alternates the hoe/sickle between hands:
        // even low nibble = right hand, odd low nibble = left hand.
        if (low >= 8)
            AddUnique(r, new Item("Sickle / Hoe", 614, "Weapon", (byte)low,
                (low & 1) == 0 ? AttachmentPoint.RightHand : AttachmentPoint.LeftHand));
        return r;
    }

    private static IReadOnlyList<Item> DecodeZealot(EslEnemyEntry e)
    {
        var r = new List<Item>();
        int high = e.Weapon >> 4, low = e.Weapon & 0x0F;
        int equip1Low=e.Equip1&0x0F,equip2High=e.Equip2>>4,equip2Low=e.Equip2&0x0F;
        if ((equip1Low & 1) != 0) AddUnique(r, new Item("Animal / goat mask", 529, "Equip1", 0x01, AttachmentPoint.Head));
        if ((equip1Low & 2) != 0) AddUnique(r, new Item("Full metal helmet", 527, "Equip1", 0x02, AttachmentPoint.Head));
        if ((equip2High & 2) != 0) AddUnique(r, new Item("Blood / head tattoo", 531, "Equip2", 0x20, AttachmentPoint.Head));
        if ((equip2High & 4) != 0) AddUnique(r, new Item("Golden necklace", 521, "Equip2", 0x40, AttachmentPoint.Head));
        if ((equip2High & 8) != 0) AddUnique(r, new Item("Silver necklace", 523, "Equip2", 0x80, AttachmentPoint.Head));
        if ((equip2Low & 4) != 0) AddUnique(r, new Item("Hood with white ribbon", 510, "Equip2", 0x04, AttachmentPoint.Head));
        else if ((equip2Low & 8) != 0) AddUnique(r, new Item("Hood", 511, "Equip2", 0x08, AttachmentPoint.Head));
        if ((e.Equip1 & 0x08) != 0) AddUnique(r, new Item("Torch", 620, "Equip1", 0x08, AttachmentPoint.RightHand));
        if ((e.Equip1 & 0x80) != 0) AddUnique(r, new Item("Crossbow", 622, "Equip1", 0x80, AttachmentPoint.RightHand));
        if (high >= 8) AddUnique(r, new Item("Shield", 351, "Weapon", 0x80, AttachmentPoint.LeftHand));
        if (high is 2 or 3 or 6 or 7 or 0xA or 0xB or 0xE or 0xF)
            AddUnique(r, new Item("Flail / Spike ball", 626, "Weapon", (byte)(high << 4), AttachmentPoint.RightHand));
        else if (high is 4 or 5 or 0xC or 0xD)
            AddUnique(r, new Item("RPG", 392, "Weapon", (byte)(high << 4), AttachmentPoint.RightHand));
        if (low is >= 4 and <= 7 or >= 0xC and <= 0xF)
            AddUnique(r, new Item("Long scythe", 618, "Weapon", (byte)low, AttachmentPoint.RightHand));
        return r;
    }

    private static IReadOnlyList<Item> DecodeIsland(EslEnemyEntry e)
    {
        var r = new List<Item>();
        if ((e.Equip2 & 0x04) != 0) AddUnique(r, new Item("Black hood / head cover", 606, "Equip2", 0x04, AttachmentPoint.Head));
        if ((e.Equip2 & 0x28) != 0) AddUnique(r, new Item("Miner helmet", 569, "Equip2", (byte)(e.Equip2&0x28), AttachmentPoint.Head));
        if ((e.Equip2 & 0x80) != 0) AddUnique(r, new Item("Round-brim cap", 609, "Equip2", 0x80, AttachmentPoint.Head));
        if ((e.Equip2 & 0x02) != 0) AddUnique(r, new Item("Dynamite", 158, "Equip2", 0x02, AttachmentPoint.RightHand));
        byte weapon = e.Weapon;
        if ((weapon & 0x80) != 0) AddUnique(r, new Item("Shield", 351, "Weapon", 0x80, AttachmentPoint.LeftHand));
        int baseWeapon = weapon & 0x7F;
        if (weapon==0x92) AddUnique(r, new Item("Crossbow", 622, "Weapon", weapon, AttachmentPoint.RightHand));
        else if (baseWeapon is 0x04 or 0x05 or 0x06) AddUnique(r, new Item("Electronic stick", 614, "Weapon", (byte)baseWeapon, AttachmentPoint.RightHand));
        else if (baseWeapon is 0x08 or 0x0A) AddUnique(r, new Item("Axe", 616, "Weapon", (byte)baseWeapon, AttachmentPoint.RightHand));
        else if (baseWeapon is 0x10 or 0x11 or 0x12 or 0x30 or 0x50 or 0x70) AddUnique(r, new Item("Chainsaw", 256, "Weapon", (byte)baseWeapon, AttachmentPoint.RightHand));
        else if (baseWeapon is 0x20 or 0x60 or 0x22) AddUnique(r, new Item("Flail / Chainball", 626, "Weapon", (byte)baseWeapon, AttachmentPoint.RightHand));
        else if (baseWeapon is 0x40 or 0x1F or 0x52) AddUnique(r, new Item("RPG", 392, "Weapon", (byte)baseWeapon, AttachmentPoint.RightHand));
        return r;
    }

    private static void AddUnique(List<Item> items, Item item)
    {
        if (item.DatEntry.HasValue && items.Any(x => x.DatEntry == item.DatEntry && x.Attachment == item.Attachment)) return;
        items.Add(item);
    }

    private static bool IsZealotPackage(byte t) => t is 0x14 or 0x1A or 0x1B or 0x1C or 0x47 or 0x48 or 0x49 or 0x4A or 0x4B or 0x4C or 0x57 or 0x58 or 0x59 or 0x5A or 0x5B or 0x5C or 0x67 or 0x68 or 0x69 or 0x6A or 0x6B or 0x6C;
    private static bool IsIslandPackage(byte t) => t is 0x1D or 0x1E or 0x1F or 0x20 or 0x43 or 0x4D or 0x4F or 0x50 or 0x5D or 0x5F or 0x60 or 0x6D or 0x6F or 0x70 or 0xAE;

    public static IReadOnlyList<string> GetWeaponPresetNames(byte enemyType) => GetWeaponPresets(enemyType).Select(x=>x.Name).ToArray();
    public static IReadOnlyList<string> GetEquipmentPresetNames(byte enemyType) => GetEquipmentPresets(enemyType).Select(x=>x.Name).ToArray();
    public static string GetWeaponPresetName(EslEnemyEntry e) => FindCurrentWeaponPreset(e)?.Name ?? $"Personalizado • E1 {e.Equip1:X2} E2 {e.Equip2:X2} W {e.Weapon:X2}";
    public static string GetEquipmentPresetName(EslEnemyEntry e) => FindCurrentEquipmentPreset(e)?.Name ?? $"Personalizado • E1 {e.Equip1:X2} E2 {e.Equip2:X2}";

    public static void ApplyWeaponPreset(EslEnemyEntry e,string name)
    {
        Preset? p=GetWeaponPresets(e.EnemyType).FirstOrDefault(x=>x.Name==name);
        if(p==null)
        {
            System.Text.RegularExpressions.Match m=System.Text.RegularExpressions.Regex.Match(name,@"E1 ([0-9A-F]{2}) E2 ([0-9A-F]{2}) W ([0-9A-F]{2})",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if(m.Success){e.Equip1=Convert.ToByte(m.Groups[1].Value,16);e.Equip2=Convert.ToByte(m.Groups[2].Value,16);e.Weapon=Convert.ToByte(m.Groups[3].Value,16);}return;
        }
        if(IsZealotPackage(e.EnemyType))e.Equip1=(byte)((e.Equip1&~0x88)|p.Equip1);
        else e.Equip1=(byte)((e.Equip1&~0x48)|p.Equip1);
        e.Equip2=(byte)((e.Equip2&~0x02)|p.Equip2);e.Weapon=p.Weapon;
    }

    public static void ApplyEquipmentPreset(EslEnemyEntry e,string name)
    {
        Preset? p=GetEquipmentPresets(e.EnemyType).FirstOrDefault(x=>x.Name==name);
        if(p==null)
        {
            System.Text.RegularExpressions.Match m=System.Text.RegularExpressions.Regex.Match(name,@"E1 ([0-9A-F]{2}) E2 ([0-9A-F]{2})",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if(m.Success){e.Equip1=Convert.ToByte(m.Groups[1].Value,16);e.Equip2=Convert.ToByte(m.Groups[2].Value,16);}return;
        }
        if(IsZealotPackage(e.EnemyType))
        { e.Equip1=(byte)((e.Equip1&~0x17)|p.Equip1);e.Equip2=(byte)((e.Equip2&0x02)|p.Equip2); }
        else if(IsIslandPackage(e.EnemyType))
        { e.Equip2=(byte)((e.Equip2&~0xAC)|p.Equip2); }
        else
        { e.Equip1=(byte)((e.Equip1&~0x03)|p.Equip1);e.Equip2=(byte)((e.Equip2&~0xEC)|p.Equip2); }
    }

    private static IReadOnlyList<Preset> GetWeaponPresets(byte type) => IsZealotPackage(type)?ZealotWeapons:IsIslandPackage(type)?IslandWeapons:VillageWeapons;
    private static IReadOnlyList<Preset> GetEquipmentPresets(byte type) => IsZealotPackage(type)?ZealotEquipment:IsIslandPackage(type)?IslandEquipment:VillageEquipment;
    private static Preset? FindCurrentWeaponPreset(EslEnemyEntry e)
    {
        return GetWeaponPresets(e.EnemyType).FirstOrDefault(p=>p.Weapon==e.Weapon &&
            (IsZealotPackage(e.EnemyType)?(e.Equip1&0x88)==p.Equip1:(e.Equip1&0x48)==p.Equip1) && (e.Equip2&0x02)==p.Equip2);
    }
    private static Preset? FindCurrentEquipmentPreset(EslEnemyEntry e)
    {
        return GetEquipmentPresets(e.EnemyType).FirstOrDefault(p=>IsZealotPackage(e.EnemyType)
            ?(e.Equip1&0x17)==p.Equip1&&(e.Equip2&~0x02)==p.Equip2
            :IsIslandPackage(e.EnemyType)?(e.Equip2&0xAC)==p.Equip2
            :(e.Equip1&0x03)==p.Equip1&&(e.Equip2&0xEC)==p.Equip2);
    }

    public static AttachmentPoint GetAttachmentPoint(EslEnemyEntry e, int datEntry)
    {
        return Decode(e).FirstOrDefault(x => x.DatEntry == datEntry && x.Attachment != AttachmentPoint.Model)?.Attachment
            ?? AttachmentPoint.Model;
    }

    /// <summary>
    /// Local authoring-axis correction applied before the equipment inherits the hand bone pose.
    /// Equipment BINs do not all use the same forward/up axes, so these values must remain scoped
    /// to the DAT entry instead of changing the common hand attachment transform.
    /// </summary>
    public static (float X,float Y,float Z) GetAttachmentRotation(EslEnemyEntry e,int datEntry)
    {
        int type=GetNativeWeaponType(e,datEntry);
        (float X,float Y,float Z) rotation=type switch
        {
            1 => (0f,-41.0625f,0f),                    // hoe
            6 => (0f,-42.125f,0f),                     // long scythe
            5 => (0f,-120f,0f),                        // bucket
            2 or 3 or 7 or 9 or 0xA or 0xF or 0x10 => (0f,-59.6f,0f),
            0xB => (0f,-45f,0f),                       // flail
            4 => (85f,0f,-90f),                        // chainsaw
            8 => (90f,0f,-90f),                        // crossbow
            0xC => (0f,0f,0f),                         // rocket launcher
            0x11 => (0f,-90f,0f),                      // shield
            _ => (0f,0f,0f)
        };
        if(GetAttachmentPoint(e,datEntry)==AttachmentPoint.LeftHand && type!=0x11)
        {
            // em10WeaponSet mirrors ordinary left-handed weapons around the character.
            rotation=type is 1 or 8 or 0xC
                ? (rotation.X,-rotation.Y,-rotation.Z)
                : (rotation.X,rotation.Y,-rotation.Z+180f);
        }
        return rotation;
    }

    /// <summary>Native local position from em10WeaponSet, converted from game units to viewer units.</summary>
    public static (float X,float Y,float Z) GetAttachmentOffset(EslEnemyEntry e,int datEntry)
    {
        if(GetAttachmentPoint(e,datEntry)==AttachmentPoint.Head)
        {
            // em10SetAccesory / EM10_ACC_OBJ12: native offsets relative to head part 0x04.
            if(IsVillageGanadoPackage(e.EnemyType))return datEntry switch
            {
                603 => (0f,1.257f,0.2f),       // cap
                606 => (0f,0.05f,0f),          // head wrap / beanie
                609 => (0f,0.87f,0.11f),       // farm hat
                612 => (0f,0.5471f,0.7273f),   // glasses
                _ => (0f,0f,0f)
            };
            if(IsIslandPackage(e.EnemyType) && datEntry is 569 or 609)return (0f,0.8f,0f);
            return (0f,0f,0f);
        }
        // The PS2 knife BIN has no em10WeaponSet entry. Its authored grip end is offset from
        // the object origin; move that measured endpoint onto the right-hand attachment bone.
        if(datEntry==466)return (-0.075f,0.003125f,1.5328125f);
        int type=GetNativeWeaponType(e,datEntry);
        (float X,float Y,float Z) offset=type switch
        {
            1 => (-8.4358f,0.0581f,8.6687f),           // hoe
            6 => (-4.03f,-1.2094f,2.9624f),            // long scythe
            5 => (-0.8f,-0.35f,-0.1f),                 // bucket
            2 or 3 or 7 or 9 or 0xA or 0xF or 0x10 => (-3.1385f,-0.212f,1.0221f),
            0xB => (-0.86f,-0.28f,-0.06f),             // flail
            4 => (-3.0424f,-0.1786f,-0.5743f),         // chainsaw
            8 => (-3.8425f,-0.2401f,0.1794f),          // crossbow
            0xC => (2.2f,1.2f,0.25f),                  // rocket launcher
            0x11 => (-2.37f,0.0002f,0.14f),            // shield
            _ => (0f,0f,0f)
        };
        if(GetAttachmentPoint(e,datEntry)==AttachmentPoint.LeftHand && type!=0x11)offset.X=-offset.X;
        return offset;
    }

    private static int GetNativeWeaponType(EslEnemyEntry e,int datEntry) => datEntry switch
    {
        // The ESL low-nibble 0x08 flag is em10's bit 27: Wep_type 3 (sickle).
        // Wep_type 1 is the separate hoe selected by bit 31 and must not be used here.
        614 => IsIslandPackage(e.EnemyType)?0xF:3, // electric rod / village sickle
        616 => 2,                                  // axe
        // ESL Weapon high bit 0x80 is em10's bit 31 and uses the Wep_type 1 transform.
        // The PS2 package supplies the pitchfork model for that slot.
        592 => 1,                                  // pitchfork / farm tool
        256 => 4,                                  // chainsaw
        594 => 5,                                  // bucket
        618 => 6,                                  // long scythe
        620 => IsIslandPackage(e.EnemyType)?0x10:7,// torch / soldier variant
        622 => 8,                                  // crossbow
        158 => 9,                                  // dynamite
        626 => 0xB,                                // flail / chainball
        392 => 0xC,                                // rocket launcher
        351 => 0x11,                               // shield (em10ShieldSet)
        _ => 0
    };

    /// <summary>Returns the documented FCV DAT entry for the currently equipped weapon.</summary>
    public static int? GetEquipmentIdleAnimationEntry(EslEnemyEntry e)
    {
        IReadOnlyList<Item> items=Decode(e);
        // A held weapon determines the stance before the off-hand shield. This also handles
        // combinations such as shield + RPG without incorrectly selecting shield-only idle.
        if(items.Any(x=>x.DatEntry==392))return 376;
        if(items.Any(x=>x.DatEntry==622))return 288;
        if(items.Any(x=>x.DatEntry==256))return 226;
        if(items.Any(x=>x.DatEntry==592))return 326;
        if(items.Any(x=>x.DatEntry==351))return 353;
        return null;
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
