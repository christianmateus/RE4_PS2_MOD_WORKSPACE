using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using RE4_PS2_MOD_WORKSPACE.Core.Dat;

using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private FcvAnimation? currentFcv;
    private System.Windows.Forms.Timer? animationPlaybackTimer;
    private int animationPlaybackFrame;
    private EnemyModelScene? animationLabModel;
    private EslEnemyEntry? animationLabEnemy;
    private string? animationLabDatPath;
    private int animationLabWeaponPartBinIndex=-1;
    private Dictionary<string,string> animationNames = new(StringComparer.OrdinalIgnoreCase);
    private string? animationNamesFile;
    private bool refreshingAnimationChoices;
    private bool applyingAnimationLabSettings;

    private void btnNavAnimations_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving(); ShowPage(pnlAnimations, btnNavAnimations, "Animações"); RememberMainPage("Animations"); RefreshAnimationDatFiles(true);
    }

    private void btnAnimationBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Resident Evil 4 FCV (*.fcv)|*.fcv|Todos os arquivos (*.*)|*.*", Title = "Abrir animação FCV" };
        if (dlg.ShowLocalizedDialog(this) == DialogResult.OK) LoadFcv(dlg.FileName);
    }
    private void btnAnimationRefresh_Click(object? sender, EventArgs e){RefreshAnimationDatFiles(false);RefreshAnimationFiles();}
    private void btnAnimationExportSmd_Click(object? sender,EventArgs e)
    {
        if(currentFcv==null||animationLabModel?.Skeleton==null){MessageBox.Show(this,"Abra um FCV e o DAT correspondente antes de exportar.","Exportar para Blender",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        using var dialog=new SaveFileDialog{Title="Exportar animacao para Blender (SMD)",Filter="Source Model Animation (*.smd)|*.smd",FileName=Path.GetFileNameWithoutExtension(currentFcv.FilePath)+"_blender.smd",InitialDirectory=Path.GetDirectoryName(currentFcv.FilePath)};
        if(dialog.ShowLocalizedDialog(this)!=DialogResult.OK)return;try{StopAnimationPlayback();FcvSmdRoundTrip.Export(dialog.FileName,animationLabModel.Skeleton,currentFcv,animationLabModel);string model=Path.GetFileNameWithoutExtension(dialog.FileName)+"_model.smd";lblAnimationStatus.Text=$"SMDs exportados: {model} + {Path.GetFileName(dialog.FileName)}";ExtractLog($"Blender: modelo e animacao SMD exportados junto de {dialog.FileName}");MessageBox.Show(this,$"Dois arquivos foram criados.\n\n1. Importe primeiro {model}.\n2. Depois importe {Path.GetFileName(dialog.FileName)} para aplicar a animacao.\n\nUse 30 FPS e mantenha o .fcv.json ao lado da animacao.","Exportar para Blender",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Exportar para Blender",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void btnAnimationImportSmd_Click(object? sender,EventArgs e)
    {
        if(currentFcv==null||animationLabModel?.Skeleton==null){MessageBox.Show(this,"Abra o FCV original e o DAT correspondente; eles serao usados como molde.","Importar do Blender",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        using var open=new OpenFileDialog{Title="Importar animacao editada do Blender",Filter="Source Model Animation (*.smd)|*.smd",InitialDirectory=Path.GetDirectoryName(currentFcv.FilePath)};if(open.ShowLocalizedDialog(this)!=DialogResult.OK)return;
        try{FcvSmdImportResult imported=FcvSmdRoundTrip.Import(open.FileName,animationLabModel.Skeleton,currentFcv);using var save=new SaveFileDialog{Title="Salvar novo FCV",Filter="Resident Evil 4 FCV (*.fcv)|*.fcv",FileName=Path.GetFileNameWithoutExtension(currentFcv.FilePath)+"_edited.fcv",InitialDirectory=Path.GetDirectoryName(currentFcv.FilePath)};if(save.ShowLocalizedDialog(this)!=DialogResult.OK)return;FcvWriter.Write(save.FileName,imported.Animation);settings.LastAnimationFcvFile=Path.GetFileName(save.FileName);LoadFcv(save.FileName);RefreshAnimationFiles();lblAnimationStatus.Text=$"FCV importado: {imported.RotationTracksUpdated} rotações, {imported.IkTargetsUpdated} alvos IK, {imported.FramesRead} frames.";ExtractLog($"Blender: {open.FileName} convertido para {save.FileName}");MessageBox.Show(this,$"Novo FCV salvo.\n\nFrames: {imported.FramesRead}\nRotações: {imported.RotationTracksUpdated}\nAlvos IK: {imported.IkTargetsUpdated}\nRaiz: {(imported.RootTranslationUpdated?"atualizada":"preservada")}","Importar do Blender",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Importar do Blender",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void cmbAnimationFiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if(refreshingAnimationChoices)return;
        if(cmbAnimationFiles.SelectedItem is AnimationFileItem item){settings.LastAnimationFcvFile=Path.GetFileName(item.Path);if(!restoringSession)SaveSettings();LoadFcv(item);}
    }
    private void cmbAnimationDatFiles_SelectedIndexChanged(object? sender,EventArgs e)
    {
        if(refreshingAnimationChoices)return;
        if(cmbAnimationDatFiles.SelectedItem is AnimationDatItem item&&!string.Equals(animationLabDatPath,item.Path,StringComparison.OrdinalIgnoreCase))LoadAnimationLaboratoryModel(item.Path,true);
    }
    private void gridAnimationTracks_SelectionChanged(object? sender, EventArgs e) => ShowSelectedFcvTrack();
    private void tabAnimationAxis_SelectedIndexChanged(object? sender, EventArgs e){ShowSelectedFcvTrack();settings.AnimationInspectorAxisIndex=tabAnimationAxis.SelectedIndex;if(!applyingAnimationLabSettings&&!restoringSession)SaveSettings();}

    private void RefreshAnimationFiles()
    {
        string? selected = (cmbAnimationFiles.SelectedItem as AnimationFileItem)?.Path;
        refreshingAnimationChoices=true;cmbAnimationFiles.BeginUpdate(); cmbAnimationFiles.Items.Clear();
        if(!string.IsNullOrWhiteSpace(animationLabDatPath))
        {
            string stem=Path.GetFileNameWithoutExtension(animationLabDatPath);
            string datDirectory=Path.GetDirectoryName(animationLabDatPath)!;
            string extractedDirectory=Path.Combine(datDirectory,stem);
            string searchRoot=Directory.Exists(extractedDirectory)?extractedDirectory:datDirectory;
            LoadAnimationNames();
            var embeddedNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DatArchive archive=NativeDatService.Read(animationLabDatPath);
                foreach(DatEntry entry in archive.Entries.Where(x=>x.Type.Equals("FCV",StringComparison.OrdinalIgnoreCase)))
                {
                    string file=$"{stem}_{entry.Index:D2}.FCV";embeddedNames.Add(file);
                    string label=animationNames.TryGetValue(AnimationNameKey(stem,file),out string? name)&&!string.IsNullOrWhiteSpace(name)?$"{file} - {name}":file;
                    cmbAnimationFiles.Items.Add(new AnimationFileItem(Path.Combine(datDirectory,file),label,entry.Data));
                }
            }
            catch { }
            try{foreach(string path in Directory.EnumerateFiles(searchRoot,stem+"_*.fcv",SearchOption.AllDirectories).OrderBy(AnimationFileNumber).ThenBy(Path.GetFileName)){string file=Path.GetFileName(path);if(embeddedNames.Contains(file))continue;string label=animationNames.TryGetValue(AnimationNameKey(stem,file),out string? name)&&!string.IsNullOrWhiteSpace(name)?$"{file} — {name}":file;cmbAnimationFiles.Items.Add(new AnimationFileItem(path,label,null));}}catch{ }
        }
        cmbAnimationFiles.EndUpdate();
        if (cmbAnimationFiles.Items.Count > 0)
        {
            int index = 0;
            for(int i=0;i<cmbAnimationFiles.Items.Count;i++)
            {
                AnimationFileItem? item=cmbAnimationFiles.Items[i] as AnimationFileItem;
                if((selected!=null&&string.Equals(item?.Path,selected,StringComparison.OrdinalIgnoreCase))||(selected==null&&!string.IsNullOrWhiteSpace(settings.LastAnimationFcvFile)&&string.Equals(Path.GetFileName(item?.Path),settings.LastAnimationFcvFile,StringComparison.OrdinalIgnoreCase))){index=i;break;}
            }
            cmbAnimationFiles.SelectedIndex=index; lblAnimationStatus.Text = $"{cmbAnimationFiles.Items.Count} FCV(s) encontrado(s) no workspace.";
        }
        else lblAnimationStatus.Text = animationLabDatPath==null ? "Abra um DAT no Laboratório 3D para listar suas animações." : $"Nenhum FCV de {Path.GetFileName(animationLabDatPath)} foi encontrado.";
        refreshingAnimationChoices=false;
        if(cmbAnimationFiles.SelectedItem is AnimationFileItem chosen&&(currentFcv==null||!string.Equals(Path.GetFileName(currentFcv.FilePath),Path.GetFileName(chosen.Path),StringComparison.OrdinalIgnoreCase)))LoadFcv(chosen);
    }

    private static int AnimationFileNumber(string path){string stem=Path.GetFileNameWithoutExtension(path);int separator=stem.LastIndexOf('_');return separator>=0&&int.TryParse(stem[(separator+1)..],out int number)?number:int.MaxValue;}

    private void LoadFcv(AnimationFileItem item)
    {
        if(item.Data==null)LoadFcv(item.Path);
        else LoadFcv(item.Data,item.Path);
    }

    private void LoadFcv(string path)
    {
        LoadFcv(null,path);
    }

    private void LoadFcv(byte[]? embeddedData,string path)
    {
        try
        {
            StopAnimationPlayback(); currentFcv = embeddedData==null?FcvReader.Read(path):FcvReader.Read(embeddedData,path); animationPlaybackFrame = 0;
            lblAnimationFile.Text = path.Contains('#')?path[(path.IndexOf('#')+1)..]:Path.GetFileName(path);
            lblAnimationSummary.Text = $"Frames: {currentFcv.FrameCount}    Tracks: {currentFcv.TrackCount}    Tamanho: {currentFcv.ActualFileSize:N0} bytes    Header size: 0x{currentFcv.DeclaredFileSize:X}";
            lblAnimationStatus.Text = currentFcv.DeclaredFileSize == currentFcv.ActualFileSize ? "FCV lido com sucesso. Tamanho do header confere com o arquivo." : $"FCV lido. Header declara {currentFcv.DeclaredFileSize:N0} bytes; arquivo possui {currentFcv.ActualFileSize:N0}.";
            gridAnimationTracks.Rows.Clear(); foreach (var t in currentFcv.Tracks) gridAnimationTracks.Rows.Add(t.Index, $"0x{t.NodeId:X2}", $"0x{t.Type:X2}", t.TypeName, $"0x{t.DataType:X2}", $"0x{t.Offset:X8}", t.PhysicalOrder, t.X.Keys.Count, t.Y.Keys.Count, t.Z.Keys.Count);
            if (gridAnimationTracks.Rows.Count > 0) { gridAnimationTracks.ClearSelection(); gridAnimationTracks.Rows[0].Selected = true; gridAnimationTracks.CurrentCell = gridAnimationTracks.Rows[0].Cells[0]; }
            trkAnimationLabFrame.Maximum=Math.Max(1,currentFcv.FrameCount-1);trkAnimationLabFrame.Value=0;UpdateAnimationFrameUi();UpdateAnimationLaboratoryPose();
            UpdateAnimationCatalogEditor();
            if(animationLabModel!=null)lblAnimationLabModel.Text=$"Modelo: {Path.GetFileName(animationLabModel.SourcePath)} • {animationLabModel.Parts.Count} parte(s) • {animationLabModel.Skeleton?.Bones.Count??0} bones • FCV: {Path.GetFileName(currentFcv.FilePath)}";
            ShowSelectedFcvTrack(); ExtractLog($"FCV Inspector: {Path.GetFileName(path)} | {currentFcv.FrameCount} frames | {currentFcv.TrackCount} tracks.");
        }
        catch (Exception ex)
        {
            currentFcv = null; gridAnimationTracks.Rows.Clear(); gridAnimationKeys.Rows.Clear(); lblAnimationStatus.Text = "Erro ao ler FCV: " + ex.Message;
            MessageBox.Show(this, ex.Message, "FCV Inspector", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowSelectedFcvTrack()
    {
        gridAnimationKeys.Rows.Clear(); if (currentFcv == null || gridAnimationTracks.CurrentRow == null) return;
        int index = Convert.ToInt32(gridAnimationTracks.CurrentRow.Cells[0].Value); if (index < 0 || index >= currentFcv.Tracks.Count) return;
        FcvTrack t = currentFcv.Tracks[index]; FcvAxis axis = tabAnimationAxis.SelectedIndex switch { 1 => t.Y, 2 => t.Z, _ => t.X };
        lblAnimationTrackDetail.Text = $"Track #{t.Index}  •  Node 0x{t.NodeId:X2}  •  {t.TypeName}  •  Data 0x{t.DataType:X2}  •  {axis.Keys.Count} key(s)";
        for (int i = 0; i < axis.Keys.Count; i++) { var k = axis.Keys[i]; gridAnimationKeys.Rows.Add(i, k.Frame, FormatFcvNumber(k.Value), FormatFcvNumber(k.TangentIn), FormatFcvNumber(k.TangentOut), FormatFcvNumber(k.Extra)); }
    }

    private void btnAnimationLabOpenDat_Click(object? sender, EventArgs e)
    {
        using var dlg=new OpenFileDialog{Filter="Enemy DAT (*.dat)|*.dat|Todos os arquivos (*.*)|*.*",Title="Abrir modelo para o laboratório"};
        if(currentFcv!=null) dlg.InitialDirectory=Path.GetDirectoryName(currentFcv.FilePath);
        if(dlg.ShowLocalizedDialog(this)==DialogResult.OK){LoadAnimationLaboratoryModel(dlg.FileName,true);RefreshAnimationDatFiles(false);SelectAnimationDat(dlg.FileName);}
    }

    private void RefreshAnimationDatFiles(bool loadSelection)
    {
        string? wanted=animationLabDatPath??settings.LastAnimationDatPath;
        var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(!string.IsNullOrWhiteSpace(project.RootPath)&&Directory.Exists(project.RootPath))
        {
            try
            {
                foreach(string path in Directory.EnumerateFiles(project.RootPath,"*.dat",SearchOption.AllDirectories))
                {
                    string file=Path.GetFileName(path);
                    if(!System.Text.RegularExpressions.Regex.IsMatch(file,@"^(?:em|pl|wep)[0-9a-f]{2}\.dat$",System.Text.RegularExpressions.RegexOptions.IgnoreCase))continue;
                    if(path.Contains($"{Path.DirectorySeparatorChar}OriginalDAT{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase))continue;
                    paths.Add(Path.GetFullPath(path));
                }
            }
            catch(Exception ex){ExtractLog("Laboratório 3D: não foi possível listar todos os DATs extraídos: "+ex.Message);}
        }
        if(!string.IsNullOrWhiteSpace(wanted)&&File.Exists(wanted))paths.Add(Path.GetFullPath(wanted));
        refreshingAnimationChoices=true;cmbAnimationDatFiles.BeginUpdate();cmbAnimationDatFiles.Items.Clear();
        foreach(string path in paths.OrderBy(p=>Path.GetFileName(p),StringComparer.OrdinalIgnoreCase).ThenByDescending(p=>p.Contains($"{Path.DirectorySeparatorChar}Content{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase)))
        {
            string label=Path.GetFileName(path);
            if(!string.IsNullOrWhiteSpace(project.RootPath))
            {
                string relative=Path.GetRelativePath(project.RootPath,path);
                if(!relative.StartsWith(".."))label+=$"  —  {Path.GetDirectoryName(relative)}";
            }
            cmbAnimationDatFiles.Items.Add(new AnimationDatItem(path,label));
        }
        cmbAnimationDatFiles.EndUpdate();
        int selected=-1;
        if(wanted!=null)for(int i=0;i<cmbAnimationDatFiles.Items.Count;i++)if(cmbAnimationDatFiles.Items[i] is AnimationDatItem item&&string.Equals(item.Path,wanted,StringComparison.OrdinalIgnoreCase)){selected=i;break;}
        if(selected<0&&cmbAnimationDatFiles.Items.Count>0)selected=0;
        cmbAnimationDatFiles.SelectedIndex=selected;refreshingAnimationChoices=false;
        if(loadSelection&&selected>=0&&cmbAnimationDatFiles.SelectedItem is AnimationDatItem chosen&&!string.Equals(animationLabDatPath,chosen.Path,StringComparison.OrdinalIgnoreCase))LoadAnimationLaboratoryModel(chosen.Path,false);
    }

    private void SelectAnimationDat(string path){for(int i=0;i<cmbAnimationDatFiles.Items.Count;i++)if(cmbAnimationDatFiles.Items[i] is AnimationDatItem item&&string.Equals(item.Path,path,StringComparison.OrdinalIgnoreCase)){refreshingAnimationChoices=true;cmbAnimationDatFiles.SelectedIndex=i;refreshingAnimationChoices=false;break;}}

    private void LoadAnimationLaboratoryModel(string path,bool showErrors)
    {
        try
        {
            bool weapon=IsWeaponDat(path);byte enemyType=TryEnemyTypeFromDatName(path,out byte parsed)?parsed:(byte)0xFE;
            animationLabWeaponPartBinIndex=-1;
            if(weapon)
            {
                string leonPath=FindLeonDat(path)??throw new FileNotFoundException("Nao encontrei pl00.dat no workspace. Extraia o pacote do Leon antes de abrir a arma no Laboratorio de Animacoes.");
                EnemyModelScene leon=Ps2EnemyDatReader.Read(leonPath,enemyType),weaponModel=Ps2EnemyDatReader.Read(path,enemyType);
                animationLabModel=MergeLeonAndFirstWeaponModel(leon,weaponModel,path,out int weaponPart);animationLabWeaponPartBinIndex=weaponPart;
            }
            else animationLabModel=Ps2EnemyDatReader.Read(path,enemyType);
            if(animationLabModel.Skeleton==null)throw new InvalidDataException("O DAT não contém um BIN com skeleton reconhecível.");
            animationLabEnemy=new EslEnemyEntry{Index=0,Active=1,EnemyType=enemyType,Subtype=0,Health=1};
            animationLabViewport.SetScene(null,false);animationLabViewport.SetEslScene(new EslScene("animation-laboratory",new List<EslEnemyEntry>{animationLabEnemy}));
            animationLabViewport.SetEnemyModels(new Dictionary<byte,EnemyModelScene>{{enemyType,animationLabModel}});
            animationLabViewport.SetEnemyForcedHandHeldPart(enemyType,weapon?animationLabWeaponPartBinIndex:null);
            // Leon's handgun is driven by Hand R (0x0A). 0x10 is Hand L and made the weapon
            // inherit the supporting hand's rotation, visibly crossing the grip.
            if(weapon&&animationLabModel.Skeleton.FirstIndexById.TryGetValue(0x0A,out int handBone))animationLabViewport.SetEnemyAttachmentBone(handBone);
            ConfigureAnimationLaboratoryParts(path,enemyType,animationLabModel);animationLabViewport.SelectEnemyEntry(animationLabEnemy);
            animationLabViewport.SetEnemyBoneDiagnostic(chkAnimationLabSkeleton.Checked,null);UpdateAnimationLaboratoryPose();animationLabViewport.FocusEnemyModel(animationLabEnemy,animationLabModel);
            animationLabDatPath=path;RefreshAnimationFiles();
            settings.LastAnimationDatPath=path;SaveSettings();
            SelectAnimationDat(path);
            lblAnimationLabModel.Text=$"Modelo: {Path.GetFileName(path)} • {animationLabModel.Parts.Count} parte(s) • {animationLabModel.Skeleton.Bones.Count} bones • FCV: {(currentFcv==null?"nenhum":Path.GetFileName(currentFcv.FilePath))}";
            ExtractLog($"Laboratório 3D: {Path.GetFileName(path)} | {animationLabModel.Skeleton.Bones.Count} bones | {animationLabModel.TexturePackages.Count} TPL(s).");
        }
        catch(Exception ex){animationLabModel=null;animationLabEnemy=null;lblAnimationLabModel.Text="Modelo: erro ao carregar "+Path.GetFileName(path);if(showErrors)MessageBox.Show(this,ex.Message,"Laboratório 3D",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }

    private static bool IsWeaponDat(string path)=>System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path),@"^wep[0-9a-f]{2}\.dat$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private string? FindLeonDat(string weaponPath)
    {
        var roots=new[]{Path.GetDirectoryName(weaponPath),project.RootPath,Application.StartupPath}.Where(x=>!string.IsNullOrWhiteSpace(x)&&Directory.Exists(x)).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach(string root in roots!)try
        {
            string? found=Directory.EnumerateFiles(root!,"pl00.dat",SearchOption.AllDirectories).Where(x=>!x.Contains($"{Path.DirectorySeparatorChar}OriginalDAT{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase)).OrderByDescending(x=>x.Contains($"{Path.DirectorySeparatorChar}Content{Path.DirectorySeparatorChar}",StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if(found!=null)return found;
        }
        catch { }
        return null;
    }
    private static EnemyModelScene MergeLeonAndFirstWeaponModel(EnemyModelScene leon,EnemyModelScene weapon,string sourcePath,out int weaponPartBinIndex)
    {
        EnemyModelPart source=weapon.Parts.Where(x=>x.Triangles.Count>0).OrderBy(x=>x.DatEntryIndex).FirstOrDefault()??throw new InvalidDataException("O primeiro modelo BIN da arma nao contem geometria renderizavel.");
        int tplKey=source.TplEntryIndex,mergedTplKey=leon.TexturePackages.Keys.DefaultIfEmpty(-1).Max()+1;
        var triangles=source.Triangles.Select(x=>x with { TplEntryIndex=tplKey>=0?mergedTplKey:-1 }).ToArray();
        weaponPartBinIndex=leon.Parts.Select(x=>x.BinIndex).DefaultIfEmpty(-1).Max()+1;
        var added=new EnemyModelPart{BinIndex=weaponPartBinIndex,DatEntryIndex=source.DatEntryIndex,TplEntryIndex=tplKey>=0?mergedTplKey:-1,TplResolution=source.TplResolution,DiffuseMaps=source.DiffuseMaps,Triangles=triangles,BoundsMin=source.BoundsMin,BoundsMax=source.BoundsMax};
        var packages=leon.TexturePackages.ToDictionary(x=>x.Key,x=>x.Value);
        if(tplKey>=0&&weapon.TexturePackages.TryGetValue(tplKey,out EnemyTexturePackage? package))packages[mergedTplKey]=new EnemyTexturePackage{DatEntryIndex=mergedTplKey,Data=package.Data};
        return new EnemyModelScene{Skeleton=leon.Skeleton,SkeletonSourceDatEntryIndex=leon.SkeletonSourceDatEntryIndex,EnemyType=leon.EnemyType,SourcePath=sourcePath,DatEntryCount=leon.DatEntryCount+weapon.DatEntryCount,BinCount=leon.BinCount+1,LoadedBinCount=leon.LoadedBinCount+1,Warnings=leon.Warnings.Concat(weapon.Warnings).ToArray(),Parts=leon.Parts.Append(added).ToArray(),TexturePackages=packages,Triangles=leon.Triangles.Concat(triangles).ToArray(),BoundsMin=System.Numerics.Vector3.Min(leon.BoundsMin,source.BoundsMin),BoundsMax=System.Numerics.Vector3.Max(leon.BoundsMax,source.BoundsMax)};
    }

    private static bool TryEnemyTypeFromDatName(string path,out byte enemyType){enemyType=0;string stem=Path.GetFileNameWithoutExtension(path);return stem.Length>=4&&stem.StartsWith("em",StringComparison.OrdinalIgnoreCase)&&byte.TryParse(stem.AsSpan(2,2),System.Globalization.NumberStyles.HexNumber,null,out enemyType);}
    private string AnimationNameKey(string datStem,string fcvFile)=>$"{datStem.ToLowerInvariant()}/{fcvFile.ToLowerInvariant()}";
    private string? GetAnimationNamesFile(){string? root=!string.IsNullOrWhiteSpace(project.RootPath)?project.RootPath:animationLabDatPath is string dat?Path.GetDirectoryName(dat):null;return string.IsNullOrWhiteSpace(root)?null:Path.Combine(root,".re4-animation-names.json");}
    private void LoadAnimationNames(){string? file=GetAnimationNamesFile();if(string.Equals(file,animationNamesFile,StringComparison.OrdinalIgnoreCase))return;animationNamesFile=file;animationNames=new(StringComparer.OrdinalIgnoreCase);try{if(file!=null&&File.Exists(file)){var loaded=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(file));if(loaded!=null)animationNames=new(loaded,StringComparer.OrdinalIgnoreCase);}}catch{ }if(!animationNames.ContainsKey("em22/em22_04.fcv"))animationNames["em22/em22_04.fcv"]="Caminhando";if(!animationNames.ContainsKey("em22/em22_16.fcv"))animationNames["em22/em22_16.fcv"]="Levantando do chão";}
    private void UpdateAnimationCatalogEditor(){if(txtAnimationCatalogName==null)return;LoadAnimationNames();string? dat=animationLabDatPath==null?null:Path.GetFileNameWithoutExtension(animationLabDatPath);string? fcv=currentFcv==null?null:Path.GetFileName(currentFcv.FilePath);txtAnimationCatalogName.Text=dat!=null&&fcv!=null&&animationNames.TryGetValue(AnimationNameKey(dat,fcv),out string? name)?name:"";btnAnimationCatalogSave.Enabled=dat!=null&&fcv!=null;}
    private void btnAnimationCatalogSave_Click(object? sender,EventArgs e){if(animationLabDatPath==null||currentFcv==null)return;LoadAnimationNames();string key=AnimationNameKey(Path.GetFileNameWithoutExtension(animationLabDatPath),Path.GetFileName(currentFcv.FilePath));string name=txtAnimationCatalogName.Text.Trim();if(name.Length==0)animationNames.Remove(key);else animationNames[key]=name;string? file=GetAnimationNamesFile();if(file!=null){Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file,System.Text.Json.JsonSerializer.Serialize(animationNames,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));}RefreshAnimationFiles();lblAnimationStatus.Text=name.Length==0?"Nome removido do catálogo.":$"Nome salvo: {name}";}
    private void txtAnimationCatalogName_KeyDown(object? sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){btnAnimationCatalogSave_Click(sender,EventArgs.Empty);e.SuppressKeyPress=true;}}
    private void btnAnimationLabFit_Click(object? sender,EventArgs e){if(animationLabEnemy!=null&&animationLabModel!=null)animationLabViewport.FocusEnemyModel(animationLabEnemy,animationLabModel);}
    private void trkAnimationLabBackground_Scroll(object? sender,EventArgs e){if(animationLabViewport!=null)animationLabViewport.BackgroundBrightness=trkAnimationLabBackground.Value;settings.AnimationLabBackgroundBrightness=trkAnimationLabBackground.Value;if(!applyingAnimationLabSettings&&!restoringSession)SaveSettings();}
    private void ConfigureAnimationLaboratoryParts(string path,byte enemyType,EnemyModelScene model)
    {
        if(!Path.GetFileNameWithoutExtension(path).Equals("pl00",StringComparison.OrdinalIgnoreCase)&&!IsWeaponDat(path)){animationLabViewport.UseAutomaticEnemyModelParts(enemyType);return;}
        bool weapon=IsWeaponDat(path);
        // BIN 7 is Leon's knife. Keep it for pl00 inspection, but never layer it over a wepXX
        // preview where it obscures the weapon grip.
        var visible=new HashSet<int>{0,1,2,3,4,5};
        if(!weapon)visible.Add(7);
        EnemyModelPart[] hands=model.Parts.Where(p=>p.BinIndex is >=10 and <=17).OrderBy(p=>p.BinIndex).ToArray();
        EnemyModelPart? left=hands.FirstOrDefault(p=>p.BoundsMax.X<0),right=hands.FirstOrDefault(p=>p.BoundsMin.X>0);
        if(left!=null)visible.Add(left.BinIndex);if(right!=null)visible.Add(right.BinIndex);
        if(left==null||right==null)foreach(EnemyModelPart hand in hands.Take(2))visible.Add(hand.BinIndex);
        if(animationLabWeaponPartBinIndex>=0)visible.Add(animationLabWeaponPartBinIndex);
        animationLabViewport.ShowAllEnemyModelParts(enemyType);
        foreach(EnemyModelPart part in model.Parts)animationLabViewport.SetEnemyModelPartVisible(enemyType,part.BinIndex,visible.Contains(part.BinIndex));
    }
    private void chkAnimationLabSkeleton_CheckedChanged(object? sender,EventArgs e){animationLabViewport.SetEnemyBoneDiagnostic(chkAnimationLabSkeleton.Checked,null);settings.AnimationLabShowSkeleton=chkAnimationLabSkeleton.Checked;if(!applyingAnimationLabSettings&&!restoringSession)SaveSettings();}
    private void chkAnimationLabRestPose_CheckedChanged(object? sender,EventArgs e){StopAnimationPlayback();UpdateAnimationLaboratoryPose();settings.AnimationLabRestPose=chkAnimationLabRestPose.Checked;if(!applyingAnimationLabSettings&&!restoringSession)SaveSettings();}
    private void chkAnimationLabIgnoreRootMotion_CheckedChanged(object? sender,EventArgs e)
    {
        if(animationLabViewport==null)return;
        animationLabViewport.SetEnemyAnimationIgnoreRootMotion(chkAnimationLabIgnoreRootMotion.Checked);
        UpdateAnimationLaboratoryPose();
        settings.AnimationLabIgnoreRootMotion=chkAnimationLabIgnoreRootMotion.Checked;if(!applyingAnimationLabSettings&&!restoringSession)SaveSettings();
    }

    private void ApplyAnimationLabSettings()
    {
        applyingAnimationLabSettings=true;
        chkAnimationLabSkeleton.Checked=settings.AnimationLabShowSkeleton;
        chkAnimationLabRestPose.Checked=settings.AnimationLabRestPose;
        chkAnimationLabIgnoreRootMotion.Checked=settings.AnimationLabIgnoreRootMotion;
        trkAnimationLabBackground.Value=Math.Clamp(settings.AnimationLabBackgroundBrightness,trkAnimationLabBackground.Minimum,trkAnimationLabBackground.Maximum);
        animationLabViewport.BackgroundBrightness=trkAnimationLabBackground.Value;
        tabAnimationAxis.SelectedIndex=Math.Clamp(settings.AnimationInspectorAxisIndex,0,tabAnimationAxis.TabCount-1);
        tabAnimationView.SelectedIndex=Math.Clamp(settings.AnimationViewTabIndex,0,tabAnimationView.TabCount-1);
        applyingAnimationLabSettings=false;
    }

    private void btnAnimationPlay_Click(object? sender, EventArgs e)
    {
        if(currentFcv==null||animationLabModel?.Skeleton==null){MessageBox.Show(this,"Abra um DAT no laboratório e selecione um FCV.","Laboratório 3D",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        // Primeiro teste de playback: sempre inicia do frame atual e faz loop a ~30 FPS.
        // Se já chegou ao final, volta ao frame zero para o PLAY ser imediatamente perceptível.
        if(animationPlaybackFrame>=currentFcv.FrameCount-1)SetAnimationPlaybackFrame(0);
        UpdateAnimationLaboratoryPose();animationPlaybackTimer??=CreateAnimationPlaybackTimer();animationPlaybackTimer.Start();btnAnimationLabPlay.Text="PLAYING";
    }
    private System.Windows.Forms.Timer CreateAnimationPlaybackTimer()
    {
        var timer=new System.Windows.Forms.Timer{Interval=33};timer.Tick+=(_,_)=>{if(currentFcv==null)return;animationPlaybackFrame++;if(animationPlaybackFrame>=currentFcv.FrameCount)animationPlaybackFrame=0;trkAnimationLabFrame.Value=Math.Min(trkAnimationLabFrame.Maximum,animationPlaybackFrame);UpdateAnimationLaboratoryPose();UpdateAnimationFrameUi();};return timer;
    }
    private void btnAnimationStop_Click(object? sender, EventArgs e) => StopAnimationPlayback();
    private void StopAnimationPlayback(){animationPlaybackTimer?.Stop();if(btnAnimationLabPlay!=null)btnAnimationLabPlay.Text="PLAY";}
    private void trkAnimationLabFrame_Scroll(object? sender,EventArgs e){StopAnimationPlayback();SetAnimationPlaybackFrame(trkAnimationLabFrame.Value);}
    private void SetAnimationPlaybackFrame(int frame){animationPlaybackFrame=frame;trkAnimationLabFrame.Value=Math.Min(trkAnimationLabFrame.Maximum,frame);UpdateAnimationLaboratoryPose();UpdateAnimationFrameUi();}
    private void UpdateAnimationLaboratoryPose(){if(animationLabViewport==null)return;animationLabViewport.SetEnemyAnimationIgnoreRootMotion(chkAnimationLabIgnoreRootMotion?.Checked==true);animationLabViewport.SetEnemyAttachmentAnimation(chkAnimationLabRestPose?.Checked==true?null:currentFcv,animationPlaybackFrame,true);}
    private void UpdateAnimationFrameUi(){if(lblAnimationLabFrame!=null)lblAnimationLabFrame.Text=currentFcv==null?"Frame 0 / —":$"Frame {animationPlaybackFrame} / {Math.Max(0,currentFcv.FrameCount-1)}";}
    private static string FormatFcvNumber(double value) => Math.Abs(value % 1) < 0.0000001 ? value.ToString("0") : value.ToString("0.######");
    private sealed record AnimationFileItem(string Path,string Label,byte[]? Data) { public override string ToString() => Label; }
    private sealed record AnimationDatItem(string Path,string Label) { public override string ToString()=>Label; }
}
