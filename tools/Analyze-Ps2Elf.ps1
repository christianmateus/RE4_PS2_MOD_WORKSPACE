param(
    [Parameter(Mandatory=$true)][string]$Path,
    [string]$SymbolPattern = 'em10|Scale|scale',
    [switch]$ListSections,
    [switch]$SkipSymbols
)

$bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path))
function U16([int]$o) { [BitConverter]::ToUInt16($bytes, $o) }
function U32([int]$o) { [BitConverter]::ToUInt32($bytes, $o) }
function Z([byte[]]$data, [int]$o) {
    $e = $o
    while ($e -lt $data.Length -and $data[$e] -ne 0) { $e++ }
    [Text.Encoding]::ASCII.GetString($data, $o, $e - $o)
}

if ($bytes.Length -lt 52 -or (Z $bytes 1) -ne 'ELF') { throw 'ELF invalido.' }
$shoff = U32 0x20
$shentsize = U16 0x2E
$shnum = U16 0x30
$shstrndx = U16 0x32

$rawSections = for ($i = 0; $i -lt $shnum; $i++) {
    $o = $shoff + $i * $shentsize
    [pscustomobject]@{
        Index=$i; NameOffset=(U32 $o); Type=(U32 ($o+4)); Flags=(U32 ($o+8));
        Address=(U32 ($o+12)); Offset=(U32 ($o+16)); Size=(U32 ($o+20));
        Link=(U32 ($o+24)); Info=(U32 ($o+28)); Align=(U32 ($o+32)); EntrySize=(U32 ($o+36))
    }
}
$shstr = $rawSections[$shstrndx]
$shstrBytes = $bytes[$shstr.Offset..($shstr.Offset + $shstr.Size - 1)]
$sectionTable = foreach ($s in $rawSections) {
    $s | Add-Member NoteProperty Name (Z $shstrBytes $s.NameOffset) -PassThru
}

if ($ListSections) {
    $sectionTable | Select-Object Index,Name,@{n='Type';e={'0x{0:X}' -f $_.Type}},@{n='Address';e={'0x{0:X8}' -f $_.Address}},@{n='Offset';e={'0x{0:X}' -f $_.Offset}},@{n='Size';e={'0x{0:X}' -f $_.Size}},Link,Info
}

if ($SkipSymbols) { return }

foreach ($symsec in $sectionTable | Where-Object Type -eq 2) {
    $strsec = $sectionTable[$symsec.Link]
    $strings = $bytes[$strsec.Offset..($strsec.Offset + $strsec.Size - 1)]
    $entrySize = if ($symsec.EntrySize) { $symsec.EntrySize } else { 16 }
    for ($o = $symsec.Offset; $o -lt $symsec.Offset + $symsec.Size; $o += $entrySize) {
        $nameOffset = U32 $o
        if ($nameOffset -ge $strings.Length) { continue }
        $name = Z $strings $nameOffset
        if ($name -match $SymbolPattern) {
            $value = U32 ($o+4); $size = U32 ($o+8); $info = $bytes[$o+12]; $sectionIndex = U16 ($o+14)
            [pscustomobject]@{Name=$name;Address=('0x{0:X8}' -f $value);Size=('0x{0:X}' -f $size);Type=($info -band 0xF);Section=$sectionIndex}
        }
    }
}
