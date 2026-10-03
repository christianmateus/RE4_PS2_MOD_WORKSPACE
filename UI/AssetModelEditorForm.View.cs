using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private sealed record EditorView(byte? ObjectId,int Part,int Resource,int TextureSource,int TextureIndex,ScenarioCameraState Camera,IReadOnlyList<AssetMeshSelection> Selection);
    private EditorView CaptureEditorView()=>new((objects.SelectedItem as ObjectItem)?.Definition.Id,activePartOrder,Selected()?.FileOrder??-1,TextureResource()?.FileOrder??-1,CurrentTextureIndex,viewport.GetCameraState(),viewport.AssetMeshSelections);
    private void RestoreEditorView(EditorView view)
    {
        int index=objects.Items.Cast<ObjectItem>().ToList().FindIndex(x=>x.Definition.Id==view.ObjectId);
        if(index>=0)objects.SelectedIndex=index;
        activePartOrder=view.Part;previewInitialized=true;SelectObject(true);
        foreach(ListViewItem row in resources.Items)row.Selected=row.Tag is EtmResource r&&r.FileOrder==view.Resource;
        SelectPreferredTextureResource(view.TextureSource);
        if(textureIndex.Items.Count>0)textureIndex.SelectedIndex=Math.Min(view.TextureIndex,textureIndex.Items.Count-1);
        viewport.RestoreAssetSelections(view.Selection);viewport.SetCameraState(view.Camera);
    }
    private void ResetObjectPosition()
    {
        if(objects.SelectedItem is not ObjectItem item)return;
        var effective=EffectiveCatalog();if(!effective.ModelParts.TryGetValue(item.Definition.Id,out var parts))return;
        var part=parts.FirstOrDefault(p=>p.Bin.FileOrder==activePartOrder);if(part==null)return;
        var document=Ps2BinDocument.Parse(Data(part.Bin));if(document.Vertices.Count==0)return;
        var min=document.Vertices.Select(v=>v.Position).Aggregate(new Vector3(float.PositiveInfinity),Vector3.Min);
        var max=document.Vertices.Select(v=>v.Position).Aggregate(new Vector3(float.NegativeInfinity),Vector3.Max);
        Vector3 center=(min+max)*.5f;if(center.LengthSquared()<1e-10f){status.Text="O centro do BIN já está em (0,0,0)";return;}
        var view=CaptureEditorView();document.Translate(document.Vertices.Select(v=>v.Offset),-center);
        RecordAssetUndo();edits[part.Bin.FileOrder]=document.Source;Changed("Centro do BIN posicionado em (0,0,0)");RestoreEditorView(view);
    }
}
