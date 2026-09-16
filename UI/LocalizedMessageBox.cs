namespace RE4_PS2_MOD_WORKSPACE;

/// <summary>Localizes every message box, including messages assembled at runtime.</summary>
internal static class MessageBox
{
    public static DialogResult Show(string text) => System.Windows.Forms.MessageBox.Show(LanguageService.Translate(text));
    public static DialogResult Show(IWin32Window owner, string text) => System.Windows.Forms.MessageBox.Show(owner, LanguageService.Translate(text));
    public static DialogResult Show(string text, string caption) => System.Windows.Forms.MessageBox.Show(LanguageService.Translate(text), LanguageService.Translate(caption));
    public static DialogResult Show(IWin32Window owner, string text, string caption) => System.Windows.Forms.MessageBox.Show(owner, LanguageService.Translate(text), LanguageService.Translate(caption));
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) => System.Windows.Forms.MessageBox.Show(LanguageService.Translate(text), LanguageService.Translate(caption), buttons);
    public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons) => System.Windows.Forms.MessageBox.Show(owner, LanguageService.Translate(text), LanguageService.Translate(caption), buttons);
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) => System.Windows.Forms.MessageBox.Show(LanguageService.Translate(text), LanguageService.Translate(caption), buttons, icon);
    public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) => System.Windows.Forms.MessageBox.Show(owner, LanguageService.Translate(text), LanguageService.Translate(caption), buttons, icon);
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => System.Windows.Forms.MessageBox.Show(LanguageService.Translate(text), LanguageService.Translate(caption), buttons, icon, defaultButton);
    public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) => System.Windows.Forms.MessageBox.Show(owner, LanguageService.Translate(text), LanguageService.Translate(caption), buttons, icon, defaultButton);
}