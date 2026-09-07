using RE4_PS2_MOD_WORKSPACE.Core.Messages;
using RE4_PS2_MOD_WORKSPACE.Core.Afs;
using System.Runtime.InteropServices;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private MdtDocument? activeMdt;
    private string? activeMdtPath;
    private MdtMessage? activeMessage;
    private bool loadingMessageUi;
    private bool messagesModified;
    private bool messageFriendlyMode = true;
    private FntFont? activeHudFont;

    private void LoadMessagesForActiveDat(bool force)
    {
        EnsureHudFont();
        string? content = GetActiveContentPath();
        string? path = !string.IsNullOrWhiteSpace(content) && Directory.Exists(content)
            ? Directory.EnumerateFiles(content, "*.MDT", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
            : null;
        if (!force && activeMdt != null && string.Equals(activeMdtPath, path, StringComparison.OrdinalIgnoreCase)) return;
        if (messagesModified && !ConfirmDiscardMessageChanges()) return;

        ResetMessageUi();
        if (path == null)
        {
            lblMessageFile.Text = "MDT • não encontrado no DAT ativo";
            lblMessageStatus.Text = string.IsNullOrWhiteSpace(project.ActiveDatName) ? "Nenhum DAT está selecionado." : $"{project.ActiveDatName} ainda não foi extraído ou não contém MDT.";
            return;
        }
        try
        {
            activeMdt = MdtDocument.Load(path);
            activeMdtPath = path;
            loadingMessageUi = true;
            cmbMessageLanguage.Items.AddRange(activeMdt.Languages.Cast<object>().ToArray());
            if (cmbMessageLanguage.Items.Count > 0) cmbMessageLanguage.SelectedIndex = activeMdt.Languages.Count > 1 ? 1 : 0;
            lblMessageFile.Text = $"MDT • {Path.GetFileName(path)}";
            lblMessageStatus.Text = $"{activeMdt.Languages.Count:N0} idioma(s) • {activeMdt.Languages.Sum(x => x.Messages.Count):N0} entradas";
            btnMessageSave.Enabled = true;
            UpdateMessageSizeInfo();
        }
        catch (Exception ex)
        {
            ResetMessageUi();
            lblMessageFile.Text = "MDT • falha ao carregar";
            lblMessageStatus.Text = ex.Message;
            MessageBox.Show(ex.Message, "Erro ao abrir MDT", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadingMessageUi = false;
            RefreshMessageList();
        }
    }

    private void ResetMessageUi()
    {
        loadingMessageUi = true;
        activeMdt = null; activeMdtPath = null; activeMessage = null; messagesModified = false;
        cmbMessageLanguage.Items.Clear(); lstMessages.Items.Clear(); txtMessageEditor.Clear(); txtMessageEditor.Enabled = false;
        lblMessageSelection.Text = "SELECIONE UMA MENSAGEM"; btnMessageSave.Enabled = false; btnMessageSave.Text = "SALVAR MDT";
        lblMessageValidation.Text = "Aguardando mensagem"; lblMessageValidation.ForeColor = TextMuted; lblMessageSize.Text = "Tamanho: —";
        messageHudPreview.SetMessage("");
        loadingMessageUi = false;
    }

    private void cmbMessageLanguage_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (loadingMessageUi) return;
        CommitActiveMessage();
        RefreshMessageList();
    }

    private void txtMessageSearch_TextChanged(object? sender, EventArgs e)
    {
        if (loadingMessageUi) return;
        CommitActiveMessage();
        RefreshMessageList();
    }

    private void txtMessageSearch_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.Handled = true; e.SuppressKeyPress = true; FindNextMessage();
    }

    private void FindNextMessage()
    {
        if (lstMessages.Items.Count == 0) return;
        int next = lstMessages.SelectedIndex + 1;
        lstMessages.SelectedIndex = next >= lstMessages.Items.Count ? 0 : next;
        txtMessageEditor.Focus();
    }

    private void RefreshMessageList(MdtMessage? preferred = null)
    {
        MdtLanguage? language = cmbMessageLanguage.SelectedItem as MdtLanguage;
        loadingMessageUi = true;
        try
        {
            lstMessages.BeginUpdate(); lstMessages.Items.Clear();
            if (language != null)
            {
                string query = txtMessageSearch.Text.Trim();
                IEnumerable<MdtMessage> messages = language.Messages;
                if (query.Length > 0) messages = messages.Where(x => x.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase) || MdtCodec.ToFriendly(x.Text).Contains(query, StringComparison.CurrentCultureIgnoreCase) || (x.Index + 1).ToString().Contains(query, StringComparison.Ordinal));
                lstMessages.Items.AddRange(messages.Cast<object>().ToArray());
            }
            if (preferred != null && lstMessages.Items.Contains(preferred)) lstMessages.SelectedItem = preferred;
            else if (lstMessages.Items.Count > 0) lstMessages.SelectedIndex = 0;
            else ShowMessage(null);
            lblMessageStatus.Text = language == null ? "Nenhum idioma disponível." : $"{lstMessages.Items.Count:N0} de {language.Messages.Count:N0} mensagens • {language.Name}";
        }
        finally { lstMessages.EndUpdate(); loadingMessageUi = false; }
        if (lstMessages.SelectedItem is MdtMessage selected) ShowMessage(selected);
    }

    private void lstMessages_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (loadingMessageUi) return;
        CommitActiveMessage();
        ShowMessage(lstMessages.SelectedItem as MdtMessage);
    }

    private void ShowMessage(MdtMessage? message)
    {
        loadingMessageUi = true;
        activeMessage = message;
        txtMessageEditor.Text = message == null ? "" : messageFriendlyMode ? MdtCodec.ToFriendly(message.Text) : message.Text;
        txtMessageEditor.Enabled = message != null;
        lblMessageSelection.Text = message == null ? "SELECIONE UMA MENSAGEM" : $"MENSAGEM {message.Index + 1:D3}";
        btnMessagePrevious.Enabled = message != null && lstMessages.SelectedIndex > 0;
        btnMessageNext.Enabled = message != null && lstMessages.SelectedIndex >= 0 && lstMessages.SelectedIndex + 1 < lstMessages.Items.Count;
        loadingMessageUi = false;
        ValidateActiveMessage();
        UpdateHudPreview();
    }

    private void txtMessageEditor_TextChanged(object? sender, EventArgs e)
    {
        if (loadingMessageUi || activeMessage == null) return;
        activeMessage.Text = GetRawEditorText();
        messagesModified = true;
        btnMessageSave.Text = "SALVAR MDT *";
        lstMessages.Refresh();
        ValidateActiveMessage();
        UpdateMessageSizeInfo();
        UpdateHudPreview();
    }

    private void CommitActiveMessage()
    {
        if (loadingMessageUi || activeMessage == null) return;
        activeMessage.Text = GetRawEditorText();
    }

    private string GetRawEditorText() => messageFriendlyMode ? MdtCodec.FromFriendly(txtMessageEditor.Text) : txtMessageEditor.Text;

    private void chkMessageFriendly_CheckedChanged(object? sender, EventArgs e)
    {
        if (loadingMessageUi) return;
        CommitActiveMessage();
        messageFriendlyMode = chkMessageFriendly.Checked;
        ShowMessage(activeMessage);
    }

    private void ValidateActiveMessage()
    {
        if (activeMessage == null) { lblMessageValidation.Text = "Aguardando mensagem"; lblMessageValidation.ForeColor = TextMuted; return; }
        try
        {
            string raw = GetRawEditorText();
            ushort[] encoded = MdtCodec.Encode(raw);
            var warnings = new List<string>();
            if (!raw.Contains("[message-start]", StringComparison.OrdinalIgnoreCase)) warnings.Add("sem início");
            if (!raw.Contains("[message-end]", StringComparison.OrdinalIgnoreCase)) warnings.Add("sem fim");
            if (warnings.Count > 0)
            {
                lblMessageValidation.Text = "⚠ Válida, mas " + string.Join(" e ", warnings);
                lblMessageValidation.ForeColor = Color.FromArgb(230, 178, 82);
            }
            else
            {
                lblMessageValidation.Text = $"✓ Mensagem válida • {encoded.Length:N0} unidades";
                lblMessageValidation.ForeColor = Color.FromArgb(105, 205, 135);
            }
        }
        catch (Exception ex)
        {
            lblMessageValidation.Text = "✕ " + ex.Message;
            lblMessageValidation.ForeColor = Color.FromArgb(225, 100, 100);
        }
    }

    private void EnsureHudFont()
    {
        if (activeHudFont != null) return;
        try
        {
            var roots = new[] { project.RootPath, AppContext.BaseDirectory }.Where(x => !string.IsNullOrWhiteSpace(x) && Directory.Exists(x)).Distinct(StringComparer.OrdinalIgnoreCase);
            string? path = null;
            foreach (string root in roots!)
            {
                string direct = Path.Combine(root, "Extracted", "_AFS", "BIO4DAT", "common_p.fnt");
                if (File.Exists(direct)) { path = direct; break; }
                string extracted = Path.Combine(root, "Extracted");
                if (Directory.Exists(extracted)) path = Directory.EnumerateFiles(extracted, "common_p.fnt", SearchOption.AllDirectories).FirstOrDefault();
                if (path != null) break;
            }
            if (path == null) { messageHudPreview.SetFont(null); return; }
            activeHudFont = FntFont.Load(path);
            messageHudPreview.SetFont(activeHudFont);
        }
        catch (Exception ex)
        {
            messageHudPreview.SetFont(null);
            ExtractLog("Fonte HUD: " + ex.Message);
        }
    }

    private async void btnExtractHudFont_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        string? isoPath = !string.IsNullOrWhiteSpace(project.IsoPath) && File.Exists(project.IsoPath)
            ? project.IsoPath
            : Clean(txtIsoPath.Text);
        if (string.IsNullOrWhiteSpace(isoPath) || !File.Exists(isoPath))
        {
            MessageBox.Show("Selecione uma ISO base válida na tela Projeto primeiro.", "Extrair fonte do HUD", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            messageHudPreview.SetExtractionBusy(true);
            lblMessageStatus.Text = "Procurando common_p.fnt na ISO...";
            ExtractLog("Fonte HUD: procurando common_p.fnt no BIO4DAT.AFS...");

            var result = await Task.Run(() =>
            {
                AfsImage afs = AfsService.OpenDefaultAfsFromIso(isoPath, "BIO4DAT.AFS");
                AfsEntry entry = AfsService.FindFirstValidEntryByName(afs, "common_p.fnt")
                    ?? throw new FileNotFoundException("common_p.fnt não foi encontrado no BIO4DAT.AFS desta ISO.");
                string afsName = Path.GetFileNameWithoutExtension(afs.IsoAfsEntry.Name);
                string destination = Path.Combine(project.RootPath!, "Extracted", "_AFS", afsName, "common_p.fnt");
                AfsService.ExtractEntry(afs, entry, destination);
                return destination;
            });

            activeHudFont?.Dispose();
            activeHudFont = FntFont.Load(result);
            messageHudPreview.SetFont(activeHudFont);
            UpdateHudPreview();
            lblMessageStatus.Text = "Fonte do HUD extraída e carregada.";
            ExtractLog("Fonte HUD extraída da ISO: " + result);
            MessageBox.Show("common_p.fnt foi extraído e a prévia do HUD já está pronta para uso.", "Fonte do HUD", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            messageHudPreview.SetFont(null);
            lblMessageStatus.Text = "Falha ao extrair a fonte: " + ex.Message;
            ExtractLog("Fonte HUD: erro ao extrair da ISO: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro ao extrair fonte do HUD", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            messageHudPreview.SetExtractionBusy(false);
        }
    }

    private void UpdateHudPreview()
    {
        if (activeMessage == null) { messageHudPreview.SetMessage(""); return; }
        try { messageHudPreview.SetMessage(GetRawEditorText()); }
        catch { messageHudPreview.SetMessage(""); }
    }

    private void UpdateHudPageUi()
    {
        if (messageHudPreview == null || lblHudPage == null) return;
        lblHudPage.Text = $"{messageHudPreview.PageIndex + 1} / {messageHudPreview.PageCount}";
        btnHudPagePrevious.Enabled = messageHudPreview.PageIndex > 0;
        btnHudPageNext.Enabled = messageHudPreview.PageIndex + 1 < messageHudPreview.PageCount;
    }

    private void UpdateMessageSizeInfo()
    {
        if (activeMdt == null) { lblMessageSize.Text = "Tamanho: —"; return; }
        try
        {
            long current = activeMdt.CalculateSize();
            long delta = current - activeMdt.OriginalSize;
            string sign = delta > 0 ? "+" : "";
            lblMessageSize.Text = $"Arquivo: {FormatBytes(activeMdt.OriginalSize)} → {FormatBytes(current)}   ({sign}{delta:N0} bytes)";
            lblMessageSize.ForeColor = delta > 0 ? Color.FromArgb(230, 178, 82) : TextMuted;
        }
        catch { lblMessageSize.Text = "Tamanho estimado indisponível enquanto houver erro"; lblMessageSize.ForeColor = Color.FromArgb(225, 100, 100); }
    }

    private void txtMessageEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.Shift && e.KeyCode == Keys.Z)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            SendMessage(txtMessageEditor.Handle, 0x0454, IntPtr.Zero, IntPtr.Zero); // EM_REDO
        }
        else if (e.Control && !e.Shift && e.KeyCode == Keys.Z)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            if (txtMessageEditor.CanUndo) txtMessageEditor.Undo();
        }
        else if (e.Control && e.KeyCode == Keys.F)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            txtMessageSearch.Focus(); txtMessageSearch.SelectAll();
        }
    }

    private void MoveMessageSelection(int delta)
    {
        int index = lstMessages.SelectedIndex + delta;
        if (index >= 0 && index < lstMessages.Items.Count) lstMessages.SelectedIndex = index;
    }

    private void messageCommand_Click(object? sender, EventArgs e)
    {
        if (!txtMessageEditor.Enabled || sender is not Button { Tag: string token }) return;
        txtMessageEditor.SelectedText = messageFriendlyMode ? MdtCodec.ToFriendly(token) : token;
        txtMessageEditor.Focus();
    }

    private void btnMessageReload_Click(object? sender, EventArgs e) => LoadMessagesForActiveDat(true);

    private void btnMessageSave_Click(object? sender, EventArgs e)
    {
        if (activeMdt == null || string.IsNullOrWhiteSpace(activeMdtPath)) return;
        CommitActiveMessage();
        try
        {
            // Validate every entry before touching the source file.
            foreach (MdtMessage message in activeMdt.Languages.SelectMany(x => x.Messages)) MdtCodec.Encode(message.Text);
            string backupRoot = Path.Combine(project.RootPath ?? Path.GetDirectoryName(activeMdtPath)!, ".workspace", "backups", Path.GetFileNameWithoutExtension(project.ActiveDatName ?? "dat"), "Messages");
            Directory.CreateDirectory(backupRoot);
            string backup = Path.Combine(backupRoot, $"{Path.GetFileNameWithoutExtension(activeMdtPath)}_{DateTime.Now:yyyyMMdd_HHmmss}.MDT.bak");
            File.Copy(activeMdtPath, backup, false);
            activeMdt.Save(activeMdtPath);
            messagesModified = false; btnMessageSave.Text = "SALVAR MDT";
            lblMessageStatus.Text = $"Salvo às {DateTime.Now:HH:mm:ss} • backup criado";
            UpdateMessageSizeInfo();
            WriteLog($"MDT salvo: {activeMdtPath}");
            _ = RefreshChangeStatusAsync();
            MessageBox.Show($"Mensagens salvas no Content do DAT ativo.\n\nBackup: {backup}", "MDT salvo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblMessageStatus.Text = "Falha ao salvar: " + ex.Message;
            MessageBox.Show(ex.Message, "Erro ao salvar MDT", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ConfirmDiscardMessageChanges()
    {
        if (!messagesModified) return true;
        return MessageBox.Show("Há alterações de mensagens ainda não salvas. Descartá-las?", "Mensagens não salvas", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
