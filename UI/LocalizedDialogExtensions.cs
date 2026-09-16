namespace RE4_PS2_MOD_WORKSPACE;

internal static class LocalizedDialogExtensions
{
    public static DialogResult ShowLocalizedDialog(this CommonDialog dialog, IWin32Window? owner = null)
    {
        switch (dialog)
        {
            case FileDialog file:
                file.Title = LanguageService.Translate(file.Title);
                file.Filter = TranslateFilter(file.Filter);
                break;
            case FolderBrowserDialog folder:
                folder.Description = LanguageService.Translate(folder.Description);
                break;
        }
        return owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    public static DialogResult ShowLocalizedDialog(this Form dialog, IWin32Window? owner = null)
    {
        LanguageService.Apply(dialog);
        return owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    private static string TranslateFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return filter;
        string[] parts = filter.Split('|');
        for (int i = 0; i < parts.Length; i += 2) parts[i] = LanguageService.Translate(parts[i]);
        return string.Join("|", parts);
    }
}