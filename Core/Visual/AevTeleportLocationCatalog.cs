using System.Reflection;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed record AevTeleportLocation(string SourceRoom,string DestinationRoom,byte[] Data,string DisplayName);

public static class AevTeleportLocationCatalog
{
    private static readonly Regex RoutePattern=new(@"^\s*r(?<source>[0-9a-f]{3})\s+to\s+r(?<destination>[0-9a-f]{3})\b",RegexOptions.IgnoreCase|RegexOptions.Compiled);
    private static readonly Lazy<IReadOnlyList<AevTeleportLocation>> Cached=new(Load);
    public static IReadOnlyList<AevTeleportLocation> Locations=>Cached.Value;

    public static AevTeleportLocation? FindByDisplayName(string? value)=>Locations.FirstOrDefault(x=>x.DisplayName.Equals(value,StringComparison.OrdinalIgnoreCase));
    public static AevTeleportLocation? FindByData(ReadOnlySpan<byte> data)
    {
        foreach(AevTeleportLocation location in Locations)if(data.SequenceEqual(location.Data))return location;
        return null;
    }

    private static IReadOnlyList<AevTeleportLocation> Load()
    {
        var parsed=new List<(string Source,string Destination,byte[] Data)>();Assembly assembly=typeof(AevTeleportLocationCatalog).Assembly;
        foreach(string resource in new[]{"TeleportLocations1.txt","TeleportLocations2.txt"})
        {
            using Stream? stream=assembly.GetManifestResourceStream(resource);if(stream==null)continue;using var reader=new StreamReader(stream,true);string[] lines=reader.ReadToEnd().Split(new[]{"\r\n","\n"},StringSplitOptions.None);
            for(int i=0;i<lines.Length;i++)
            {
                Match match=RoutePattern.Match(lines[i]);if(!match.Success)continue;
                for(int j=i+1;j<Math.Min(lines.Length,i+5);j++)
                {
                    string[] tokens=lines[j].Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(tokens.Length!=18)continue;
                    var bytes=new byte[18];bool valid=true;for(int k=0;k<tokens.Length;k++)if(!byte.TryParse(tokens[k],System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out bytes[k])){valid=false;break;}
                    if(valid)parsed.Add(("r"+match.Groups["source"].Value.ToLowerInvariant(),"r"+match.Groups["destination"].Value.ToLowerInvariant(),bytes));break;
                }
            }
        }
        var unique=parsed.GroupBy(x=>$"{x.Source}>{x.Destination}:{Convert.ToHexString(x.Data)}",StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).OrderBy(x=>x.Destination,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.Source,StringComparer.OrdinalIgnoreCase).ToArray();
        var routeCounts=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);var result=new List<AevTeleportLocation>(unique.Length);
        foreach(var item in unique){string key=$"{item.Source}>{item.Destination}";routeCounts.TryGetValue(key,out int occurrence);occurrence++;routeCounts[key]=occurrence;string suffix=occurrence==1?"":$" • opção {occurrence}";result.Add(new(item.Source,item.Destination,item.Data,$"Destino {item.Destination}  •  vindo de {item.Source}{suffix}"));}
        return result;
    }
}
