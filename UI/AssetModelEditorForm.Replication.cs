using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private void ImportTextureFile()
    {
        var target=TextureResource();if(target==null||textureIndex.SelectedIndex<0)return;
        using var dialog=new OpenFileDialog{Title="Importar textura para "+target.Name,Filter="Texturas PNG ou TPL|*.png;*.tpl|PNG|*.png|TPL|*.tpl"};
        if(dialog.ShowLocalizedDialog(this)!=DialogResult.OK)return;
        int selectedIndex=CurrentTextureIndex;
        try
        {
            byte[] data;
            if(Path.GetExtension(dialog.FileName).Equals(".png",StringComparison.OrdinalIgnoreCase))data=PrepareTextureImage(target,selectedIndex,dialog.FileName);
            else
            {
                var reader=new TplReader();int count=checked((int)reader.ReadTextureCount(dialog.FileName));
                if(count==0)throw new InvalidDataException("O TPL não contém texturas.");
                int sourceIndex=0;
                if(count>1)
                {
                    using var picker=new AppForm{Text="Escolher textura do TPL",StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(390,145),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=BackColor,ForeColor=ForeColor,Font=Font};
                    var select=new ComboBox{Left=18,Top=25,Width=354,DropDownStyle=ComboBoxStyle.DropDownList};StyleEditorCombo(select);
                    for(int i=0;i<count;i++){var info=reader.ReadTexture(dialog.FileName,i);select.Items.Add($"#{i:D2} • {info.width} × {info.height}");}select.SelectedIndex=Math.Min(selectedIndex,count-1);
                    var ok=B("IMPORTAR",(_,_)=>picker.DialogResult=DialogResult.OK,115);ok.Location=new Point(257,88);picker.Controls.Add(select);picker.Controls.Add(ok);picker.AcceptButton=ok;
                    if(picker.ShowLocalizedDialog(this)!=DialogResult.OK)return;sourceIndex=select.SelectedIndex;
                }
                if(IsEff(target))data=EtmModelReplacement.Prepare(new[]{new EtmReplacementRequest(target with{Data=Data(target)},selectedIndex,dialog.FileName,sourceIndex)})[target.FileOrder];
                else
                {
                    var textures=ReadSelectedTextures(target).ToArray();textures[selectedIndex]=reader.ReadTexture(dialog.FileName,sourceIndex);
                    string temp=Path.Combine(Path.GetTempPath(),"tpl_import_"+Guid.NewGuid().ToString("N")+".tpl");
                    try{new TplWriter(reader).RebuildFile(temp,textures);data=File.ReadAllBytes(temp);}finally{if(File.Exists(temp))File.Delete(temp);}
                }
            }
            RecordAssetUndo();edits[target.FileOrder]=data;Changed("Textura importada • "+Path.GetFileName(dialog.FileName));SelectObject(true);SelectPreferredTextureResource(target.FileOrder);textureIndex.SelectedIndex=Math.Min(selectedIndex,textureIndex.Items.Count-1);
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Importar textura",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }

    private void ReplicateObject()
    {
        if(objects.SelectedItem is not ObjectItem item||path==null||!Path.GetExtension(path).Equals(".etm",StringComparison.OrdinalIgnoreCase))return;
        RefreshEtmNavigation();
        var effective=EffectiveCatalog();var source=new EtmCatalog{SourcePath=path,Resources=effective.Resources,Objects=effective.Objects,Models=effective.Models,ModelParts=effective.ModelParts};var resources=EtmObjectDependencies.Resources(source,item.Definition.Id);
        var files=etmNavigation.Items.Cast<EtmNavigationItem>().Where(x=>!SamePackage(x.FullPath,path)).Select(x=>x.FullPath).ToArray();
        using var dialog=new EtmReplicationDialog(source,item.Definition.Id,resources,files);dialog.ShowLocalizedDialog(this);
    }
}
