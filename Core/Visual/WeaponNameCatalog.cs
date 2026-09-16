using System.Reflection;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class WeaponNameCatalog
{
    private static readonly IReadOnlyDictionary<string,string> Names=Load();
    public static string Get(string fileName){string id=Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();return Names.TryGetValue(id,out string? name)?name:"Arma desconhecida";}
    public static string Display(string fileName)=>Path.GetFileName(fileName)+"  •  "+Get(fileName);
    private static IReadOnlyDictionary<string,string> Load()
    {
        var result=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);using Stream? stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WeaponList.txt");if(stream==null)return new Dictionary<string,string>();using var reader=new StreamReader(stream);while(reader.ReadLine() is string line){Match m=Regex.Match(line,@"^\s*(wep[0-9a-f]{2})\s*-\s*(.+?)\s*$",RegexOptions.IgnoreCase);if(!m.Success)continue;string id=m.Groups[1].Value,name=m.Groups[2].Value;if(!result.TryGetValue(id,out var aliases)){aliases=new();result[id]=aliases;}if(!aliases.Contains(name,StringComparer.OrdinalIgnoreCase))aliases.Add(name);}return result.ToDictionary(x=>x.Key,x=>string.Join(" / ",x.Value),StringComparer.OrdinalIgnoreCase);
    }
}
