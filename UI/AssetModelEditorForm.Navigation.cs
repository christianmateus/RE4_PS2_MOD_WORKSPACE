namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private readonly ComboBox etmNavigation=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=530};
    private readonly Label packageIndicator=new(){AutoSize=false,Height=30,Width=250,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI Semibold",9)};
    private bool refreshingEtmNavigation;
    private sealed record EtmNavigationItem(string FullPath,string Room)
    {
        public override string ToString()=>$"{Room}  •  {Path.GetFileName(FullPath)}";
    }

    private void InitializePackageNavigation(Panel heading)
    {
        packageIndicator.Top=17;packageIndicator.Left=heading.ClientSize.Width-packageIndicator.Width-20;
        packageIndicator.Anchor=AnchorStyles.Top|AnchorStyles.Right;heading.Controls.Add(packageIndicator);
        UpdatePackageIndicator();
        if(!etmOnly)return;
        var row=new TableLayoutPanel{Left=18,Top=78,Height=36,Width=heading.ClientSize.Width-36,ColumnCount=5,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,42));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,108));
        row.Controls.Add(new Label{Text="ETMs EXTRAÍDOS",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(148,157,173)},0,0);
        StyleEditorCombo(etmNavigation);row.Controls.Add(etmNavigation,1,0);
        var previous=B("‹",(_,_)=>StepEtm(-1),36);previous.Dock=DockStyle.Fill;row.Controls.Add(previous,2,0);
        var next=B("›",(_,_)=>StepEtm(1),36);next.Dock=DockStyle.Fill;row.Controls.Add(next,3,0);
        var refresh=B("Atualizar",(_,_)=>RefreshEtmNavigation(),100);refresh.Dock=DockStyle.Fill;row.Controls.Add(refresh,4,0);
        heading.Controls.Add(row);
        etmNavigation.SelectedIndexChanged+=(_,_)=>
        {
            if(refreshingEtmNavigation||etmNavigation.SelectedItem is not EtmNavigationItem selected||SamePackage(selected.FullPath,path))return;
            string target=selected.FullPath;
            if(!ConfirmPackageSwitch()||!LoadPackage(target))SelectCurrentEtm();
        };
        etmNavigation.DropDown+=(_,_)=>RefreshEtmNavigation();
        RefreshEtmNavigation();
    }

    private static bool SamePackage(string? a,string? b)=>a!=null&&b!=null&&Path.GetFullPath(a).Equals(Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);

    private string? FindExtractedDirectory()
    {
        if(!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            string extracted=Path.Combine(workspaceRoot,"Extracted");
            if(Directory.Exists(extracted))return extracted;
        }
        for(var directory=path==null?null:Directory.GetParent(path);directory!=null;directory=directory.Parent)
            if(directory.Name.Equals("Extracted",StringComparison.OrdinalIgnoreCase))return directory.FullName;
        return null;
    }

    private void RefreshEtmNavigation()
    {
        if(!etmOnly)return;
        refreshingEtmNavigation=true;
        try
        {
            var entries=new List<EtmNavigationItem>();
            string? extracted=FindExtractedDirectory();
            if(extracted!=null)
                foreach(string room in Directory.EnumerateDirectories(extracted,"r*",SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
                {
                    string name=Path.GetFileName(room);
                    string content=Path.Combine(room,"Content",name);
                    if(!Directory.Exists(content))continue;
                    foreach(string file in Directory.EnumerateFiles(content,"*.ETM",SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
                        entries.Add(new EtmNavigationItem(Path.GetFullPath(file),name));
                }
            if(path!=null&&Path.GetExtension(path).Equals(".etm",StringComparison.OrdinalIgnoreCase)&&!entries.Any(e=>SamePackage(e.FullPath,path)))
                entries.Add(new EtmNavigationItem(Path.GetFullPath(path),"Avulso"));
            etmNavigation.Items.Clear();etmNavigation.Items.AddRange(entries.Cast<object>().ToArray());
            etmNavigation.SelectedIndex=entries.FindIndex(e=>SamePackage(e.FullPath,path));
            etmNavigation.Enabled=entries.Count>0;
        }
        catch(Exception ex){status.Text="Não foi possível listar os ETMs: "+ex.Message;}
        finally{refreshingEtmNavigation=false;}
    }

    private void SelectCurrentEtm()
    {
        refreshingEtmNavigation=true;
        try{etmNavigation.SelectedIndex=etmNavigation.Items.Cast<EtmNavigationItem>().ToList().FindIndex(e=>SamePackage(e.FullPath,path));}
        finally{refreshingEtmNavigation=false;}
    }

    private void StepEtm(int direction)
    {
        int next=etmNavigation.SelectedIndex+direction;
        if(next>=0&&next<etmNavigation.Items.Count)etmNavigation.SelectedIndex=next;
    }

    private void UpdatePackageIndicator()
    {
        packageIndicator.Text=dirty?$"●  ALTERAÇÕES PENDENTES  •  {edits.Count}":path==null?"○  NENHUM ARQUIVO ABERTO":"●  TODAS AS ALTERAÇÕES SALVAS";
        packageIndicator.BackColor=dirty?Color.FromArgb(79,45,29):Color.FromArgb(25,43,37);
        packageIndicator.ForeColor=dirty?Color.FromArgb(255,190,111):Color.FromArgb(150,211,176);
    }

    private bool ConfirmPackageSwitch()
    {
        if(!dirty)return true;
        var result=MessageBox.Show(this,$"{Path.GetFileName(path)} possui alterações não salvas.\n\nDeseja salvar antes de continuar?\n\nSim: salvar e continuar.\nNão: descartar as alterações.\nCancelar: permanecer neste arquivo.","Salvar alterações",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
        return ResolvePackageSwitch(result);
    }

    private bool ResolvePackageSwitch(DialogResult result)
    {
        if(result==DialogResult.Cancel)return false;
        if(result==DialogResult.No)return true;
        Save(false);
        return !dirty;
    }
}
