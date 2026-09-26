using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using RE4_PS2_MOD_WORKSPACE.Core.Iso;
using RE4_PS2_MOD_WORKSPACE.Core.Afs;
using RE4_PS2_MOD_WORKSPACE.Core.Patching;

if (args.Length < 1) return;
byte[] d;
string pattern;
if(Path.GetExtension(args[0]).Equals(".iso",StringComparison.OrdinalIgnoreCase)){
 if(args.Length>2&&args[1].Equals("SCALE",StringComparison.OrdinalIgnoreCase)){var x=GanadoScalePatch.Apply(args[0],float.Parse(args[2],System.Globalization.CultureInfo.InvariantCulture));Console.WriteLine($"entries={x.EntryCount} changed={x.ChangedCount} scale={x.Multiplier}");return;}
 if(args.Length>1&&args[1].Equals("ESLSTAT",StringComparison.OrdinalIgnoreCase)){
  var afs=AfsService.OpenDefaultAfsFromIso(args[0]);using var fs=File.OpenRead(args[0]);int files=0,active=0,nonzero=0,inactiveNonzero=0;
  var byOffset=new int[4];var values=new Dictionary<uint,int>();
  foreach(var e in afs.Entries.Where(e=>!e.IsDummy&&e.FileName.StartsWith("emleon",StringComparison.OrdinalIgnoreCase)&&e.FileName.EndsWith(".esl",StringComparison.OrdinalIgnoreCase))){files++;byte[] eslRaw=new byte[e.StoredSize];fs.Position=afs.IsoAfsEntry.DataOffset+e.Offset;fs.ReadExactly(eslRaw);for(int o=0;o+32<=eslRaw.Length;o+=32){bool isActive=eslRaw[o]!=0;if(isActive)active++;uint v=BinaryPrimitives.ReadUInt32LittleEndian(eslRaw.AsSpan(o+0x1c,4));if(v!=0){nonzero++;if(!isActive)inactiveNonzero++;values[v]=values.GetValueOrDefault(v)+1;for(int j=0;j<4;j++)if(eslRaw[o+0x1c+j]!=0)byOffset[j]++;}}}
  Console.WriteLine($"files={files} active={active} dummyNonzero={nonzero} inactiveNonzero={inactiveNonzero}");for(int j=0;j<4;j++)Console.WriteLine($"+0x{0x1c+j:X2}: nonzero={byOffset[j]}");foreach(var p in values.OrderByDescending(x=>x.Value).Take(30))Console.WriteLine($"0x{p.Key:X8} count={p.Value}");return;}
 var entries=Iso9660Reader.ReadAllFiles(args[0]);
 if(args.Length>1&&args[1].Equals("AFS",StringComparison.OrdinalIgnoreCase)){var afs=AfsService.OpenDefaultAfsFromIso(args[0]);var arx=new Regex(args.Length>2?args[2]:"em12|udas",RegexOptions.IgnoreCase);foreach(var e in afs.Entries.Where(e=>arx.IsMatch(e.FileName)))Console.WriteLine($"{e.Index,5} 0x{afs.IsoAfsEntry.DataOffset+e.Offset:X12} 0x{e.StoredSize:X8} {e.FileName}");return;}
 if(args.Length>2&&args[1].Equals("AFSREL",StringComparison.OrdinalIgnoreCase)){var afs=AfsService.OpenDefaultAfsFromIso(args[0]);int idx=int.Parse(args[2]);var e=afs.Entries[idx];d=new byte[e.StoredSize];using var fs=File.OpenRead(args[0]);fs.Position=afs.IsoAfsEntry.DataOffset+e.Offset;fs.ReadExactly(d);Console.WriteLine($"RAW {idx} {e.FileName} size=0x{d.Length:X}");ScanRaw(d);return;}
 if(args.Length>4&&args[1].Equals("AFSDUMP",StringComparison.OrdinalIgnoreCase)){var afs=AfsService.OpenDefaultAfsFromIso(args[0]);int idx=int.Parse(args[2]);int start=Convert.ToInt32(args[3],16),count=Convert.ToInt32(args[4],16);var e=afs.Entries[idx];d=new byte[e.StoredSize];using var fs=File.OpenRead(args[0]);fs.Position=afs.IsoAfsEntry.DataOffset+e.Offset;fs.ReadExactly(d);for(int o=start;o<start+count;o+=4)Console.WriteLine($"+0x{o:X6}: {BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(o,4)):X8}");return;}
 if(args.Length>4&&args[1].Equals("AFSFIND",StringComparison.OrdinalIgnoreCase)){var afs=AfsService.OpenDefaultAfsFromIso(args[0]);int idx=int.Parse(args[2]);uint wanted=Convert.ToUInt32(args[3],16),mask=Convert.ToUInt32(args[4],16);var e=afs.Entries[idx];d=new byte[e.StoredSize];using var fs=File.OpenRead(args[0]);fs.Position=afs.IsoAfsEntry.DataOffset+e.Offset;fs.ReadExactly(d);for(int o=0;o+4<=d.Length;o+=4){uint w=BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(o,4));if((w&mask)==(wanted&mask))Console.WriteLine($"+0x{o:X6}: {w:X8}");}return;}
 if(args.Length>3&&args[1].Equals("AFSZERO",StringComparison.OrdinalIgnoreCase)){var afs=AfsService.OpenDefaultAfsFromIso(args[0]);int idx=int.Parse(args[2]),min=Convert.ToInt32(args[3],16);var e=afs.Entries[idx];d=new byte[e.StoredSize];using var fs=File.OpenRead(args[0]);fs.Position=afs.IsoAfsEntry.DataOffset+e.Offset;fs.ReadExactly(d);for(int o=0;o<d.Length;){if(d[o]!=0){o++;continue;}int s=o;while(o<d.Length&&d[o]==0)o++;if(o-s>=min)Console.WriteLine($"+0x{s:X6} len=0x{o-s:X}");}return;}
 if(args.Length>1&&args[1].Equals("MODULES.DAT",StringComparison.OrdinalIgnoreCase)){
  var e=entries.First(x=>x.FullPath.Equals("MODULES.DAT",StringComparison.OrdinalIgnoreCase));d=new byte[e.Size];using var fs=File.OpenRead(args[0]);fs.Position=e.DataOffset;fs.ReadExactly(d);pattern=args.Length>2?args[2]:"em10|scale";
 }else{string p=args.Length>1?args[1]:"em12|udas|em1[0-9a-f]";var r=new Regex(p,RegexOptions.IgnoreCase);foreach(var e in entries.Where(e=>r.IsMatch(e.FullPath)))Console.WriteLine($"0x{e.DataOffset:X12} 0x{e.Size:X8} {e.FullPath}");return;}
}else{d=File.ReadAllBytes(args[0]);pattern=args.Length>1?args[1]:"em10|scale";}

