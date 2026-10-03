using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private readonly bool etmOnly;
    private readonly ComboBox partSelector = new() { Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30,34,40), ForeColor = Color.White };
    private int activePartOrder = -1;
    private bool refreshingParts;
    private sealed record PartChoice(int Order, string Name) { public override string ToString() => Name; }

    private void InitializePartSelector(Control parent)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8), BackColor = Color.FromArgb(22,25,30), WrapContents = false };
        bar.Controls.Add(new Label { Text = "BIN / VARIANTE", AutoSize = true, ForeColor = Color.Silver, Margin = new Padding(3,5,12,0) });
        bar.Controls.Add(partSelector);
        parent.Controls.Add(bar);
        partSelector.SelectedIndexChanged += (_,_) =>
        {
            if (refreshingParts || objects.SelectedItem is not ObjectItem item) return;
            activePartOrder = (partSelector.SelectedItem as PartChoice)?.Order ?? -1;
            viewport.SetAssetMeshSelection(null);
            var effective = EffectiveCatalog();
            ShowPreview(effective, item.Definition.Id);
            if (effective.ModelParts.TryGetValue(item.Definition.Id, out var parts))
            {
                var part = parts.FirstOrDefault(p => p.Bin.FileOrder == activePartOrder);
                if (part != null)
                {
                    SelectPreferredTextureResource((part.TextureFallback ?? part.Effect)?.FileOrder ?? -1);
                    status.Text = $"{part.Bin.Name} • {part.Triangles.Count:N0} faces • textura: {(part.TextureFallback ?? part.Effect)?.Name ?? "sem textura"}";
                }
            }
        };
    }

    private void RefreshPartSelector(EtmCatalog source, byte id, bool preserve)
    {
        int previous = preserve ? activePartOrder : -1;
        refreshingParts = true;
        try
        {
            partSelector.Items.Clear();
            if (source.ModelParts.TryGetValue(id, out var parts))
                foreach (var part in parts) partSelector.Items.Add(new PartChoice(part.Bin.FileOrder, part.Bin.Name));
            int selected = partSelector.Items.Cast<PartChoice>().ToList().FindIndex(p => p.Order == previous);
            partSelector.SelectedIndex = partSelector.Items.Count == 0 ? -1 : Math.Max(0, selected);
            activePartOrder = (partSelector.SelectedItem as PartChoice)?.Order ?? -1;
        }
        finally { refreshingParts = false; }
    }

    private EtmCatalog PreviewCatalog(EtmCatalog source, byte id)
    {
        if (!source.ModelParts.TryGetValue(id, out var parts)) return source;
        var part = parts.FirstOrDefault(p => p.Bin.FileOrder == activePartOrder);
        if (part == null) return source;
        return new EtmCatalog { SourcePath = source.SourcePath, Resources = source.Resources, Objects = source.Objects,
            Models = new Dictionary<byte,IReadOnlyList<ScenarioTriangle>> { [id] = part.Triangles },
            ModelParts = new Dictionary<byte,IReadOnlyList<EtmModelPart>> { [id] = new[] { part } } };
    }

    private byte[] PrepareTextureImage(EtmResource resource, int index, string image, byte[]? basis=null)
    {
        resource=resource with { Data=basis??Data(resource), FileOrder=int.MinValue };
        string temp = Path.Combine(Path.GetTempPath(), "etm_texture_" + Guid.NewGuid().ToString("N") + ".tpl");
        try
        {
            if (IsEff(resource)) ExportSingleTpl(resource,index,temp);
            else File.WriteAllBytes(temp,Data(resource));
            new TextureWorkspaceService().ReplaceFromImage(temp,IsEff(resource)?0:index,image);
            if (!IsEff(resource)) return File.ReadAllBytes(temp);
            var current = resource with { Data = Data(resource) };
            return EtmModelReplacement.Prepare(new[] { new EtmReplacementRequest(current,index,temp) })[resource.FileOrder];
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private byte[] PrepareCompanionImages(EtmResource resource, EtmModelPart receiver, string model)
    {
        byte[] result=Data(resource);
        string folder=Path.GetDirectoryName(Path.GetFullPath(model))!;
        string stem=Path.GetFileNameWithoutExtension(model);
        bool imported=false;
        int count=ReadSelectedTextures(resource).Count;
        for(int index=0;index<count;index++)
        {
            string png=Path.Combine(folder,$"{stem}_texture_{index:D3}.png");
            if(!File.Exists(png))continue;
            result=PrepareTextureImage(resource,index,png,result); imported=true;
        }
        if(!imported && File.Exists(Path.ChangeExtension(model,".png")))
        {
            int index=receiver.Triangles.Select(t=>t.TextureIndex).FirstOrDefault(i=>i>=0);
            result=PrepareTextureImage(resource,index,Path.ChangeExtension(model,".png"),result); imported=true;
        }
        if(!imported)throw new InvalidDataException("Não há PNGs associados aos índices de textura deste BIN.");
        return result;
    }

    private void ReplaceTextureImage()
    {
        var resource = TextureResource();
        if (resource == null || textureIndex.SelectedIndex < 0) return;
        using var dialog = new OpenFileDialog { Filter = "Imagem PNG (*.png)|*.png" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            byte[] changed=PrepareTextureImage(resource,CurrentTextureIndex,dialog.FileName);
            RecordAssetUndo();edits[resource.FileOrder] = changed;
            Changed($"Textura de {resource.Name} substituída"); SelectObject(true);
        }
        catch (Exception ex) { MessageBox.Show(this,ex.Message,"Importar textura",MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }

    private void ExportObjectBundle()
    {
        if (objects.SelectedItem is not ObjectItem item) return;
        var effective = EffectiveCatalog();
        if (!effective.ModelParts.TryGetValue(item.Definition.Id,out var parts)) return;
        var part = parts.FirstOrDefault(p => p.Bin.FileOrder == activePartOrder) ?? parts.First();
        using var dialog = new SaveFileDialog { Title = "Exportar BIN selecionado e seus recursos", FileName = Path.GetFileNameWithoutExtension(part.Bin.Name),
            Filter = "BIN + EFF/TPL originais|*.bin|BIN + texturas PNG|*.bin|BIN + texturas TPL|*.bin|OBJ + MTL + PNG|*.obj|SMD + PNG|*.smd", AddExtension = true };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        string temporary = Path.Combine(Path.GetTempPath(), "etm_export_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string bin = Path.Combine(temporary,"model.bin"); File.WriteAllBytes(bin,Data(part.Bin));
            EtmResource? source = part.TextureFallback ?? part.Effect;
            if (source != null && ReadSelectedTextures(source).Count == 0) source = null;
            string? tpl = null;
            if (source != null)
            {
                tpl = Path.Combine(temporary,"model.tpl");
                if (IsTpl(source)) File.WriteAllBytes(tpl,Data(source));
                else
                {
                    var textures = ReadSelectedTextures(source).Select(t => { t.mipmapCount = 0; return t; }).ToArray();
                    new TplWriter(new TplReader()).RebuildFile(tpl,textures);
                }
            }
            string destinationFolder = Path.GetDirectoryName(dialog.FileName)!;
            string folder = Path.Combine(temporary,"output");
            Directory.CreateDirectory(folder);
            string modelOutput = Path.Combine(folder,Path.GetFileName(dialog.FileName));
            string stem = Path.GetFileNameWithoutExtension(dialog.FileName);
            if (dialog.FilterIndex == 4) SmdModelExporter.ExportStandaloneObj(bin,tpl,modelOutput);
            else if (dialog.FilterIndex == 5) SmdModelExporter.ExportStandaloneSmd(bin,tpl,modelOutput);
            else
            {
                File.WriteAllBytes(modelOutput,Data(part.Bin));
                if (dialog.FilterIndex == 1)
                {
                    foreach (var resource in new[] { part.Effect,part.TextureFallback }.OfType<EtmResource>().DistinctBy(r=>r.FileOrder))
                        File.WriteAllBytes(Path.Combine(folder,Path.GetFileName(resource.Name)),Data(resource));
                }
                else if (source != null)
                    for (int index = 0; index < ReadSelectedTextures(source).Count; index++)
                    {
                        string target = Path.Combine(folder,$"{stem}_texture_{index:D3}" + (dialog.FilterIndex==3?".tpl":".png"));
                        if (dialog.FilterIndex==3) ExportSingleTpl(source,index,target);
                        else { using var bitmap=DecodeSelectedTexture(source,index); bitmap.Save(target,System.Drawing.Imaging.ImageFormat.Png); }
                    }
            }
            string[] outputs = Directory.GetFiles(folder);
            if (outputs.Any(file => File.Exists(Path.Combine(destinationFolder,Path.GetFileName(file)))) &&
                MessageBox.Show(this,"A exportação substituirá arquivos na pasta de destino. Continuar?","Exportar objeto",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
            foreach (string file in outputs) File.Copy(file,Path.Combine(destinationFolder,Path.GetFileName(file)),true);
            status.Text = $"{part.Bin.Name} exportado • {dialog.FileName}";
        }
        catch (Exception ex) { MessageBox.Show(this,ex.Message,"Exportar objeto",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        finally { Directory.Delete(temporary,true); }
    }
}
