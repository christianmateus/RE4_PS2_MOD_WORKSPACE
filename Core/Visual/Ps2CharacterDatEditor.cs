using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>
/// Edits character and enemy DAT entries while keeping the existing public editing API.
/// Entry I/O, skeleton adaptation, mesh editing, and textures are grouped in separate files.
/// </summary>
public static partial class Ps2CharacterDatEditor
{
    public enum TextureTransform { Rotate90, FlipX, FlipY }
    public enum SkeletonAdaptMode { Automatic, Rigid, Distributed, KeepDonor }

}
