using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private void ApplySelectedMeshTransform()
    {
        if (animationTimer.Enabled) return;
        if (datPath == null || selectedPart == null) { MessageBox.Show(this, "Selecione uma parte do modelo primeiro."); return; }
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            var translation = new Vector3((float)moveX.Value, (float)moveY.Value, (float)moveZ.Value);
            var rotation = new Vector3((float)rotateX.Value, (float)rotateY.Value, (float)rotateZ.Value);
            var scale = new Vector3((float)scaleX.Value, (float)scaleY.Value, (float)scaleZ.Value);
            if (chkFaceEdit.Checked && selectedFaceFlags.Count > 0)
            {
                if (selectedFaceBin != binIndex) throw new InvalidOperationException("As faces selecionadas pertencem a outro BIN.");
                Ps2CharacterDatEditor.TransformBinVertices(datPath, entry, selectedFaceVertices, translation, rotation, scale);
            }
            else Ps2CharacterDatEditor.TransformBinEntry(datPath, entry, translation, rotation, scale);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Transformação do BIN {binIndex:D2}", true));
            ResetTransformControls(); showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = chkFaceEdit.Checked && selectedFaceFlags.Count > 0 ? $"{selectedFaceFlags.Count} face(s) transformada(s) no BIN {binIndex:D2}." : $"Transformação gravada no BIN {binIndex:D2}. Use Desfazer para reverter.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Transformar mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void CenterSelectedMesh()
    {
        if (animationTimer.Enabled) return;
        if (datPath == null || selectedPart == null) { MessageBox.Show(this, "Selecione uma parte do modelo primeiro."); return; }
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.CenterBinOnOriginal(datPath, entry);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Centralização do BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} centralizado no encaixe original.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Centralizar mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ResetTransformControls()
    {
        moveX.Value = moveY.Value = moveZ.Value = rotateX.Value = rotateY.Value = rotateZ.Value = 0;
        scaleX.Value = scaleY.Value = scaleZ.Value = 1;
    }

    private void ToggleFaceEditMode()
    {
        if (animationTimer.Enabled && chkFaceEdit.Checked) { chkFaceEdit.Checked = false; return; }
        if (chkFaceEdit.Checked) SetMeshGizmoMode(EnemyMeshGizmoMode.Move);
        btnRotateMesh.Enabled = !chkFaceEdit.Checked;
        viewport.EnemyModelFacePickingEnabled = chkFaceEdit.Checked;
        viewport.EnemyModelEditWireframeVisible = chkFaceEdit.Checked;
        viewport.EnemyModelPartPickingEnabled = !chkFaceEdit.Checked;
        viewport.Invalidate();
        if (!chkFaceEdit.Checked) ClearFaceSelection();
        btnDeleteFaces.Enabled = chkFaceEdit.Checked && selectedFaceFlags.Count > 0;
        status.Text = chkFaceEdit.Checked ? "Modo Faces: clique para selecionar; Ctrl+clique adiciona ou remove faces." : "Modo de seleção de partes ativado.";
    }

    private void SetMeshGizmoMode(EnemyMeshGizmoMode mode)
    {
        if (animationTimer.Enabled) return;
        viewport.SetEnemyModelGizmoMode(mode);
        UpdateMeshGizmoButtons();
    }

    private void UpdateMeshGizmoButtons()
    {
        btnMoveMesh.BackColor = viewport.EnemyModelGizmoMode == EnemyMeshGizmoMode.Move ? Color.FromArgb(65, 102, 154) : Color.FromArgb(42, 47, 56);
        btnRotateMesh.BackColor = viewport.EnemyModelGizmoMode == EnemyMeshGizmoMode.Rotate ? Color.FromArgb(65, 102, 154) : Color.FromArgb(42, 47, 56);
    }

    private void SelectFaceFromViewport(EnemyModelFaceHit? hit, bool additive)
    {
        if (!chkFaceEdit.Checked) return;
        if (!hit.HasValue) { if (!additive) ClearFaceSelection(); return; }
        EnemyModelFaceHit face = hit.Value;
        if (!additive || selectedFaceBin != face.Part.BinIndex) ClearFaceSelection();
        selectedFaceBin = face.Part.BinIndex;
        bool removing = additive && selectedFaceFlags.Contains(face.Triangle.StripFlagOffset);
        if (removing) selectedFaceFlags.Remove(face.Triangle.StripFlagOffset); else selectedFaceFlags.Add(face.Triangle.StripFlagOffset);
        RebuildSelectedFaceVertices(face.Part);
        selectedPart = face.Part; SelectPartByBinIndex(face.Part.BinIndex);
        viewport.SetSelectedEnemyModelFaces(selectedFaceBin, selectedFaceFlags);
        lblFaceSelection.Text = $"{selectedFaceFlags.Count} face(s)";
        btnDeleteFaces.Enabled = selectedFaceFlags.Count > 0;
        status.Text = $"BIN {selectedFaceBin:D2}: {selectedFaceFlags.Count} face(s) selecionada(s). Ctrl+clique para seleção múltipla.";
    }

    private void RebuildSelectedFaceVertices(EnemyModelPart part)
    {
        selectedFaceVertices.Clear();
        foreach (EnemyModelTriangle triangle in part.Triangles.Where(x => selectedFaceFlags.Contains(x.StripFlagOffset)))
        {
            if (triangle.SourceVertexA >= 0) selectedFaceVertices.Add(triangle.SourceVertexA);
            if (triangle.SourceVertexB >= 0) selectedFaceVertices.Add(triangle.SourceVertexB);
            if (triangle.SourceVertexC >= 0) selectedFaceVertices.Add(triangle.SourceVertexC);
        }
    }

    private void ClearFaceSelection()
    {
        selectedFaceFlags.Clear(); selectedFaceVertices.Clear(); selectedFaceBin = -1; lblFaceSelection.Text = "0 face(s)";
        btnDeleteFaces.Enabled = false;
        viewport.SetSelectedEnemyModelFaces(-1, null);
    }

    private void DeleteSelectedFaces()
    {
        if (animationTimer.Enabled) return;
        if (datPath == null || selectedPart == null || selectedFaceFlags.Count == 0 || selectedFaceBin != selectedPart.BinIndex)
        { MessageBox.Show(this, "Ative Selecionar Faces e escolha uma ou mais faces no modelo."); return; }
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            int removed = Ps2CharacterDatEditor.DeleteBinFaces(datPath, entry, selectedFaceFlags);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Excluir {removed} face(s) do BIN {binIndex:D2}", true));
            ClearFaceSelection(); showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"{removed} face(s) removida(s) do BIN {binIndex:D2}. Use Desfazer para restaurar.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Excluir faces", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void MoveSelectedFacesWithGizmo(Vector3 delta)
    {
        if (animationTimer.Enabled) return;
        if (datPath == null || selectedPart == null || selectedFaceFlags.Count == 0 || selectedFaceBin != selectedPart.BinIndex) return;
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.TransformBinVertices(datPath, entry, selectedFaceVertices, delta, Vector3.Zero, Vector3.One);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Mover {selectedFaceFlags.Count} face(s) do BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            viewport.SetSelectedEnemyModelFaces(binIndex, selectedFaceFlags);
            status.Text = $"{selectedFaceFlags.Count} face(s) movida(s) pelo gizmo • Δ {delta.X:0.##}, {delta.Y:0.##}, {delta.Z:0.##}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Mover faces", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void MoveSelectedMeshWithGizmo(Vector3 delta)
    {
        if (animationTimer.Enabled) return;
        if (datPath == null || selectedPart == null || chkFaceEdit.Checked) return;
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.TransformBinEntry(datPath, entry, delta, Vector3.Zero, Vector3.One);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Mover mesh BIN {binIndex:D2} pelo gizmo", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} movido pelo gizmo • Δ {delta.X:0.##}, {delta.Y:0.##}, {delta.Z:0.##}. Use Desfazer para reverter.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Mover mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RotateSelectedMeshWithGizmo(Vector3 degrees)
    {
        if (animationTimer.Enabled) return;
        if (datPath == null || selectedPart == null || chkFaceEdit.Checked) return;
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.TransformBinEntry(datPath, entry, Vector3.Zero, degrees, Vector3.One);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Rotacionar mesh BIN {binIndex:D2} pelo gizmo", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} rotacionado • {degrees.X:0.#}°, {degrees.Y:0.#}°, {degrees.Z:0.#}°. Use Desfazer para reverter.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Rotacionar mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void PopulateBoneDiagnostics(EnemyModelPart part)
    {
        lstBones.BeginUpdate(); lstBones.Items.Clear();
        var influences = new Dictionary<byte, (int Count, float Max)>();
        void Add(EnemyVertexSkin skin)
        {
            foreach (EnemySkinInfluence influence in new[] { skin.A, skin.B, skin.C }.Take(Math.Clamp(skin.Count, 0, 3)))
            {
                if (influence.Weight <= 0) continue;
                influences.TryGetValue(influence.BoneId, out var current);
                influences[influence.BoneId] = (current.Count + 1, Math.Max(current.Max, influence.Weight));
            }
        }
        foreach (EnemyModelTriangle triangle in part.Triangles) { Add(triangle.SkinA); Add(triangle.SkinB); Add(triangle.SkinC); }
        foreach (var pair in influences.OrderBy(x => x.Key))
        {
            bool physicsCandidate = pair.Key >= 64;
            var row = new ListViewItem(pair.Key.ToString()) { Tag = pair.Key, ForeColor = physicsCandidate ? Color.FromArgb(245, 180, 70) : Color.Gainsboro };
            row.SubItems.Add(pair.Value.Count.ToString("N0")); row.SubItems.Add(pair.Value.Max.ToString("P0")); row.SubItems.Add(physicsCandidate ? "secundário/física" : "normal");
            lstBones.Items.Add(row);
        }
        int suspicious = influences.Count(x => x.Key >= 64);
        lblBoneWarning.Text = influences.Count == 0 ? "Esta malha não expõe pesos de skin." : suspicious > 0 ? $"⚠ {suspicious} osso(s) secundário(s) detectado(s). Se a peça balançar, use encaixe Rígido." : $"{influences.Count} osso(s) usados; nenhuma influência secundária suspeita.";
        lstBones.EndUpdate(); UpdateBoneDiagnosticViewport();
    }

    private void UpdateBoneDiagnosticViewport()
    {
        byte? boneId = lstBones.SelectedItems.Count > 0 && lstBones.SelectedItems[0].Tag is byte selected ? selected : null;
        viewport.SetEnemyBoneDiagnostic(chkSkeleton.Checked, boneId);
    }

}