static void ScanRaw(byte[] b){
 uint W(int o)=>BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o,4));
 for(int i=0;i+48<b.Length;i+=4){uint a=W(i);if((a>>26)!=0x39)continue;int off=(short)a;if(off is not(0xE0 or 0xE4 or 0xE8))continue;uint key=a&0x03FF0000;int mask=1<<((off-0xE0)/4),last=i;for(int j=i+4;j<Math.Min(b.Length,i+48);j+=4){uint w=W(j);if((w>>26)==0x39&&(w&0x03FF0000)==key){int x=(short)w;if(x is 0xE0 or 0xE4 or 0xE8){mask|=1<<((x-0xE0)/4);last=j;}}}if(mask==7){Console.WriteLine($"SCALE3 file+0x{i:X}..0x{last:X} base={(a>>21)&31} ft={(a>>16)&31}");i=last;}}
}
uint U32(int o) => BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(o, 4));
ushort U16(int o) => BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(o, 2));
string Z(ReadOnlySpan<byte> b, int o) { int e=o; while(e<b.Length&&b[e]!=0)e++; return Encoding.ASCII.GetString(b[o..e]); }
int shoff=checked((int)U32(0x20)), shentsize=U16(0x2e), shnum=U16(0x30), shstrndx=U16(0x32);
var sections=new List<(uint no,uint type,uint addr,uint off,uint size,uint link,uint entsize,string name)>();
var raw=new List<(uint no,uint type,uint addr,uint off,uint size,uint link,uint entsize)>();
for(uint i=0;i<shnum;i++){int o=shoff+(int)i*shentsize;raw.Add((U32(o),U32(o+4),U32(o+12),U32(o+16),U32(o+20),U32(o+24),U32(o+36)));}
var names=d.AsSpan((int)raw[shstrndx].off,(int)raw[shstrndx].size);
foreach(var s in raw)sections.Add((s.no,s.type,s.addr,s.off,s.size,s.link,s.entsize,Z(names,(int)s.no)));
var rx=new Regex(pattern,RegexOptions.IgnoreCase);
if(pattern=="__sections__")foreach(var s in sections)Console.WriteLine($"SEC type={s.type:X8} addr={s.addr:X8} off={s.off:X8} size={s.size:X8} link={s.link} {s.name}");
foreach(var sec in sections.Where(s=>s.type==2)){
 var str=sections[(int)sec.link];var strings=d.AsSpan((int)str.off,(int)str.size);int es=(int)(sec.entsize==0?16:sec.entsize);
 for(int o=(int)sec.off;o<(int)(sec.off+sec.size);o+=es){uint no=U32(o);if(no>=strings.Length)continue;string n=Z(strings,(int)no);if(!rx.IsMatch(n))continue;Console.WriteLine($"{U32(o+4):X8} {U32(o+8):X8} sh={U16(o+14),3} type={d[o+12]&15} {n}");}
}

