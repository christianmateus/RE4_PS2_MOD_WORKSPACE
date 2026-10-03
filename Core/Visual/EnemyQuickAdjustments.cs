using System.Security.Cryptography;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public enum EnemyQuickDifficulty { Nenhum, Facil, Normal, Dificil, Profissional, Brutal, Pesadelo, Impossivel }
public sealed record EnemyQuickAdjustmentOptions(bool SetFixedHp,int HpValue,EnemyQuickDifficulty Difficulty,bool FullDifficulty,bool RandomizeWeapons,bool RandomizeEquipment,bool RandomizeScale,int ScaleMin,int ScaleMax,string Seed,bool ActiveOnly,byte? StageId,byte? RoomId);
public readonly record struct EnemyQuickAdjustmentResult(int Matched,int Changed,int Weapons,int Equipment,int Scales);

public static class EnemyQuickAdjustments
{
    private const int MaxEnemyHp=short.MaxValue;
    public static EnemyQuickAdjustmentResult Apply(EslScene scene,EnemyQuickAdjustmentOptions o,string scopeKey)
    {
        int matched=0,changed=0,weapons=0,equipment=0,scales=0;
        foreach(EslEnemyEntry e in scene.Entries)
        {
            if(o.ActiveOnly&&e.Active==0||o.StageId.HasValue&&(e.StageID!=o.StageId||e.RoomID!=o.RoomId))continue;
            matched++;ushort hp=e.Health;byte sight=e.SightRange,e1=e.Equip1,e2=e.Equip2,w=e.Weapon,size=e.Unknown4;
            var rng=new Random(StableSeed(o.Seed,scopeKey,e.Index,e.EnemyType,e.StageID,e.RoomID));DifficultyInfo info=GetDifficulty(o.Difficulty);
            if(o.SetFixedHp)e.Health=(ushort)Math.Clamp(o.HpValue,1,MaxEnemyHp);else if(o.Difficulty!=EnemyQuickDifficulty.Nenhum)e.Health=(ushort)RandomHundred(rng,info.MinHp,info.MaxHp);
            if(o.FullDifficulty&&o.Difficulty!=EnemyQuickDifficulty.Nenhum)e.SightRange=(byte)Math.Clamp((int)Math.Round(e.SightRange*info.SightMultiplier),0,byte.MaxValue);
            if(EnemyEquipmentCatalog.SupportsPresets(e.EnemyType))
            {
                if(o.FullDifficulty&&info.ArmChance>0&&rng.Next(100)<info.ArmChance&&e.Weapon==0)ApplyRandomWeapon(e,rng);
                if(o.RandomizeWeapons){ApplyRandomWeapon(e,rng);weapons++;}if(o.RandomizeEquipment){ApplyRandomEquipment(e,rng);equipment++;}
                if(o.RandomizeScale){int min=Math.Clamp(Math.Min(o.ScaleMin,o.ScaleMax),8,48),max=Math.Clamp(Math.Max(o.ScaleMin,o.ScaleMax),8,48);int[] choices=new[]{8,12,16,20,24,32,40,48}.Where(x=>x>=min&&x<=max).ToArray();e.Unknown4=(byte)(choices.Length>0?choices[rng.Next(choices.Length)]:min);scales++;}
            }
            if(hp!=e.Health||sight!=e.SightRange||e1!=e.Equip1||e2!=e.Equip2||w!=e.Weapon||size!=e.Unknown4)changed++;
        }
        return new(matched,changed,weapons,equipment,scales);
    }
    private static int RandomHundred(Random rng,int min,int max){min=Math.Clamp((int)Math.Ceiling(min/100m)*100,100,MaxEnemyHp);max=Math.Clamp(max/100*100,min,MaxEnemyHp);return rng.Next(min/100,max/100+1)*100;}
    private static void ApplyRandomWeapon(EslEnemyEntry e,Random rng){var p=EnemyEquipmentCatalog.GetWeaponPresetNames(e.EnemyType);if(p.Count>0)EnemyEquipmentCatalog.ApplyWeaponPreset(e,p[rng.Next(p.Count)]);}
    private static void ApplyRandomEquipment(EslEnemyEntry e,Random rng){var p=EnemyEquipmentCatalog.GetEquipmentPresetNames(e.EnemyType);if(p.Count>0)EnemyEquipmentCatalog.ApplyEquipmentPreset(e,p[rng.Next(p.Count)]);}
    private readonly record struct DifficultyInfo(int MinHp,int MaxHp,decimal SightMultiplier,int ArmChance);
    private static DifficultyInfo GetDifficulty(EnemyQuickDifficulty v)=>v switch{EnemyQuickDifficulty.Facil=>new(500,1000,.75m,0),EnemyQuickDifficulty.Normal=>new(1000,2000,1m,10),EnemyQuickDifficulty.Dificil=>new(2500,5000,1.15m,30),EnemyQuickDifficulty.Profissional=>new(5000,8000,1.25m,45),EnemyQuickDifficulty.Brutal=>new(8000,14000,1.35m,60),EnemyQuickDifficulty.Pesadelo=>new(14000,20000,1.50m,75),EnemyQuickDifficulty.Impossivel=>new(20000,30000,1.75m,95),_=>new(0,0,1m,0)};
    private static int StableSeed(string text,string scope,int index,byte type,byte stage,byte room){byte[] hash=SHA256.HashData(Encoding.UTF8.GetBytes($"{text}\n{scope}\n{index}\n{type}\n{stage}\n{room}"));return BitConverter.ToInt32(hash,0);}
}
