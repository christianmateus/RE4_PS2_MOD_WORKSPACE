using RE4_PS2_MOD_WORKSPACE.Core.Collision;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
namespace RE4_PS2_MOD_WORKSPACE;
public partial class Form1
{
    private Action? CancelCatalogCollisionSelection;
    private string? catalogLinkSmdPath;
    private object? catalogLinkSat,catalogLinkEat;
    private List<SmdCatalogCollisionLink>? catalogLinks;
    private List<SmdCatalogCollisionLink> GetSmdCatalogCollisionLinks()
    {if(catalogLinks==null||catalogLinkSmdPath!=visualSmdPath||!ReferenceEquals(catalogLinkSat,visualViewport.SatCollision)||!ReferenceEquals(catalogLinkEat,visualViewport.EatCollision)){catalogLinkSmdPath=visualSmdPath;catalogLinkSat=visualViewport.SatCollision;catalogLinkEat=visualViewport.EatCollision;catalogLinks=SmdCatalogCollisionLinks.Load(visualSmdPath!);}return catalogLinks;}
    private void PersistSmdCatalogCollisionLinks(){if(catalogLinks!=null&&catalogLinkSmdPath==visualSmdPath)SmdCatalogCollisionLinks.Write(SmdCatalogCollisionLinks.PathFor(visualSmdPath!),catalogLinks);}
    private void UpdateSmdCatalogCollision(ScenarioEntry entry)
    {
        if(string.IsNullOrWhiteSpace(visualSmdPath))return;
        try{var links=GetSmdCatalogCollisionLinks();if(!SmdCatalogCollisionLinks.Apply(links,entry,visualViewport.SatCollision,visualViewport.EatCollision))return;visualCollisionModified=(visualViewport.SatCollision?.Meshes.Any(m=>m.IsModified)??false)||(visualViewport.EatCollision?.Meshes.Any(m=>m.IsModified)??false);btnVisualCollisionSave.Enabled=visualCollisionModified;visualViewport.RefreshCollisionDisplay();}
        catch(Exception ex){ExtractLog("Colisão do catálogo: "+ex.Message);lblVisualStatus.Text=ex.Message;}
    }
    private void UnlinkSelectedSmdCatalogCollision()
    {
        if(string.IsNullOrWhiteSpace(visualSmdPath))return;
        try{var orders=lstVisualSmdEntries.SelectedItems.Cast<ScenarioEntry>().Select(e=>e.FileOrder).ToHashSet();var links=GetSmdCatalogCollisionLinks();int count=links.RemoveAll(l=>orders.Contains(l.Entry));var disk=SmdCatalogCollisionLinks.Load(visualSmdPath);disk.RemoveAll(l=>orders.Contains(l.Entry));SmdCatalogCollisionLinks.Write(SmdCatalogCollisionLinks.PathFor(visualSmdPath),disk);lblVisualStatus.Text=$"{count} vínculo(s) removido(s) • colisões preservadas no cenário";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Desvincular colisão",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void RemapSmdCatalogCollisionLinksAfterDeletion(IReadOnlyList<ScenarioEntry> removed,IReadOnlyList<ScenarioEntry> physicallyRemoved)
    {
        if(string.IsNullOrWhiteSpace(visualSmdPath))return;
        var links=GetSmdCatalogCollisionLinks();var remove=removed.Select(e=>e.FileOrder).ToHashSet();int[] shift=physicallyRemoved.Select(e=>e.FileOrder).ToArray();links.RemoveAll(l=>remove.Contains(l.Entry));
        var scene=Ps2ScenarioReader.Read(visualSmdPath);foreach(var link in links){link.Entry-=shift.Count(i=>i<link.Entry);var entry=scene.Entries.FirstOrDefault(e=>e.FileOrder==link.Entry);if(entry!=null)link.BinId=entry.BinId;}PersistSmdCatalogCollisionLinks();
    }
    private async Task<IReadOnlyList<EsatFaceInspection>?> SelectSmdCatalogCollisionAsync(IReadOnlyList<EsatFaceInspection> initial)
    {
        if(visualViewport.SatCollision==null&&visualViewport.EatCollision==null){MessageBox.Show(this,"Este cenário não possui SAT/EAT carregado.","Selecionar colisão",MessageBoxButtons.OK,MessageBoxIcon.Information);return null;}
        var viewport=visualViewport;int tab=tabVisualEntities.SelectedIndex;var camera=viewport.GetCameraState();
        bool tabsEnabled=tabVisualEntities.Enabled,propertiesEnabled=pgVisualProperties.Enabled,actionsEnabled=pnlVisualContextActions.Enabled;
        bool visible=viewport.CollisionVisible,smd=viewport.SmdEditingEnabled,sat=viewport.SatCollisionVisible,eat=viewport.EatCollisionVisible;int sf=viewport.SatCollisionMeshFilter,ef=viewport.EatCollisionMeshFilter;bool floor=viewport.CollisionFloorVisible,slope=viewport.CollisionSlopeVisible,wall=viewport.CollisionWallVisible;
        var disabledControls=new List<(Control Control,bool Enabled)>();Control branch=viewport;while(branch.Parent!=null){foreach(Control sibling in branch.Parent.Controls)if(!ReferenceEquals(sibling,branch)){disabledControls.Add((sibling,sibling.Enabled));}branch=branch.Parent;}
        var done=new TaskCompletionSource<IReadOnlyList<EsatFaceInspection>?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var bar=new AppForm{Text="Associar colisão ao objeto SMD",ClientSize=new Size(680,125),StartPosition=FormStartPosition.Manual,Location=PointToScreen(new Point(30,95)),FormBorderStyle=FormBorderStyle.FixedToolWindow,BackColor=Color.FromArgb(24,28,35),ForeColor=Color.White,ShowInTaskbar=false,Font=new Font("Segoe UI",9)};
        var count=new Label{Left=18,Top=12,Width=640,Height=24,Font=new Font("Segoe UI Semibold",11)};
        var hint=new Label{Left=18,Top=39,Width=640,Height=22,Text="Clique nas faces • Ctrl/Shift adiciona ou remove • botão direito navega"};
        var kind=new ComboBox{Left=18,Top=76,Width=130,DropDownStyle=ComboBoxStyle.DropDownList};kind.Items.AddRange(new object[]{"SAT + EAT","Somente SAT","Somente EAT"});kind.SelectedIndex=0;kind.SelectedIndexChanged+=(_,_)=>viewport.CatalogCollisionKind=kind.SelectedIndex==1?EsatKind.Sat:kind.SelectedIndex==2?EsatKind.Eat:null;
        var clear=new Button{Left=160,Top=73,Width=95,Height=33,Text="LIMPAR"};clear.Click+=(_,_)=>viewport.SetCatalogCollisionFaces(Array.Empty<EsatFaceInspection>());
        var confirm=new Button{Left=365,Top=73,Width=170,Height=33,Text="CONFIRMAR SELEÇÃO",BackColor=Color.FromArgb(188,53,62),FlatStyle=FlatStyle.Flat};
        var cancel=new Button{Left=545,Top=73,Width=115,Height=33,Text="CANCELAR"};cancel.Click+=(_,_)=>done.TrySetResult(null);confirm.Click+=(_,_)=>done.TrySetResult(viewport.CatalogCollisionFaces);bar.FormClosing+=(_,_)=>done.TrySetResult(null);bar.CancelButton=cancel;
        void Update(){var faces=viewport.CatalogCollisionFaces;count.Text=$"COLISÃO DO CATÁLOGO • {faces.Count(f=>f.File.Kind==EsatKind.Sat)} SAT • {faces.Count(f=>f.File.Kind==EsatKind.Eat)} EAT";confirm.Enabled=faces.Count>0;}
        kind.BackColor=Color.FromArgb(36,41,50);kind.ForeColor=Color.White;kind.FlatStyle=FlatStyle.Flat;
        foreach(var button in new[]{clear,confirm,cancel}){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;button.ForeColor=Color.White;if(button!=confirm)button.BackColor=Color.FromArgb(45,51,62);}
        foreach(Control c in new Control[]{count,hint,kind,clear,confirm,cancel})bar.Controls.Add(c);
        try{foreach(var state in disabledControls)state.Control.Enabled=false;CancelCatalogCollisionSelection=()=>done.TrySetResult(null);tabVisualEntities.Enabled=pgVisualProperties.Enabled=pnlVisualContextActions.Enabled=false;viewport.CatalogCollisionPicking=true;viewport.CatalogCollisionKind=null;viewport.SmdEditingEnabled=true;viewport.CollisionVisible=true;viewport.SatCollisionVisible=viewport.EatCollisionVisible=true;viewport.SatCollisionMeshFilter=viewport.EatCollisionMeshFilter=-1;viewport.CollisionFloorVisible=viewport.CollisionSlopeVisible=viewport.CollisionWallVisible=true;viewport.CatalogCollisionSelectionChanged+=Update;viewport.SetCatalogCollisionFaces(initial);bar.Show(this);viewport.Focus();return await done.Task;}
        finally{foreach(var state in disabledControls)if(!state.Control.IsDisposed)state.Control.Enabled=state.Enabled;CancelCatalogCollisionSelection=null;tabVisualEntities.Enabled=tabsEnabled;pgVisualProperties.Enabled=propertiesEnabled;pnlVisualContextActions.Enabled=actionsEnabled;viewport.CatalogCollisionSelectionChanged-=Update;bar.Close();viewport.CatalogCollisionPicking=false;viewport.SetCatalogCollisionFaces(Array.Empty<EsatFaceInspection>());viewport.CatalogCollisionKind=null;viewport.CollisionVisible=visible;viewport.SatCollisionVisible=sat;viewport.EatCollisionVisible=eat;viewport.SatCollisionMeshFilter=sf;viewport.EatCollisionMeshFilter=ef;viewport.CollisionFloorVisible=floor;viewport.CollisionSlopeVisible=slope;viewport.CollisionWallVisible=wall;tabVisualEntities.SelectedIndex=tab;viewport.SmdEditingEnabled=smd;viewport.SetCameraState(camera);viewport.RefreshCollisionDisplay();}
    }
}
