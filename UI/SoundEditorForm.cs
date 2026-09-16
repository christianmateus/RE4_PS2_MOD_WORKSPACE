using RE4_PS2_MOD_WORKSPACE.Core.Audio;
using System.Media;

namespace RE4_PS2_MOD_WORKSPACE;

internal sealed class SoundEditorForm : AppForm
{
    private static readonly Color Bg = Color.FromArgb(13, 15, 18), Surface = Color.FromArgb(22, 25, 30), Surface2 = Color.FromArgb(28, 31, 37), Border = Color.FromArgb(47, 51, 60), TextPrimary = Color.FromArgb(238, 240, 244), TextMuted = Color.FromArgb(145, 151, 163), Accent = Color.FromArgb(196, 56, 56);
    private short[] samples;
    private int sampleRate;
    private bool loop;
    private readonly Stack<EditorState> undo = new();
    private readonly AudioWaveform waveform = new();
    private readonly NumericUpDown nudGain = new(), nudRate = new(), nudFade = new();
    private readonly CheckBox chkLoop = new();
    private readonly Label lblInfo = new(), lblStatus = new();
    private readonly ToolTip tips = new();
    private SoundPlayer? player;
    private string? previewPath;

    public byte[] ResultData { get; private set; } = [];
    public uint ResultRate { get; private set; }
    public uint ResultLoop { get; private set; }

