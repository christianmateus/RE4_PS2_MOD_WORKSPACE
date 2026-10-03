using RE4_PS2_MOD_WORKSPACE.Core.Visual;
namespace RE4_PS2_MOD_WORKSPACE;

internal sealed class EtmReplicationDialog:AppForm
{
    private readonly byte id;private readonly IReadOnlyList<EtmResource> source;private readonly string[] paths;
    private readonly ListView targets=new(){Dock=DockStyle.Fill,View=View.Details,CheckBoxes=true,FullRowSelect=true,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(22,25,31),ForeColor=Color.Gainsboro};
    private readonly Label summary=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(177,188,206)};
    private readonly Button apply=new(){Text="APLICAR SUBSTITUIÇÃO",Width=206,Height=36,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(130,44,54),ForeColor=Color.White,Enabled=false};
    private readonly ProgressBar progress=new(){Dock=DockStyle.Bottom,Height=4,Style=ProgressBarStyle.Marquee,Visible=false};
    private bool loading;

    public EtmReplicationDialog(EtmCatalog catalog,byte id,IReadOnlyList<EtmResource> resources,string[] paths)
    {
        this.id=id;source=resources;this.paths=paths;
        Text="Replicar objeto • RE4 PS2";StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(1050,650);MinimumSize=new Size(900,570);BackColor=Color.FromArgb(14,17,22);ForeColor=Color.Gainsboro;Font=new Font("Segoe UI",9);
        var heading=new Panel{Dock=DockStyle.Top,Height=104,Padding=new Padding(22)};
        heading.Controls.Add(new Label{Text=$"{EtsObjectNames.Get(id)}  /  ID 0x{id:X2}",Left=22,Top=16,Width=950,Height=35,Font=new Font("Segoe UI Semibold",19),ForeColor=Color.White});
        heading.Controls.Add(new Label{Text=$"Origem: {Path.GetFileName(catalog.SourcePath)} • {resources.Count} recursos • inclui as alterações atuais do editor",Left=24,Top=61,Width=980,Height=28,ForeColor=Color.FromArgb(155,167,187)});
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(20,0,20,12)};body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,285));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var files=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(22,25,31),ForeColor=Color.Gainsboro,Font=Font};foreach(var r in resources)files.Items.Add($"{r.Name}  •  {r.Data.Length/1024d:0.#} KB");
        var left=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12),Margin=new Padding(0,0,12,0),BackColor=files.BackColor};left.Controls.Add(files);left.Controls.Add(new Label{Text="CONJUNTO DO OBJETO",Dock=DockStyle.Top,Height=34,ForeColor=Color.White});
        left.Controls.Add(new Label{Text="BINs, texturas e animações associados serão copiados. Dependências compartilhadas também afetam seus outros usuários no ETM.",Dock=DockStyle.Bottom,Height=90,ForeColor=Color.FromArgb(177,158,115)});
        targets.HeaderStyle=ColumnHeaderStyle.None;targets.Columns.Add("Cenário / ETM",230);targets.Columns.Add("Compatibilidade",390);targets.ItemChecked+=(_,e)=>{if(!loading)UpdateSummary();};targets.ItemCheck+=(_,e)=>{if(targets.Items[e.Index].Tag is EtmReplicationTarget t&&!t.Compatible)e.NewValue=CheckState.Unchecked;};
        var right=new Panel{Dock=DockStyle.Fill,BackColor=targets.BackColor};right.Controls.Add(targets);
        var columns=new TableLayoutPanel{Dock=DockStyle.Top,Height=34,ColumnCount=2,BackColor=Color.FromArgb(32,37,46)};columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,230));columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        foreach(string title in new[]{"CENÁRIOS DE DESTINO","COMPATIBILIDADE"})columns.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(10,0,0,0),ForeColor=Color.FromArgb(164,179,200),Font=new Font("Segoe UI Semibold",9)});
        right.Controls.Add(columns);targets.Resize+=(_,_)=>targets.Columns[1].Width=Math.Max(260,targets.ClientSize.Width-230-SystemInformation.VerticalScrollBarWidth-8);
        body.Controls.Add(left,0,0);body.Controls.Add(right,1,0);
        var footer=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=96,ColumnCount=3,Padding=new Padding(20,10,20,10)};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,218));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,115));
        var selectAll=new CheckBox{Text="Selecionar todos os compatíveis",AutoSize=true,ForeColor=Color.White};selectAll.CheckedChanged+=(_,_)=>{foreach(ListViewItem row in targets.Items)if(row.Tag is EtmReplicationTarget t&&t.Compatible)row.Checked=selectAll.Checked;};
        var info=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};info.Controls.Add(selectAll);summary.Height=30;summary.Width=540;info.Controls.Add(summary);
        apply.FlatAppearance.BorderSize=0;apply.Click+=async(_,_)=>await ApplyAsync();var cancel=new Button{Text="FECHAR",Width=100,Height=36,FlatStyle=FlatStyle.Flat,ForeColor=Color.Gainsboro,BackColor=Color.FromArgb(38,43,52)};cancel.FlatAppearance.BorderSize=0;cancel.Click+=(_,_)=>Close();
        footer.Controls.Add(info,0,0);footer.Controls.Add(apply,1,0);footer.Controls.Add(cancel,2,0);
        Controls.Add(body);Controls.Add(footer);Controls.Add(heading);Controls.Add(progress);
        Shown+=async(_,_)=>await ScanAsync();FormClosing+=(_,e)=>{if(loading)e.Cancel=true;};
    }
    private async Task ScanAsync()
    {
        loading=true;progress.Visible=true;summary.Text="Analisando cenários…";
        try
        {
            var results=await Task.Run(()=>paths.Select(p=>EtmObjectReplication.Inspect(p,id,source)).OfType<EtmReplicationTarget>().ToArray());
            foreach(var target in results){var row=new ListViewItem(Path.GetFileName(Path.GetDirectoryName(target.Path))+" / "+Path.GetFileName(target.Path)){Tag=target,ForeColor=target.Compatible?Color.Gainsboro:Color.FromArgb(222,163,104)};row.SubItems.Add(target.Detail);targets.Items.Add(row);}
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Analisar ETMs",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{loading=false;progress.Visible=false;UpdateSummary();}
    }
    private void UpdateSummary(){int selected=targets.CheckedItems.Count,compatible=targets.Items.Cast<ListViewItem>().Count(r=>r.Tag is EtmReplicationTarget t&&t.Compatible);summary.Text=$"{selected} selecionados • {compatible} compatíveis • backups por ETM";apply.Enabled=!loading&&selected>0;}
    private async Task ApplyAsync()
    {
        var selected=targets.CheckedItems.Cast<ListViewItem>().Select(r=>(EtmReplicationTarget)r.Tag!).ToArray();
        if(selected.Length==0)return;
        if(MessageBox.Show(this,$"Substituir o objeto 0x{id:X2} em {selected.Length} ETM(s)?\n\nTodos os recursos listados serão copiados. As posições no cenário permanecem iguais. Serão criados backups antes da gravação.","Replicar objeto",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        loading=true;targets.Enabled=false;apply.Enabled=false;progress.Visible=true;summary.Text="Preparando e validando o lote…";
        try{var backups=await Task.Run(()=>EtmObjectReplication.Apply(id,source,selected));MessageBox.Show(this,$"Objeto replicado em {backups.Count} cenário(s).\n\nBackups .replica_*.bak salvos ao lado de cada ETM.","Replicação concluída",MessageBoxButtons.OK,MessageBoxIcon.Information);loading=false;Close();}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Falha na replicação",MessageBoxButtons.OK,MessageBoxIcon.Error);loading=false;await ScanRefreshAsync();}
        finally{loading=false;targets.Enabled=true;progress.Visible=false;UpdateSummary();}
    }
    private async Task ScanRefreshAsync(){targets.Items.Clear();await ScanAsync();}
}
