using RE4_PS2_MOD_WORKSPACE.Core.Patching;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class RelPatchManagerForm : AppForm
{
    static readonly Color Bg=Color.FromArgb(15,17,21),Surface=Color.FromArgb(24,27,33),Surface2=Color.FromArgb(31,35,43),TextColor=Color.FromArgb(235,237,241),Muted=Color.FromArgb(154,162,176),Accent=Color.FromArgb(190,55,57),Success=Color.FromArgb(112,205,155);
    static readonly decimal[] ScaleValues={0.50m,0.75m,1.00m,1.25m,1.50m,2.00m,2.50m,3.00m};
    readonly TextBox isoPath=new();readonly CheckBox ganadoScale=new();readonly ComboBox globalScale=new();readonly Label status=new();readonly Button inject=new();readonly Action<decimal>? scaleChanged;bool initializingScale=true;

    public RelPatchManagerForm(string? suggestedIso,decimal selectedScale=1.00m,Action<decimal>? scaleChanged=null)
    {
        this.scaleChanged=scaleChanged;
        Text="Patches para REL • Resident Evil 4 PS2";BackColor=Bg;ForeColor=TextColor;Font=new("Segoe UI",9F);MinimumSize=new(820,560);Size=new(940,650);StartPosition=FormStartPosition.CenterParent;
        BuildUi();
        int scaleIndex=Array.IndexOf(ScaleValues,selectedScale);globalScale.SelectedIndex=scaleIndex>=0?scaleIndex:2;initializingScale=false;
        if(!string.IsNullOrWhiteSpace(suggestedIso)&&File.Exists(suggestedIso)){isoPath.Text=suggestedIso;status.Text="ISO de Build encontrada. Selecione os patches que deseja injetar.";}
    }

    void BuildUi()
    {
        var head=new Panel{Dock=DockStyle.Top,Height=104,BackColor=Surface,Padding=new(24,18,24,12)};
        head.Controls.Add(new Label{Text="PATCHES PARA REL",Dock=DockStyle.Top,Height=30,ForeColor=TextColor,Font=new("Segoe UI Semibold",15F)});
        head.Controls.Add(new Label{Text="Injeção independente de funcionalidades nos módulos do jogo.",Dock=DockStyle.Bottom,Height=28,ForeColor=Muted});
        var footer=new Panel{Dock=DockStyle.Bottom,Height=70,BackColor=Surface,Padding=new(22,16,22,12)};
        Style(inject,"INJETAR SELECIONADOS",Accent,205);inject.Dock=DockStyle.Right;inject.Click+=async (_,_)=>await InjectAsync();
        status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;status.ForeColor=Muted;status.AutoEllipsis=true;status.Text="Selecione uma ISO de Build para começar.";footer.Controls.Add(inject);footer.Controls.Add(status);
        var body=new Panel{Dock=DockStyle.Fill,Padding=new(24,20,24,20),AutoScroll=true,BackColor=Bg};
        var source=new Panel{Dock=DockStyle.Top,Height=92,BackColor=Surface2};source.Controls.Add(new Label{Text="ISO DE DESTINO",Left=18,Top=12,Width=180,Height=20,ForeColor=Muted,Font=new("Segoe UI Semibold",8F)});
        isoPath.SetBounds(18,39,720,32);isoPath.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;isoPath.BackColor=Surface;isoPath.ForeColor=TextColor;isoPath.BorderStyle=BorderStyle.FixedSingle;isoPath.ReadOnly=true;
        var browse=new Button{Top=38,Left=752,Anchor=AnchorStyles.Top|AnchorStyles.Right};Style(browse,"SELECIONAR",Surface,120);browse.Click+=(_,_)=>BrowseIso();source.Controls.Add(isoPath);source.Controls.Add(browse);
        var title=new Label{Dock=DockStyle.Top,Height=54,Padding=new Padding(2,23,0,0),Text="PATCHES DISPONÍVEIS",ForeColor=Muted,Font=new("Segoe UI Semibold",8.5F)};
        var card=new Panel{Dock=DockStyle.Top,Height=230,BackColor=Surface2};
        ganadoScale.Text="Tamanho individual de Ganados via ESL";ganadoScale.Checked=true;ganadoScale.SetBounds(20,18,610,28);ganadoScale.ForeColor=TextColor;ganadoScale.BackColor=Surface2;ganadoScale.Font=new("Segoe UI Semibold",11F);card.Controls.Add(ganadoScale);
        card.Controls.Add(new Label{Text="em09 e em10–em4F  •  SLES_537.02",Left=42,Top=52,Width=350,Height=22,ForeColor=Color.FromArgb(105,190,255),Font=new("Segoe UI Semibold",8.5F)});
        card.Controls.Add(new Label{Text="Lê o multiplicador no byte livre +0x1C de cada entry do ESL e aplica a escala ao modelo, colisão e hitbox. Suporta spawn normal, inimigos inicialmente inativos e o primeiro Ganado especial da r100.",Left=42,Top=80,Width=790,Height=62,ForeColor=Muted});
        card.Controls.Add(new Label{Text="Entries em Automático preservam o comportamento original.",Left=42,Top=148,Width=790,Height=24,ForeColor=Success});
        card.Controls.Add(new Label{Text="TAMANHO GLOBAL",Left=42,Top=184,Width=145,Height=22,ForeColor=Muted,Font=new("Segoe UI Semibold",8F)});
        globalScale.SetBounds(190,179,180,30);globalScale.DropDownStyle=ComboBoxStyle.DropDownList;globalScale.BackColor=Surface;globalScale.ForeColor=TextColor;globalScale.FlatStyle=FlatStyle.Flat;globalScale.Items.AddRange(new object[]{"0,50×","0,75×","1,00× (original)","1,25×","1,50×","2,00×","2,50×","3,00×"});globalScale.SelectedIndexChanged+=(_,_)=>{if(!initializingScale&&globalScale.SelectedIndex>=0)scaleChanged?.Invoke(ScaleValues[globalScale.SelectedIndex]);};card.Controls.Add(globalScale);
        card.Controls.Add(new Label{Text="Aplicado junto aos tamanhos individuais durante o Build.",Left=384,Top=184,Width=430,Height=22,ForeColor=Muted});
        body.Controls.Add(card);body.Controls.Add(title);body.Controls.Add(source);Controls.Add(body);Controls.Add(footer);Controls.Add(head);
    }

    void BrowseIso(){using var dialog=new OpenFileDialog{Title="Selecione a ISO de Build do Resident Evil 4 PS2",Filter="Imagem de disco (*.iso)|*.iso|Todos os arquivos (*.*)|*.*"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;isoPath.Text=dialog.FileName;status.Text="ISO selecionada. O arquivo será validado antes da alteração.";}

    async Task InjectAsync()
    {
        if(string.IsNullOrWhiteSpace(isoPath.Text)||!File.Exists(isoPath.Text)){MessageBox.Show(this,"Selecione uma ISO válida.","Patches para REL",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        if(!ganadoScale.Checked){MessageBox.Show(this,"Selecione pelo menos um patch.","Patches para REL",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        inject.Enabled=false;status.ForeColor=Muted;status.Text="Validando e injetando patches...";UseWaitCursor=true;
        decimal scale=globalScale.SelectedIndex>=0?ScaleValues[globalScale.SelectedIndex]:1.00m;
        try{GanadoScalePatchResult result=await Task.Run(()=>GanadoScalePatch.Apply(isoPath.Text,(float)scale));status.ForeColor=Success;status.Text=$"Concluído • {result.EntryCount} módulo(s) em09 e em10–em4F, {result.ChangedCount} bloco(s) atualizado(s).";MessageBox.Show(this,$"Patch de tamanho individual dos inimigos em09 e em10–em4F injetado com sucesso.\n\nEscala global: {scale:0.00}×. As escalas individuais serão lidas das entries do emleon00.esl.","Patches para REL",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        catch(Exception ex){status.ForeColor=Color.FromArgb(235,118,118);status.Text="Falha na injeção.";MessageBox.Show(this,ex.Message,"Não foi possível injetar",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{UseWaitCursor=false;inject.Enabled=true;}
    }

    static void Style(Button button,string text,Color color,int width){button.Text=text;button.Width=width;button.Height=34;button.BackColor=color;button.ForeColor=Color.White;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;button.Font=new("Segoe UI Semibold",8.5F);button.Cursor=Cursors.Hand;}
}
