using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class SmdTextureEditorForm : AppForm
{
    private readonly TextureWorkspaceService textures=new();
    private readonly ListBox materials=new(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(22,25,30),ForeColor=Color.Gainsboro,BorderStyle=BorderStyle.None,Font=new Font("Consolas",9)};
    private readonly ListView catalog=new(){Dock=DockStyle.Fill,View=View.LargeIcon,MultiSelect=false,BackColor=Color.FromArgb(22,25,30),ForeColor=Color.White,BorderStyle=BorderStyle.None,HideSelection=false};
    private readonly ImageList thumbnails=new(){ImageSize=new Size(96,96),ColorDepth=ColorDepth.Depth32Bit};
    private readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Black};
    private readonly Label info=new(){Dock=DockStyle.Bottom,Height=48,ForeColor=Color.Silver,TextAlign=ContentAlignment.MiddleCenter};
    private readonly Label status=new(){Dock=DockStyle.Bottom,Height=28,ForeColor=Color.FromArgb(255,170,55),Padding=new Padding(8,5,0,0)};
    public byte[] WorkingBin {get;}
    public string WorkingTplPath {get;}
    public SmdTextureEditorForm(byte[] bin,string tplPath,int binId)
    {
        WorkingBin=(byte[])bin.Clone();WorkingTplPath=Path.Combine(Path.GetTempPath(),"re4_smd_texture_"+Guid.NewGuid().ToString("N")+".tpl");File.Copy(tplPath,WorkingTplPath,true);
        Text=$"Texturas locais • BIN {binId:D3}";Width=1040;Height=680;MinimumSize=new Size(820,520);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(18,20,24);ForeColor=Color.White;
        catalog.LargeImageList=thumbnails;var split=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=250,BackColor=Color.FromArgb(42,45,52)};var right=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=480,BackColor=Color.FromArgb(42,45,52)};
        var materialTitle=Title("MATERIAIS DO OBJ MODELO BIN");var textureTitle=Title("TEXTURAS DO CENÁRIO");var previewTitle=Title("PREVIEW");
        split.Panel1.Controls.Add(materials);split.Panel1.Controls.Add(materialTitle);right.Panel1.Controls.Add(catalog);right.Panel1.Controls.Add(textureTitle);right.Panel2.Controls.Add(preview);right.Panel2.Controls.Add(info);right.Panel2.Controls.Add(previewTitle);split.Panel2.Controls.Add(right);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(8),FlowDirection=FlowDirection.RightToLeft,BackColor=Color.FromArgb(29,32,38),WrapContents=false};
        buttons.Controls.Add(Button("SALVAR ALTERAÇÕES",Color.FromArgb(255,122,26),(_,_)=>{DialogResult=DialogResult.OK;Close();},160));buttons.Controls.Add(Button("CANCELAR",Color.FromArgb(52,55,62),(_,_)=>Close(),95));buttons.Controls.Add(Button("USAR TEXTURA",Color.FromArgb(68,86,116),(_,_)=>Assign(),125));buttons.Controls.Add(Button("SEM TEXTURA",Color.FromArgb(65,57,63),(_,_)=>ClearAssignment(),115));buttons.Controls.Add(Button("IMPORTAR TPL...",Color.FromArgb(52,55,62),(_,_)=>ImportTpl(),120));buttons.Controls.Add(Button("ADICIONAR PNG...",Color.FromArgb(52,55,62),(_,_)=>ImportPng(),135));
        Controls.Add(split);Controls.Add(status);Controls.Add(buttons);materials.SelectedIndexChanged+=(_,_)=>UpdateMaterialSelection();catalog.SelectedIndexChanged+=(_,_)=>ShowTexture();catalog.DoubleClick+=(_,_)=>Assign();Shown+=(_,_)=>ReloadAll();
    }
    private static Label Title(string text)=>new(){Text=text,Dock=DockStyle.Top,Height=32,Padding=new Padding(8,8,0,0),ForeColor=Color.FromArgb(174,180,190),BackColor=Color.FromArgb(29,32,38),Font=new Font("Segoe UI Semibold",8)};
    private static Button Button(string text,Color color,EventHandler click,int width){var b=new Button{Text=text,Width=width,Height=34,BackColor=color,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Margin=new Padding(5,0,0,0)};b.FlatAppearance.BorderSize=0;b.Click+=click;return b;}
    private void ReloadAll(int selectTexture=-1)
    {
        int selectedMaterial=Math.Max(0,materials.SelectedIndex);materials.Items.Clear();IReadOnlyList<byte> assigned=SmdEmbeddedBinService.ReadBinMaterialTextures(WorkingBin);for(int i=0;i<assigned.Count;i++)materials.Items.Add($"Material {i:D2}   →   {(assigned[i]==0xFF?"sem textura":$"Textura {assigned[i]:D3}")}");if(materials.Items.Count>0)materials.SelectedIndex=Math.Min(selectedMaterial,materials.Items.Count-1);
        foreach(Image image in thumbnails.Images)image.Dispose();thumbnails.Images.Clear();catalog.Items.Clear();foreach(TextureInfo texture in textures.ReadCatalog(WorkingTplPath)){using Bitmap thumb=textures.CreateThumbnail(WorkingTplPath,texture.Index,96);thumbnails.Images.Add(texture.Index.ToString(),thumb);var row=new ListViewItem($"#{texture.Index:D3}\n{texture.Width}×{texture.Height}",texture.Index.ToString()){Tag=texture};catalog.Items.Add(row);if(texture.Index==selectTexture)row.Selected=true;}status.Text=$"{assigned.Count} materiais • {catalog.Items.Count} texturas disponíveis";
    }
    private void UpdateMaterialSelection(){if(materials.SelectedIndex<0)return;byte assigned=SmdEmbeddedBinService.ReadBinMaterialTextures(WorkingBin)[materials.SelectedIndex];foreach(ListViewItem item in catalog.Items)item.Selected=item.Tag is TextureInfo texture&&texture.Index==assigned;if(assigned==0xFF){catalog.SelectedItems.Clear();preview.Image=null;info.Text="Material sem textura";}}
    private void ShowTexture(){Image? old=preview.Image;preview.Image=null;old?.Dispose();if(catalog.SelectedItems.Count==0||catalog.SelectedItems[0].Tag is not TextureInfo texture){info.Text="Selecione uma textura";return;}preview.Image=textures.Decode(WorkingTplPath,texture.Index);info.Text=$"Textura #{texture.Index:D3} • {texture.Width}×{texture.Height} • {texture.BitDepthName}";}
    private void Assign(){if(materials.SelectedIndex<0||catalog.SelectedItems.Count==0||catalog.SelectedItems[0].Tag is not TextureInfo texture)return;if(texture.Index>255){MessageBox.Show(this,"O formato BIN aceita apenas índices de textura até 255.");return;}SmdEmbeddedBinService.SetBinMaterialTextureAt(WorkingBin,materials.SelectedIndex,(byte)texture.Index);int material=materials.SelectedIndex;ReloadAll(texture.Index);materials.SelectedIndex=material;status.Text=$"Material {material:D2} associado à textura #{texture.Index:D3}";}
    private void ClearAssignment(){if(materials.SelectedIndex<0)return;int material=materials.SelectedIndex;SmdEmbeddedBinService.SetBinMaterialTextureAt(WorkingBin,material,0xFF);ReloadAll();materials.SelectedIndex=material;status.Text=$"Material {material:D2} configurado sem textura";}
    private void ImportPng()
    {
        using var dialog=new OpenFileDialog{Title="Adicionar textura PNG ao cenário",Filter="Imagem PNG (*.png)|*.png|Imagens (*.png;*.bmp;*.jpg;*.jpeg)|*.png;*.bmp;*.jpg;*.jpeg"};if(dialog.ShowLocalizedDialog(this)!=DialogResult.OK)return;
        try{if(new TplReader().ReadTextureCount(WorkingTplPath)>=256)throw new InvalidDataException("O TPL já atingiu o limite de 256 texturas.");int index=textures.AppendFromImage(WorkingTplPath,dialog.FileName);ReloadAll(index);status.Text=$"{Path.GetFileName(dialog.FileName)} adicionada como textura #{index:D3}";}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Adicionar PNG",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void ImportTpl()
    {
        using var dialog=new OpenFileDialog{Title="Importar texturas de outro TPL",Filter="Pacote de texturas PS2 (*.tpl)|*.tpl"};if(dialog.ShowLocalizedDialog(this)!=DialogResult.OK)return;
        string temp=Path.Combine(Path.GetTempPath(),"re4_smd_tpl_import_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);try{var reader=new TplReader();uint count=reader.ReadTextureCount(dialog.FileName),current=reader.ReadTextureCount(WorkingTplPath);if(current+count>256)throw new InvalidDataException($"Não há espaço para {count} texturas. O TPL aceita no máximo 256 e já possui {current}.");int first=-1;for(int i=0;i<count;i++){string png=Path.Combine(temp,$"texture_{i:D3}.png");textures.ExportPng(dialog.FileName,i,png);int added=textures.AppendFromImage(WorkingTplPath,png);if(first<0)first=added;}ReloadAll(first);status.Text=$"{count} textura(s) importada(s) de {Path.GetFileName(dialog.FileName)}";}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Importar TPL",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{try{Directory.Delete(temp,true);}catch{}}
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing){preview.Image?.Dispose();foreach(Image image in thumbnails.Images)image.Dispose();thumbnails.Dispose();DisposeTemp();}base.Dispose(disposing);
    }
    private void DisposeTemp(){try{File.Delete(WorkingTplPath);}catch{}}
}
