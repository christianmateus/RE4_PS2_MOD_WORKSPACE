using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private string? visualEsePath,visualFsePath,visualDsePath;
    private EseScene? visualEseScene;private FseScene? visualFseScene;private DseScene? visualDseScene;

    private async Task LoadVisualSoundAsync(string content)
    {
        visualEsePath=Directory.GetFiles(content,"*.ESE",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        visualFsePath=Directory.GetFiles(content,"*.FSE",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        visualDsePath=Directory.GetFiles(content,"*.DSE",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        visualEseScene=visualEsePath==null?null:await Task.Run(()=>Ps2EseReader.Read(visualEsePath));
        visualFseScene=visualFsePath==null?null:await Task.Run(()=>Ps2FseReader.Read(visualFsePath));
        visualDseScene=visualDsePath==null?null:await Task.Run(()=>Ps2DseReader.Read(visualDsePath));
        visualViewport.SetSoundScenes(visualEseScene,visualFseScene);WireVisualSoundEvents();
        RefreshVisualSoundEntries();
        if(visualEseScene!=null)ExtractLog($"Visual Editor: ESE loaded: {Path.GetFileName(visualEsePath)} • {visualEseScene.Entries.Count} fontes"+(visualEseScene.IsPlaceholder?" • placeholder vazio.":"."));
        if(visualFseScene!=null)ExtractLog($"Visual Editor: FSE loaded: {Path.GetFileName(visualFsePath)} • {visualFseScene.Entries.Count} zonas"+(visualFseScene.IsPlaceholder?" • placeholder vazio.":"."));
        if(visualDseScene!=null)ExtractLog($"Visual Editor: DSE loaded: {Path.GetFileName(visualDsePath)} • {visualDseScene.Entries.Count} transições • {(visualDseScene.IsBigEndian?"big":"little")}-endian • {visualDseScene.TrailingData.Length} bytes finais preservados.");
    }
    private void ClearVisualSound(){visualEsePath=visualFsePath=visualDsePath=null;visualEseScene=null;visualFseScene=null;visualDseScene=null;visualViewport?.SetSoundScenes(null,null);lstVisualEseEntries?.Items.Clear();lstVisualFseEntries?.Items.Clear();lstVisualDseEntries?.Items.Clear();}
    private void RefreshVisualSoundEntries(object? select=null)
    {
        void Fill(ListBox list,System.Collections.IEnumerable? values){list.BeginUpdate();try{list.Items.Clear();if(values!=null)foreach(object value in values)list.Items.Add(value);}finally{list.EndUpdate();}}
        Fill(lstVisualEseEntries,visualEseScene?.Entries);Fill(lstVisualFseEntries,visualFseScene?.Entries);Fill(lstVisualDseEntries,visualDseScene?.Entries);
        if(select is EseEntry)lstVisualEseEntries.SelectedItem=select;else if(select is FseEntry)lstVisualFseEntries.SelectedItem=select;else if(select is DseEntry)lstVisualDseEntries.SelectedItem=select;
    }
    private void lstVisualEseEntries_SelectedIndexChanged(object? sender,EventArgs e){EseEntry? x=lstVisualEseEntries.SelectedItem as EseEntry;pgVisualProperties.SelectedObject=x;lblVisualPropertiesTitle.Text=x==null?"PROPERTIES • ESE":$"ESE SOURCE • #{x.FileOrder:D3}";visualViewport?.SelectEseEntry(x);}
    private void lstVisualFseEntries_SelectedIndexChanged(object? sender,EventArgs e){FseEntry? x=lstVisualFseEntries.SelectedItem as FseEntry;pgVisualProperties.SelectedObject=x;lblVisualPropertiesTitle.Text=x==null?"PROPERTIES • FSE":$"FSE ZONE • #{x.FileOrder:D3}";visualViewport?.SelectFseEntry(x);}
    private void lstVisualDseEntries_SelectedIndexChanged(object? sender,EventArgs e){DseEntry? x=lstVisualDseEntries.SelectedItem as DseEntry;pgVisualProperties.SelectedObject=x;lblVisualPropertiesTitle.Text=x==null?"PROPRIEDADES • DSE":$"DSE PORTA • #{x.FileOrder:D3}";visualViewport?.SelectEseEntry(null);visualViewport?.SelectFseEntry(null);}
    private void SetFseGizmoMode(FseGizmoMode mode){visualViewport?.SetFseTransformMode(mode);btnVisualFseMove.BackColor=mode==FseGizmoMode.Move?Accent:Surface2;btnVisualFseVertex.BackColor=mode==FseGizmoMode.Vertex?Accent:Surface2;btnVisualFseFace.BackColor=mode==FseGizmoMode.Face?Accent:Surface2;visualViewport?.Focus();}
    private void lstVisualEseEntries_KeyDown(object? s,KeyEventArgs e)=>SoundKey(e,true);
    private void lstVisualFseEntries_KeyDown(object? s,KeyEventArgs e)=>SoundKey(e,false);
    private void lstVisualDseEntries_KeyDown(object? s,KeyEventArgs e){if(e.Control&&e.KeyCode==Keys.D){DuplicateVisualSoundEntry(2);e.Handled=e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){DeleteVisualSoundEntry(2);e.Handled=e.SuppressKeyPress=true;}}
    private void SoundKey(KeyEventArgs e,bool ese){int kind=ese?0:1;if(e.Control&&e.KeyCode==Keys.D){DuplicateVisualSoundEntry(kind);e.Handled=e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){DeleteVisualSoundEntry(kind);e.Handled=e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.F){if(ese)visualViewport?.FocusEse(lstVisualEseEntries.SelectedItem as EseEntry);else visualViewport?.FocusFse(lstVisualFseEntries.SelectedItem as FseEntry);e.Handled=e.SuppressKeyPress=true;}}
    private void AddVisualSoundEntry(int kind)
    {
        if(kind==0&&visualEseScene!=null){var x=new EseEntry{Index=NextSoundIndex(visualEseScene.Entries.Select(e=>e.Index)),Parameter1=0x00050000,Parameter2=0x00010000};visualEseScene.Entries.Add(x);ReindexSound();MarkSoundModified(true);RefreshVisualSoundEntries(x);}
        else if(kind==1&&visualFseScene!=null){var x=new FseEntry{Index=NextSoundIndex16(visualFseScene.Entries.Select(e=>e.Index))};visualFseScene.Entries.Add(x);ReindexSound();MarkSoundModified(false);RefreshVisualSoundEntries(x);}
        else if(kind==2&&visualDseScene!=null){var x=new DseEntry();visualDseScene.Entries.Add(x);ReindexSound();MarkDseModified();RefreshVisualSoundEntries(x);}
    }
    private void DuplicateVisualSoundEntry(int kind)
    {
        if(kind==0&&visualEseScene!=null&&lstVisualEseEntries.SelectedItem is EseEntry a){EseEntry x=a.Clone();x.Index=NextSoundIndex(visualEseScene.Entries.Select(e=>e.Index));x.PositionX+=100f;visualEseScene.Entries.Add(x);ReindexSound();MarkSoundModified(true);RefreshVisualSoundEntries(x);}
        else if(kind==1&&visualFseScene!=null&&lstVisualFseEntries.SelectedItem is FseEntry b){FseEntry x=b.Clone();x.Index=NextSoundIndex16(visualFseScene.Entries.Select(e=>e.Index));x.Corner0X+=100;x.Corner1X+=100;x.Corner2X+=100;x.Corner3X+=100;visualFseScene.Entries.Add(x);ReindexSound();MarkSoundModified(false);RefreshVisualSoundEntries(x);}
        else if(kind==2&&visualDseScene!=null&&lstVisualDseEntries.SelectedItem is DseEntry c){DseEntry x=c.Clone();visualDseScene.Entries.Add(x);ReindexSound();MarkDseModified();RefreshVisualSoundEntries(x);}
    }
    private void DeleteVisualSoundEntry(int kind)
    {
        if(kind==0&&visualEseScene!=null&&lstVisualEseEntries.SelectedItem is EseEntry a){visualEseScene.Entries.Remove(a);MarkSoundModified(true);}
        else if(kind==1&&visualFseScene!=null&&lstVisualFseEntries.SelectedItem is FseEntry b){visualFseScene.Entries.Remove(b);MarkSoundModified(false);}
        else if(kind==2&&visualDseScene!=null&&lstVisualDseEntries.SelectedItem is DseEntry c){visualDseScene.Entries.Remove(c);MarkDseModified();}else return;ReindexSound();RefreshVisualSoundEntries();pgVisualProperties.SelectedObject=null;
    }
    private void ReindexSound(){if(visualEseScene!=null)for(int i=0;i<visualEseScene.Entries.Count;i++)visualEseScene.Entries[i].FileOrder=i;if(visualFseScene!=null)for(int i=0;i<visualFseScene.Entries.Count;i++)visualFseScene.Entries[i].FileOrder=i;if(visualDseScene!=null)for(int i=0;i<visualDseScene.Entries.Count;i++)visualDseScene.Entries[i].FileOrder=i;}
    private static byte NextSoundIndex(IEnumerable<byte> source){HashSet<byte> used=source.ToHashSet();for(int i=0;i<256;i++)if(!used.Contains((byte)i))return(byte)i;throw new InvalidOperationException("ESE sem índice livre.");}
    private static ushort NextSoundIndex16(IEnumerable<ushort> source){HashSet<ushort> used=source.ToHashSet();for(int i=0;i<=ushort.MaxValue;i++)if(!used.Contains((ushort)i))return(ushort)i;throw new InvalidOperationException("FSE sem índice livre.");}
    private void MarkSoundModified(bool ese){if(ese&&visualEseScene!=null)visualEseScene.IsModified=true;if(!ese&&visualFseScene!=null)visualFseScene.IsModified=true;visualViewport?.RefreshSoundGeometry();UpdateVisualStatus();UpdateTopVisualSaveState();}
    private void MarkDseModified(){if(visualDseScene!=null)visualDseScene.IsModified=true;UpdateVisualStatus();UpdateTopVisualSaveState();}
    private bool SaveVisualDse(){try{if(visualDseScene==null||visualDsePath==null)return false;bool backup=Ps2SoundWriter.Save(visualDseScene,GetVisualAevBackupPath(visualDsePath));visualDseScene=Ps2DseReader.Read(visualDsePath);RefreshVisualSoundEntries();ExtractLog($"Visual Editor: DSE salvo e verificado: {Path.GetFileName(visualDsePath)}."+(backup?" • backup original preservado.":""));UpdateVisualStatus();return true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar DSE",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog($"Visual Editor: erro ao salvar DSE: {ex.Message}");return false;}}
    private bool SaveVisualSound(bool ese)
    {
        try
        {
            if(ese){if(visualEseScene==null||visualEsePath==null)return false;bool backup=Ps2SoundWriter.Save(visualEseScene,GetVisualAevBackupPath(visualEsePath));visualEseScene=Ps2EseReader.Read(visualEsePath);visualViewport.SetSoundScenes(visualEseScene,visualFseScene);RefreshVisualSoundEntries();ExtractLog($"Visual Editor: ESE saved and verified: {Path.GetFileName(visualEsePath)}."+(backup?" • original backup preserved.":""));}
            else{if(visualFseScene==null||visualFsePath==null)return false;bool backup=Ps2SoundWriter.Save(visualFseScene,GetVisualAevBackupPath(visualFsePath));visualFseScene=Ps2FseReader.Read(visualFsePath);visualViewport.SetSoundScenes(visualEseScene,visualFseScene);RefreshVisualSoundEntries();ExtractLog($"Visual Editor: FSE saved and verified: {Path.GetFileName(visualFsePath)}."+(backup?" • original backup preserved.":""));}
            UpdateVisualStatus();return true;
        }catch(Exception ex){MessageBox.Show(this,ex.Message,ese?"Save ESE":"Save FSE",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog($"Visual Editor: error saving {(ese?"ESE":"FSE")}: {ex.Message}");return false;}
    }
    private void WireVisualSoundEvents(){visualViewport.EseEntryClicked-=visualViewport_EseClicked;visualViewport.EseEntryClicked+=visualViewport_EseClicked;visualViewport.FseEntryClicked-=visualViewport_FseClicked;visualViewport.FseEntryClicked+=visualViewport_FseClicked;visualViewport.FseEntryEdited-=visualViewport_FseEdited;visualViewport.FseEntryEdited+=visualViewport_FseEdited;}
    private void visualViewport_EseClicked(EseEntry? e){if(e==null)return;tabVisualEntities.SelectedIndex=10;tabVisualSound.SelectedIndex=0;lstVisualEseEntries.SelectedItem=e;}
    private void visualViewport_FseClicked(FseEntry? e){if(e==null)return;tabVisualEntities.SelectedIndex=10;tabVisualSound.SelectedIndex=1;lstVisualFseEntries.SelectedItem=e;}
    private void visualViewport_FseEdited(FseEntry e){MarkSoundModified(false);lstVisualFseEntries.Refresh();if(ReferenceEquals(pgVisualProperties.SelectedObject,e))pgVisualProperties.Refresh();}
}
