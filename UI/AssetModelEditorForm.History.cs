using RE4_PS2_MOD_WORKSPACE.Core.Visual;
namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private sealed class AssetObjectListBox:ListBox
    {
        protected override void WndProc(ref Message message)
        {
            if(message.Msg is 0x0201 or 0x0203)
            {
                long coordinates=message.LParam.ToInt64();
                var point=new Point((short)(coordinates&0xffff),(short)((coordinates>>16)&0xffff));
                if(IndexFromPoint(point)<0)return;
            }
            base.WndProc(ref message);
        }
    }
    private bool previewInitialized;
    private readonly Stack<AssetUndoState> editHistory=new();
    private sealed record AssetUndoState(Dictionary<int,byte[]> Edits,bool Dirty,byte ObjectId,int PartOrder,IReadOnlyList<AssetMeshSelection> Selection);

    private void RecordAssetUndo()
    {
        if(objects.SelectedItem is not ObjectItem item)return;
        if(editHistory.Count>=32){var recent=editHistory.Take(31).Reverse().ToArray();editHistory.Clear();foreach(var entry in recent)editHistory.Push(entry);}
        editHistory.Push(new(new Dictionary<int,byte[]>(edits),dirty,item.Definition.Id,activePartOrder,viewport.AssetMeshSelections));
    }
    private void UndoAssetEdit()
    {
        if(!editHistory.TryPop(out var entry)){status.Text="Nenhuma alteração para desfazer";return;}
        edits.Clear();foreach(var pair in entry.Edits)edits[pair.Key]=pair.Value;
        dirty=entry.Dirty;
        int index=objects.Items.Cast<ObjectItem>().ToList().FindIndex(x=>x.Definition.Id==entry.ObjectId);
        if(index>=0)objects.SelectedIndex=index;
        activePartOrder=entry.PartOrder;SelectObject(true);viewport.RestoreAssetSelections(entry.Selection);
        UpdatePackageIndicator();status.Text="Alteração desfeita • Ctrl+Z";
    }
    private void RestoreCurrentObject()
    {
        if(objects.SelectedItem is not ObjectItem item)return;
        var keys=item.Definition.Resources.Select(r=>r.FileOrder).Where(edits.ContainsKey).ToArray();
        if(keys.Length==0)return;
        RecordAssetUndo();foreach(int key in keys)edits.Remove(key);
        dirty=edits.Count>0;SelectObject(true);UpdatePackageIndicator();status.Text="Recursos do objeto restaurados";
    }
    private void InitializeObjectActions()
    {
        var menu=new ContextMenuStrip{Renderer=new DarkToolStripRenderer(),BackColor=Color.FromArgb(31,35,43),ForeColor=Color.Gainsboro};
        void Add(string text,Action action){var item=new ToolStripMenuItem(text);item.Click+=(_,_)=>action();menu.Items.Add(item);}
        Add("Enquadrar objeto",()=>viewport.FitScene());
        var import=new ToolStripMenuItem("Importar OBJ / SMD…");import.Click+=async (_,_)=>await ImportExternalModelAsync();menu.Items.Add(import);
        Add("Exportar modelo e texturas…",ExportObjectBundle);if(etmOnly)Add("Replicar objeto nos cenários…",ReplicateObject);
        menu.Items.Add(new ToolStripSeparator());
        Add("Exportar todas as texturas PNG…",()=>ExportAllTextures(false));Add("Exportar todas as texturas TPL…",()=>ExportAllTextures(true));
        Add("Importar textura PNG / TPL…",ImportTextureFile);
        menu.Items.Add(new ToolStripSeparator());Add("Restaurar recursos do objeto",RestoreCurrentObject);
        Add("Desfazer alteração   Ctrl+Z",UndoAssetEdit);menu.Items.Add(new ToolStripSeparator());Add("Salvar pacote   Ctrl+S",()=>Save(false));Add("Salvar pacote como…",()=>Save(true));
        objects.MouseDown+=(_,e)=>
        {
            int index=objects.IndexFromPoint(e.Location);
            if(index<0)return;
            if(e.Button==MouseButtons.Right){objects.SelectedIndex=index;menu.Show(objects,e.Location);}
        };
        menu.Opening+=(_,e)=>e.Cancel=objects.SelectedItem is not ObjectItem;
        objects.Disposed+=(_,_)=>menu.Dispose();
    }
}
