# Internal PS2 model codec

This module adapts the RE4 PS2 BIN Tool V.1.5.0 algorithms by JADERLINK to the
workspace's .NET 8 application. Attribution and MIT licenses are distributed in
`THIRD_PARTY_NOTICES.txt`. No external converter executable or reference project
is required to build or run the application.

Responsibilities:

- `Decoding`: read the binary model and export model/material text.
- `TextFormats`: parse OBJ, MTL and Studio Model Data.
- `Encoding`: compile interchange geometry, optimize triangle strips, pack VIF
  segments, quantize vertices and write binary tables and headers.
- `Materials`: represent material flags and texture slots.
- `ModelImportPreparation`: normalize OBJ references, align geometry and assign
  receiver weights when requested.
- `BinConversionService`: preserve receiver format/rig flags and joint blends,
  bind materials, validate results and restore the worker's numeric culture.
- `Core/Visual/Ps2ModelConversionService`: application-facing async conversion,
  model statistics/preview and ownership of temporary conversion files.

The original console dispatcher is deliberately excluded. Receiver BIN metadata
is decoded in memory; `.idxps2bin` files are unnecessary. Exported character
`.idxmaterial` and `.character.json` sidecars remain supported for editing and
validation. Cancellation is checked before work and between compilation phases;
an already running strip optimization completes before observing cancellation.

Validation:

```powershell
dotnet build RE4_PS2_MOD_WORKSPACE.csproj
dotnet run --project tools/NativeBinValidation -- "<BIN or ETM>" "<optional scenario SMD>"
dotnet run --project tools/CharacterSmdValidation -- "<original em12.dat>"
dotnet run --project tools/EtmValidation -- "<directory with ETM files>"
```

The conversion tests work on temporary copies. They check geometry, texture
coordinates, materials, weighted meshes, character rigs, selective DAT writes,
join and cancellation. Rendering in PCSX2 remains a separate manual check.
