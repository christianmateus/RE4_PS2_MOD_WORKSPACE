using RE4_PS2_MOD_WORKSPACE.Core.Visual;
namespace RE4_PS2_MOD_WORKSPACE;
public sealed partial class CharacterCustomizerForm
{
    private async Task ExportCharacterPartsSmdAsync()
    {
        if(datPath==null)return;
        try{
            var actual=Ps2EnemyDatReader.Read(datPath,PreviewType);using var selection=new CharacterSmdExportDialog(actual.Parts,visibleBins,characterAnimation!=null,partAnnotations.ToDictionary(p=>p.Key,p=>p.Value.Name));if(selection.ShowLocalizedDialog(this)!=DialogResult.OK)return;
            using var folder=new FolderBrowserDialog{Description="Escolha onde criar a pasta de exportação das partes SMD",UseDescriptionForTitle=true};if(folder.ShowLocalizedDialog(this)!=DialogResult.OK)return;
            PauseCharacterPlayback();string output=Path.Combine(folder.SelectedPath,Path.GetFileNameWithoutExtension(datPath)+"_SMD_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")[..4]);Enabled=false;UseWaitCursor=true;
            var progress=new Progress<string>(message=>status.Text=message);var selected=selection.SelectedParts;var clip=selection.IncludeAnimation?characterAnimation:null;var assignments=manualAssignments.ToDictionary(p=>p.Key,p=>p.Value.TplEntry);await Task.Run(()=>CharacterSmdRoundTrip.ExportAsync(datPath,selected,output,clip,progress,assignments));
            status.Text=$"{selection.SelectedParts.Count} parte(s) exportada(s) • {output}";
            MessageBox.Show(this,$"Exportação concluída.\n\n{output}\n\nImporte os arquivos _model.smd no Blender. A animação fica em _animation.smd.\nApós editar, exporte Reference SMD com o mesmo nome e use REIMPORTAR SMD. Mantenha .character.json e .idxmaterial ao lado do arquivo.","Exportar partes SMD",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }catch(Exception ex){MessageBox.Show(this,ex.Message,"Exportar partes SMD",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{Enabled=true;UseWaitCursor=false;}
    }
    private async Task ReimportCharacterPartsSmdAsync()
    {
        if(datPath==null)return;using var dialog=new OpenFileDialog{Title="Reimportar partes editadas no Blender",Filter="SMD de modelo (*.smd)|*.smd",Multiselect=true};if(dialog.ShowLocalizedDialog(this)!=DialogResult.OK)return;await ReimportCharacterPartsSmdAsync(dialog.FileNames);
    }
    private async Task ReimportCharacterPartsSmdAsync(IReadOnlyList<string> files)
    {
        if(datPath==null)return;
        try{
            // Validate the exported identity before compiling or changing the DAT.
            foreach(string file in files)CharacterSmdRoundTrip.ReadMetadata(file);
            PauseCharacterPlayback();Enabled=false;UseWaitCursor=true;
            var progress=new Progress<string>(message=>status.Text=message);var changes=await Task.Run(()=>CharacterSmdRoundTrip.ImportAsync(datPath,files,progress));
            foreach(var change in changes)PushTextureEdit(new TextureEdit(datPath,change.DatEntry,-1,change.Before,change.After,$"SMD do Blender → BIN {change.BinIndex:D2}",true));
            showingOriginal=false;LoadDat(datPath);if(changes.Count>0)SelectPartByBinIndex(changes[0].BinIndex);status.Text=$"{changes.Count} parte(s) reimportada(s) • posição, escala, pesos e rig preservados • Ctrl+Z desfaz";
        }catch(Exception ex){MessageBox.Show(this,ex.Message,"Reimportar partes SMD",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{Enabled=true;UseWaitCursor=false;}
    }
}