    public SoundEditorForm(SndSample sample)
    {
        samples = PsxAdpcm.Decode(sample.EffectiveData); sampleRate = checked((int)sample.SampleRate); loop = sample.LoopFlag != 0;
        Text = $"Ajustar faixa #{sample.Index + 1:000}"; StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(760, 570); MinimumSize = new Size(700, 540); BackColor = Bg; ForeColor = TextPrimary; Font = new Font("Segoe UI", 9F); KeyPreview = true; KeyDown += OnEditorKeyDown; FormClosed += (_, _) => StopPreview();
        var title = new Label { Text = "AJUSTES DE ÁUDIO", Left = 22, Top = 18, Width = 350, Height = 30, Font = new Font("Segoe UI Semibold", 18F), ForeColor = TextPrimary }; Controls.Add(title);
        lblInfo.SetBounds(24, 53, 700, 22); lblInfo.ForeColor = TextMuted; Controls.Add(lblInfo);
        waveform.SetBounds(22, 84, 716, 172); waveform.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; waveform.BackColor = Surface; waveform.ForeColor = Accent; Controls.Add(waveform);

        var settings = new Panel { Left = 22, Top = 270, Width = 716, Height = 192, BackColor = Surface, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; Controls.Add(settings);
        AddCaption(settings, "GANHO", 16, 14); nudGain.SetBounds(16, 38, 94, 28); nudGain.DecimalPlaces = 1; nudGain.Minimum = -48; nudGain.Maximum = 24; nudGain.Increment = .5M; nudGain.BackColor = Surface2; nudGain.ForeColor = TextPrimary; settings.Controls.Add(nudGain); AddButton(settings, "APLICAR dB", 118, 38, 100, (_, _) => ApplyGain(), 28);
        AddCaption(settings, "FREQUÊNCIA / PITCH", 244, 14); nudRate.SetBounds(244, 38, 112, 28); nudRate.Minimum = 1000; nudRate.Maximum = 192000; nudRate.Increment = 1000; nudRate.BackColor = Surface2; nudRate.ForeColor = TextPrimary; settings.Controls.Add(nudRate); AddButton(settings, "APLICAR", 364, 38, 90, (_, _) => ApplySettings(), 28);
        chkLoop.Text = "Ativar loop"; chkLoop.SetBounds(490, 38, 120, 28); chkLoop.ForeColor = TextPrimary; chkLoop.BackColor = Surface; settings.Controls.Add(chkLoop); AddButton(settings, "APLICAR", 610, 38, 88, (_, _) => ApplySettings(), 28);

        AddCaption(settings, "PROCESSAMENTO", 16, 83); AddButton(settings, "NORMALIZAR", 16, 107, 108, (_, _) => Normalize(), 28); AddButton(settings, "REMOVER SILÊNCIO", 132, 107, 132, (_, _) => TrimSilence(), 28); AddButton(settings, "INVERTER", 272, 107, 92, (_, _) => Reverse(), 28);
        AddCaption(settings, "FADE (ms)", 390, 83); nudFade.SetBounds(390, 107, 88, 28); nudFade.Minimum = 1; nudFade.Maximum = 10000; nudFade.Value = 100; nudFade.BackColor = Surface2; nudFade.ForeColor = TextPrimary; settings.Controls.Add(nudFade); AddButton(settings, "FADE IN", 486, 107, 94, (_, _) => Fade(true), 28); AddButton(settings, "FADE OUT", 588, 107, 102, (_, _) => Fade(false), 28);
        AddButton(settings, "DESFAZER  Ctrl+Z", 16, 151, 142, (_, _) => Undo());

        lblStatus.SetBounds(24, 474, 700, 28); lblStatus.ForeColor = TextMuted; Controls.Add(lblStatus);
        var play = new Button { Text = "▶", Left = 22, Top = 508, Width = 48, Height = 36, BackColor = Accent, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Symbol", 11F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left }; play.FlatAppearance.BorderSize = 0; play.Click += (_, _) => Play(); Controls.Add(play);
        var stop = new Button { Text = "■", Left = 78, Top = 508, Width = 48, Height = 36, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Symbol", 10F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left }; stop.FlatAppearance.BorderColor = Border; stop.Click += (_, _) => StopPreview(); Controls.Add(stop);
        tips.SetToolTip(play, "Ouvir prévia"); tips.SetToolTip(stop, "Parar reprodução");
        var cancel = new Button { Text = "CANCELAR", DialogResult = DialogResult.Cancel, Left = 520, Top = 508, Width = 100, Height = 36, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; cancel.FlatAppearance.BorderColor = Border; Controls.Add(cancel);
        var save = new Button { Text = "APLICAR AJUSTES", Left = 628, Top = 508, Width = 110, Height = 36, BackColor = Accent, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; save.FlatAppearance.BorderSize = 0; save.Click += (_, _) => SaveResult(); Controls.Add(save); CancelButton = cancel;
        nudRate.Value = Math.Clamp(sampleRate, (int)nudRate.Minimum, (int)nudRate.Maximum); chkLoop.Checked = loop; RefreshView("Use os controles para ajustar a faixa. Ctrl+Z desfaz cada operação.");
    }

    private static void AddCaption(Control parent, string text, int x, int y) => parent.Controls.Add(new Label { Text = text, Left = x, Top = y, Width = 150, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
    private static void AddButton(Control parent, string text, int x, int y, int width, EventHandler click, int height = 30) { var b = new Button { Text = text, Left = x, Top = y, Width = width, Height = height, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 8F) }; b.FlatAppearance.BorderColor = Border; b.Click += click; parent.Controls.Add(b); }
    private void PushUndo() => undo.Push(new EditorState((short[])samples.Clone(), sampleRate, loop));
    private void ApplyGain() { PushUndo(); double gain = Math.Pow(10, (double)nudGain.Value / 20); for (int i = 0; i < samples.Length; i++) samples[i] = (short)Math.Clamp((int)Math.Round(samples[i] * gain), short.MinValue, short.MaxValue); RefreshView($"Ganho de {nudGain.Value:0.0} dB aplicado."); }
    private void Normalize() { int peak = samples.Select(x => Math.Abs((int)x)).DefaultIfEmpty().Max(); if (peak == 0) return; PushUndo(); double gain = 32700d / peak; for (int i = 0; i < samples.Length; i++) samples[i] = (short)Math.Clamp((int)Math.Round(samples[i] * gain), short.MinValue, short.MaxValue); RefreshView("Normalizado para -0,02 dBFS."); }
    private void Reverse() { PushUndo(); Array.Reverse(samples); RefreshView("Áudio invertido."); }
    private void TrimSilence() { int threshold = 64, first = Array.FindIndex(samples, x => Math.Abs((int)x) > threshold), last = Array.FindLastIndex(samples, x => Math.Abs((int)x) > threshold); if (first < 0 || (first == 0 && last == samples.Length - 1)) return; PushUndo(); samples = first < 0 ? [0] : samples[first..(last + 1)]; RefreshView("Silêncio nas extremidades removido."); }
    private void Fade(bool fadeIn) { int count = Math.Min(samples.Length, (int)(sampleRate * (double)nudFade.Value / 1000)); if (count <= 0) return; PushUndo(); for (int i = 0; i < count; i++) { int p = fadeIn ? i : samples.Length - 1 - i; samples[p] = (short)Math.Round(samples[p] * i / (double)count); } RefreshView(fadeIn ? "Fade-in aplicado." : "Fade-out aplicado."); }
    private void ApplySettings() { int rate = (int)nudRate.Value; bool newLoop = chkLoop.Checked; if (rate == sampleRate && newLoop == loop) return; PushUndo(); sampleRate = rate; loop = newLoop; RefreshView("Frequência e loop atualizados."); }
    private void Undo() { if (undo.Count == 0) { lblStatus.Text = "Nada para desfazer."; return; } StopPreview(); EditorState state = undo.Pop(); samples = state.Samples; sampleRate = state.SampleRate; loop = state.Loop; nudRate.Value = sampleRate; chkLoop.Checked = loop; RefreshView("Último ajuste desfeito."); }
    private void Play() { try { StopPreview(); previewPath = Path.Combine(Path.GetTempPath(), $"re4ps2-editor-{Guid.NewGuid():N}.wav"); File.WriteAllBytes(previewPath, WaveCodec.Write(samples, sampleRate)); player = new SoundPlayer(previewPath); player.Play(); lblStatus.Text = "Reproduzindo prévia..."; } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Prévia", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
    private void StopPreview() { player?.Stop(); player?.Dispose(); player = null; if (previewPath != null) { try { File.Delete(previewPath); } catch { } previewPath = null; } }
    private void RefreshView(string status) { waveform.Samples = samples; waveform.Invalidate(); lblInfo.Text = $"{samples.Length:N0} amostras  •  {sampleRate:N0} Hz  •  {samples.Length / (double)sampleRate:0.00} s  •  {(loop ? "loop ativo" : "sem loop")}"; lblStatus.Text = status; }
    private void SaveResult() { ApplySettings(); StopPreview(); ResultData = PsxAdpcm.Encode(samples, loop); ResultRate = checked((uint)sampleRate); ResultLoop = loop ? 1u : 0u; DialogResult = DialogResult.OK; Close(); }
    private void OnEditorKeyDown(object? sender, KeyEventArgs e) { if (e.Control && e.KeyCode == Keys.Z) { Undo(); e.Handled = e.SuppressKeyPress = true; } }
    private sealed record EditorState(short[] Samples, int SampleRate, bool Loop);
}

internal sealed class AudioWaveform : Control
{
    public short[] Samples { get; set; } = [];
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.Clear(BackColor); using var grid = new Pen(Color.FromArgb(43, 47, 55)); using var wave = new Pen(ForeColor, 1.2f); int mid = Height / 2; e.Graphics.DrawLine(grid, 0, mid, Width, mid); if (Samples.Length == 0 || Width < 2) return;
        int step = Math.Max(1, Samples.Length / Width); for (int x = 0; x < Width; x++) { int start = x * Samples.Length / Width, end = Math.Min(Samples.Length, start + step); short min = 0, max = 0; for (int i = start; i < end; i++) { if (Samples[i] < min) min = Samples[i]; if (Samples[i] > max) max = Samples[i]; } e.Graphics.DrawLine(wave, x, mid - max * mid / 32768f, x, mid - min * mid / 32768f); }
    }
}