var md=sections.FirstOrDefault(s=>s.type==0x70000005);
if(md.size!=0){
 int h=(int)md.off;uint isymMax=U32(h+32),cbSym=U32(h+36),issMax=U32(h+56),cbSs=U32(h+60),ifdMax=U32(h+72),cbFd=U32(h+76);
 var ss=d.AsSpan((int)cbSs,(int)issMax);
 for(int fi=0;fi<ifdMax;fi++){
  int f=(int)cbFd+fi*72;uint adr=U32(f);int rss=(int)U32(f+4),issBase=(int)U32(f+8),isymBase=(int)U32(f+16),csym=(int)U32(f+20);
  if(rss>=0&&rss<ss.Length){string fn=Z(ss,rss);if(rx.IsMatch(fn))Console.WriteLine($"FDR {adr:X8} fd={fi,3} syms={isymBase}..{isymBase+csym-1} {fn}");}
  for(int si=0;si<csym&&isymBase+si<isymMax;si++){
   int so=(int)cbSym+(isymBase+si)*12;int iss=(int)U32(so);if(iss<0||iss>=ss.Length)continue;
   string n=Z(ss,iss);if(rx.IsMatch(n))Console.WriteLine($"MDEBUG {U32(so+4):X8} fd={fi,3} sym={isymBase+si,6} {n}");
  }
 }
}

var text=sections.FirstOrDefault(s=>s.name==".text");
if(text.size!=0){
 var words=new uint[text.size/4];for(int i=0;i<words.Length;i++)words[i]=U32((int)text.off+i*4);
 for(int i=0;i<words.Length;i++){
  uint a=words[i];if((a>>26)!=0x39)continue;int off=(short)a; if(off is not (0xE0 or 0xE4 or 0xE8))continue;
  uint key=a&0x03FF0000;int mask=1<<((off-0xE0)/4),last=i;
  for(int j=i+1;j<Math.Min(words.Length,i+12);j++){uint w=words[j];if((w>>26)==0x39&&(w&0x03FF0000)==key){int x=(short)w;if(x is 0xE0 or 0xE4 or 0xE8){mask|=1<<((x-0xE0)/4);last=j;}}}
  if(mask==7){uint va=text.addr+(uint)i*4;Console.WriteLine($"SCALE3 {va:X8}..{text.addr+(uint)last*4:X8} base={(a>>21)&31} ft={(a>>16)&31}");i=last;}
 }
}
