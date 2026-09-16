using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private string? visualItaPath;
    private ItaScene? visualItaScene;
    private EtmCatalog? visualItmCatalog;

    private async Task LoadVisualItaAsync(string content)
    {
        string? path=Directory.GetFiles(content,"*.ITA",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        visualItaPath=path;
        if(path==null)
        {
            visualItaScene=null;visualItmCatalog=null;lstVisualItaEntries.Items.Clear();visualViewport.SetItaScene(null,null);return;
        }
        visualItaScene=await Task.Run(()=>Ps2ItaReader.Read(path));
        string? itmPath=Directory.GetFiles(content,"*.ITM",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        visualItmCatalog=itmPath==null?null:await Task.Run(()=>Ps2ItmReader.Read(itmPath));
        visualViewport.SetItaScene(visualItaScene,visualItmCatalog);WireVisualItaEvents();
        RefreshVisualItaEntries();
        ExtractLog($"Visual Editor: ITA loaded: {Path.GetFileName(path)} • {visualItaScene.Entries.Count:N0} items • {visualItmCatalog?.Models.Count??0:N0} ITM models.");
    }

    private void ClearVisualIta()
    {
        visualItaPath=null;visualItaScene=null;visualItmCatalog=null;visualViewport?.SetItaScene(null,null);lstVisualItaEntries?.Items.Clear();
    }

    private void RefreshVisualItaEntries(ItaEntry? select=null)
    {
        lstVisualItaEntries.BeginUpdate();
        try{lstVisualItaEntries.Items.Clear();if(visualItaScene!=null)foreach(ItaEntry entry in visualItaScene.Entries)lstVisualItaEntries.Items.Add(entry);}
        finally{lstVisualItaEntries.EndUpdate();}
        if(select!=null)lstVisualItaEntries.SelectedItem=select;
    }

    private void lstVisualItaEntries_SelectedIndexChanged(object? sender,EventArgs e)
    {
        ItaEntry? entry=lstVisualItaEntries.SelectedItem as ItaEntry;pgVisualProperties.SelectedObject=entry;
        lblVisualPropertiesTitle.Text=entry==null?"PROPERTIES • ITA":$"ITA ITEM • #{entry.DataBlockIndex:X2}";visualViewport?.SelectItaEntry(entry);
    }

    private void lstVisualItaEntries_KeyDown(object? sender,KeyEventArgs e)
    {
        if(e.Control&&e.KeyCode==Keys.D){DuplicateSelectedIta();e.Handled=true;e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.Delete){DeleteSelectedIta();e.Handled=true;e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.F&&lstVisualItaEntries.SelectedItem is ItaEntry item){visualViewport?.FocusIta(item);e.Handled=true;e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.G){visualViewport.ItaTransformMode=EtsGizmoMode.Move;visualViewport.RefreshItaGeometry();e.Handled=true;e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.R){visualViewport.ItaTransformMode=EtsGizmoMode.Rotate;visualViewport.RefreshItaGeometry();e.Handled=true;e.SuppressKeyPress=true;}
    }

    private void DuplicateSelectedIta()
    {
        if(visualItaScene==null||lstVisualItaEntries.SelectedItem is not ItaEntry source)return;
        ItaEntry clone=source.Clone();clone.FileOrder=visualItaScene.Entries.Count;
        clone.DataBlockIndex=NextFreeItaByte(visualItaScene.Entries.Select(x=>x.DataBlockIndex));
        clone.ScriptLink=NextFreeItaUShort(visualItaScene.Entries.Select(x=>x.ScriptLink));clone.PositionX+=100f;
        visualItaScene.Entries.Add(clone);MarkItaModified();RefreshVisualItaEntries(clone);visualViewport?.RefreshItaGeometry(clone);
    }

    private void DeleteSelectedIta()
    {
        if(visualItaScene==null||lstVisualItaEntries.SelectedItem is not ItaEntry entry)return;
        visualItaScene.Entries.Remove(entry);ReindexItaFileOrder();MarkItaModified();RefreshVisualItaEntries();visualViewport?.RefreshItaGeometry();
    }

    private static byte NextFreeItaByte(IEnumerable<byte> values){var used=values.ToHashSet();for(int i=0;i<=byte.MaxValue;i++)if(!used.Contains((byte)i))return(byte)i;throw new InvalidOperationException("No free ITA data block index.");}
    private static ushort NextFreeItaUShort(IEnumerable<ushort> values){var used=values.ToHashSet();for(int i=1;i<=ushort.MaxValue;i++)if(!used.Contains((ushort)i))return(ushort)i;throw new InvalidOperationException("No free ITA script link.");}
    private void ReindexItaFileOrder(){if(visualItaScene==null)return;for(int i=0;i<visualItaScene.Entries.Count;i++)visualItaScene.Entries[i].FileOrder=i;}
    private void MarkItaModified(){if(visualItaScene==null)return;visualItaScene.IsModified=true;UpdateVisualStatus();}

    private bool SaveVisualIta()
    {
        if(visualItaScene==null||string.IsNullOrWhiteSpace(visualItaPath))return false;
        try
        {
            bool backup=Ps2ItaWriter.Save(visualItaScene,GetVisualAevBackupPath(visualItaPath));
            ItaScene verified=Ps2ItaReader.Read(visualItaPath);
            visualItaScene=verified;visualViewport.SetItaScene(verified,visualItmCatalog);WireVisualItaEvents();RefreshVisualItaEntries();
            ExtractLog($"Visual Editor: ITA saved and verified: {Path.GetFileName(visualItaPath)}."+(backup?" • original backup preserved.":""));UpdateVisualStatus();return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Save ITA",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: error saving ITA: "+ex.Message);return false;}
    }

    private void WireVisualItaEvents(){visualViewport.ItaEntryClicked-=visualViewport_ItaClicked;visualViewport.ItaEntryClicked+=visualViewport_ItaClicked;visualViewport.ItaEntryEdited-=visualViewport_ItaEdited;visualViewport.ItaEntryEdited+=visualViewport_ItaEdited;}
    private void visualViewport_ItaClicked(ItaEntry? entry){if(entry!=null&&tabVisualEntities!=null)tabVisualEntities.SelectedIndex=9;lstVisualItaEntries.SelectedItem=entry;}
    private void visualViewport_ItaEdited(ItaEntry entry){MarkItaModified();lstVisualItaEntries.Refresh();pgVisualProperties.Refresh();}
}
