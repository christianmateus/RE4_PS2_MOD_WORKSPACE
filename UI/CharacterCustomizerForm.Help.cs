namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm
{
    private void AttachHelpTips()
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ATUALIZAR"] = "Atualiza a lista e relê os BINs e TPLs extraídos.",
            ["ABRIR IDX"] = "Abre o IDX da pasta Content para editar os BINs e TPLs extraídos.",
            ["ENQUADRAR"] = "Centraliza a câmera no modelo sem mover o modelo ou a grade.",
            ["TODAS"] = "Mostra todos os BINs deste DAT no viewer.",
            ["NENHUMA"] = "Oculta todos os BINs deste DAT no viewer.",
            ["MOVER"] = "Mostra os eixos coloridos para mover o BIN selecionado. H oculta ou mostra a parte.",
            ["ROTACIONAR"] = "Mostra os anéis coloridos para girar o BIN selecionado em um eixo.",
            ["ISOLAR"] = "Deixa visível somente o BIN selecionado.",
            ["MOSTRAR TODAS"] = "Mostra todas as partes do modelo no viewer.",
            ["OCULTAR TODAS"] = "Oculta todas as partes do modelo no viewer.",
            ["INVERTER"] = "Troca a visibilidade das partes: visíveis ficam ocultas e vice-versa.",
            ["EXCLUIR FACES"] = "Remove do BIN as faces selecionadas no Edit Mode. Pode ser desfeito.",
            ["NOVO"] = "Cria um preset com o nome e o conjunto de partes visíveis agora.",
            ["DUPLICAR"] = "Cria uma cópia do preset selecionado com outro nome.",
            ["SALVAR"] = "Atualiza o preset selecionado com a visibilidade atual. Em visualização livre, cria um novo preset.",
            ["RENOMEAR"] = "Altera o nome do preset selecionado.",
            ["EXCLUIR"] = "Exclui o preset selecionado, sem alterar o DAT.",
            ["VER ORIGINAL"] = "Compara a aparência atual com o backup original do DAT.",
            ["DESFAZER"] = "Desfaz a última edição de BIN, material ou textura.",
            ["REFAZER"] = "Refaz a edição desfeita.",
            ["RESTAURAR"] = "Restaura a textura selecionada a partir do backup original.",
            ["IMPORTAR MODELO..."] = "Importa um modelo externo para o BIN selecionado.",
            ["SUBSTITUIR BIN..."] = "Substitui o BIN selecionado usando o mesh escolhido na biblioteca.",
            ["APLICAR"] = "Aplica os valores escolhidos à parte ou ao material selecionado.",
            ["CENTRALIZAR"] = "Centraliza o mesh selecionado em relação ao modelo original.",
            ["ZERAR"] = "Restaura os campos de transformação para posição e rotação zero e escala um.",
            ["LIMPAR OSSO"] = "Limpa o filtro de diagnóstico de osso do viewport."
        };

        void Attach(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button button)
                {
                    string label = button.Text;
                    if (descriptions.TryGetValue(label, out string? description)) helpTips.SetToolTip(button, description);
                }
                Attach(control);
            }
        }

        Attach(this);
        helpTips.SetToolTip(chkFaceEdit, "Ativa a seleção de faces e mostra o wireframe amarelo. Ctrl+clique seleciona várias faces.");
        helpTips.SetToolTip(chkSkeleton, "Mostra os ossos e destaca o peso do osso selecionado.");
        helpTips.SetToolTip(cmbViewPreset, "Escolha uma visualização salva. Presets mudam apenas os BINs visíveis no viewer.");
        helpTips.SetToolTip(txtPartName, "Dê um nome ao BIN e pressione Enter para salvar.");
        helpTips.SetToolTip(txtTextureName, "Dê um nome à textura e pressione Enter para salvar.");
        helpTips.SetToolTip(chkAnimationTimeline, "Mostra ou oculta os controles de animação para ampliar o visualizador.");
        helpTips.SetToolTip(chkCharacterLockRoot, "Mantém o personagem na origem ao reproduzir o FCV, ignorando o deslocamento do root. A pose dos demais ossos é preservada.");
        helpTips.SetToolTip(chkCharacterTPose, "Mostra a pose original para selecionar e identificar partes. Desmarque para voltar ao frame escolhido.");
        helpTips.SetToolTip(cmbCharacterAnimation, "Escolha um FCV encontrado no DAT selecionado ou nos arquivos extraídos.");
        helpTips.SetToolTip(btnCharacterPlayPause, "Reproduz ou pausa a animação no frame atual.");
        helpTips.SetToolTip(btnCharacterAnimationSave, "Salva o nome da animação no catálogo compartilhado com o Laboratório 3D.");
    }
}


