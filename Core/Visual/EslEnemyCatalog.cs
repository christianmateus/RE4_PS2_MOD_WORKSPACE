namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class EslEnemyCatalog
{
    public sealed record EnemyInfo(byte Id, string Name, IReadOnlyDictionary<byte, string> Subtypes);

    private static IReadOnlyDictionary<byte, string> S(params (byte id, string name)[] values) =>
        values.ToDictionary(x => x.id, x => x.name);

    private static readonly Dictionary<byte, EnemyInfo> Data = new()
    {
        [0x00] = E(0x00, "Invalid", (0x00,"---"),(0x01,"segundo")),
        [0x01] = E(0x01, "Invalid", (0x00,"---")),
        [0x02] = E(0x02, "Leon", (0x00,"Leon")),
        [0x03] = E(0x03, "Ashley (HUD)", (0x00,"Ashley")),
        [0x04] = E(0x04, "Luis", (0x00,"Partner")),
        [0x05] = E(0x05, "Ashley", (0x00,"Ashley")),
        [0x06] = E(0x06, "Invalid", (0x00,"Invalid")), [0x07] = E(0x07, "Invalid", (0x00,"Invalid")),
        [0x08] = E(0x08, "Invalid", (0x00,"Invalid")), [0x09] = E(0x09, "Invalid", (0x00,"Invalid")),
        [0x0A] = E(0x0A, "Invalid", (0x00,"Invalid")), [0x0B] = E(0x0B, "Invalid", (0x00,"Invalid")),
        [0x0C] = E(0x0C, "Invalid", (0x00,"Invalid")), [0x0D] = E(0x0D, "Invalid", (0x00,"Invalid")),
        [0x0E] = E(0x0E, "Jet-ski", (0x00,"Jet-ski")),
        [0x0F] = E(0x0F, "Boat", (0x00,"Boat"),(0x01,"Boat"),(0x02,"Boat"),(0x03,"Boat"),(0x04,"Boat"),(0x05,"Boat")),
        [0x10] = E(0x10, "Ganado", (0x00,"???")),
        [0x11] = E(0x11, "Zealot", (0x07,"Black"),(0x08,"Red")),
        [0x12] = E(0x12, "Ganado", (0x00,"Don Jose"),(0x01,"Don Manuel"),(0x03,"Don Esteban"),(0x04,"Don Diego"),(0x0B,"Maria"),(0x0C,"Isabel")),
        [0x13] = E(0x13, "Ganado", (0x00,"Don Jose"),(0x01,"Don Manuel"),(0x03,"Don Esteban"),(0x04,"Don Diego"),(0x06,"Merchant")),
        [0x14] = E(0x14, "Zealot", (0x06,"Merchant"),(0x07,"Black"),(0x08,"Red")),
        [0x15] = E(0x15, "Ganado", (0x00,"Don Jose"),(0x03,"Don Esteban"),(0x04,"Don Diego"),(0x0B,"Maria")),
        [0x16] = E(0x16, "Ganado", (0x03,"Don Esteban"),(0x04,"Don Diego"),(0x0B,"Maria"),(0x0C,"Isabel")),
        [0x17] = E(0x17, "Ganado", (0x00,"Don Jose"),(0x01,"Don Manuel"),(0x03,"Don Esteban"),(0x04,"Don Diego"),(0x0C,"Isabel")),
        [0x18] = E(0x18, "Merchant", (0x00,"Merchant"),(0x01,"Merchant"),(0x06,"Merchant"),(0x07,"Merchant")),
        [0x19] = E(0x19, "Zealot", (0x07,"Black")),
        [0x1A] = E(0x1A, "Zealot", (0x07,"Black"),(0x08,"Red"),(0x09,"Blue")),
        [0x1B] = E(0x1B, "Zealot", (0x07,"Black"),(0x0A,"Garrador")),
        [0x1C] = E(0x1C, "Zealot", (0x09,"Blue"),(0x0A,"Garrador"),(0x0D,"Armor Garrador")),
        [0x1D] = E(0x1D, "Soldier", (0x02,"J.J"),(0x0E,"Soldier 1"),(0x0F,"Soldier 2"),(0x10,"Soldier 3"),(0x11,"Soldier 4"),(0x12,"Soldier 5"),(0x13,"Soldier 6"),(0x14,"Soldier 7"),(0x15,"Soldier 8")),
        [0x1E] = E(0x1E, "Soldier", (0x00,"Soldier 1"),(0x06,"Merchant"),(0x0E,"Soldier 2"),(0x0F,"Soldier 3"),(0x17,"Soldier 4"),(0x18,"Soldier 5")),
        [0x1F] = E(0x1F, "Soldier", (0x0E,"Soldier 1"),(0x0F,"Soldier 2"),(0x10,"Soldier 3"),(0x18,"Heavy Soldier")),
        [0x20] = E(0x20, "Soldier", (0x0E,"Soldier 1"),(0x0F,"Soldier 2"),(0x10,"Soldier 3"),(0x11,"Soldier 4"),(0x12,"Soldier 5"),(0x13,"Soldier 6"),(0x14,"Soldier 7"),(0x15,"Soldier 8"),(0x16,"Mega Salvador"),(0x17,"Soldier 8")),
        [0x21] = E(0x21, "Dog", (0x00,"Farm dog"),(0x01,"Injured dog")),
        [0x22] = E(0x22, "Colmillo", (0x00,"Colmillo")), [0x23] = E(0x23, "Crow", (0x00,"Crow")),
        [0x24] = E(0x24, "Snake", (0x00,"Snake")), [0x25] = E(0x25, "Parasite", (0x00,"Type 1"),(0x07,"Type 2"),(0x09,"Type 3")),
        [0x26] = E(0x26, "Cow", (0x00,"Black & white"),(0x01,"Brown")), [0x27] = E(0x27, "Black Bass", (0x00,"Small"),(0x01,"Big")),
        [0x28] = E(0x28, "Chicken", (0x00,"Bright"),(0x01,"Dark")), [0x29] = E(0x29, "Bat", (0x00,"Bat")),
        [0x2A] = E(0x2A, "Trap", (0x00,"Bear Trap"),(0x02,"Explosive")),
        [0x2B] = E(0x2B, "El Gigante", (0x00,"Normal"),(0x01,"With Mask"),(0x02,"Second"),(0x03,"Chained")),
        [0x2C] = E(0x2C, "Verdugo", (0x00,"Normal"),(0x01,"Random Ceiling")),
        [0x2D] = E(0x2D, "Novistador", (0x00,"Normal"),(0x02,"White"),(0x04,"Flying")),
        [0x2E] = E(0x2E, "Spider", (0x00,"Spider")), [0x2F] = E(0x2F, "Del Lago", (0x00,"Normal")),
        [0x30] = E(0x30, "Saddler", (0x00,"Body/Legs"),(0x01,"Head")), [0x31] = E(0x31, "Saddler v2", (0x00,"Default")),
        [0x32] = E(0x32, "Unknown", (0x00,"Default")), [0x33] = E(0x33, "Invalid", (0x00,"Default")),
        [0x34] = E(0x34, "No. & No.", (0x00,"Default")), [0x35] = E(0x35, "Bitores", (0x00,"1st Form"),(0x01,"2nd Form"),(0x02,"Legs")),
        [0x36] = E(0x36, "Regenerator", (0x00,"Normal"),(0x01,"Normal"),(0x02,"Iron Maiden"),(0x03,"Iron Maiden")),
        [0x37] = E(0x37, "Invalid", (0x00,"Default")), [0x38] = E(0x38, "Salazar", (0x00,"Boss"),(0x01,"Tentacle"),(0x02,"Tentacle"),(0x03,"Human")),
        [0x39] = E(0x39, "Krauser", (0x00,"Human"),(0x01,"Human (story)"),(0x02,"Mutated"),(0x03,"Mutated (Ada)")),
        [0x3A] = E(0x3A, "Robot", (0x00,"Flying"),(0x01,"Flying"),(0x02,"Ground")),
        [0x3B] = E(0x3B, "Truck/Wagon", (0x00,"Truck"),(0x01,"Wagon (moves)"),(0x02,"Wagon (explodes)")),
        [0x3C] = E(0x3C, "Knight", (0x00,"Rusty"),(0x01,"Rusty"),(0x02,"Type 2"),(0x03,"Type 2")),
        [0x3D] = E(0x3D, "Helicopter", (0x00,"Helicopter")), [0x3E] = E(0x3E, "r22c Mark", (0x00,"Default")),
        [0x3F] = E(0x3F, "Saddler Human", (0x00,"Saddler")), [0x40] = E(0x40, "Unknown", (0x00,"Default")), [0x41] = E(0x41, "Unknown", (0x00,"Default")),
        [0x42] = E(0x42, "SW Ganado", (0x00,"Don Jose"),(0x01,"Don Manuel"),(0x03,"Don Esteban"),(0x0B,"Maria"),(0x0C,"Isabel")),
        [0x43] = E(0x43, "SW Soldier", (0x00,"Soldier (voice)")),
        [0x44] = E(0x44, "SW Ganado", (0x00,"Don Jose"),(0x03,"Don Esteban"),(0x04,"Don Manuel"),(0x0B,"Maria"),(0x1A,"Don Manuel")),
        [0x4E] = E(0x4E, "SW Ship", (0x00,"Cannon"),(0x01,"Cannon"))
    };

    private static EnemyInfo E(byte id, string name, params (byte id, string name)[] subtypes) => new(id, name, S(subtypes));

    public static string GetEnemyName(byte type) => Data.TryGetValue(type, out var enemy) ? enemy.Name : "Unknown";
    public static string GetSubtypeName(byte type, byte subtype) => Data.TryGetValue(type, out var enemy) && enemy.Subtypes.TryGetValue(subtype, out var name) ? name : "Unknown";
    public static string GetEnemyLabel(byte type) => $"em{type:X2}: {GetEnemyName(type)}";
    public static string GetSubtypeLabel(byte type, byte subtype) => $"0x{subtype:X2}: {GetSubtypeName(type, subtype)}";
    public static string GetFullName(byte type, byte subtype)
    {
        string enemy = GetEnemyName(type), subtypeName = GetSubtypeName(type, subtype);
        if (subtypeName == "Unknown" || subtypeName == "Default" || string.Equals(enemy, subtypeName, StringComparison.OrdinalIgnoreCase)) return enemy;
        return $"{enemy} — {subtypeName}";
    }
    public static IReadOnlyList<EnemyInfo> All => Data.Values.OrderBy(x => x.Id).ToArray();
    public static IReadOnlyDictionary<byte, string> GetSubtypes(byte type) => Data.TryGetValue(type, out var enemy) ? enemy.Subtypes : new Dictionary<byte, string>();
}
