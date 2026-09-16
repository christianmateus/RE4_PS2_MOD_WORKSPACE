namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private bool changingLanguage;

    private void InitializeLocalization()
    {
        availableLanguages = LanguageService.Discover();
        if (availableLanguages.Count == 0)
            availableLanguages = new[] { new LanguageInfo(LanguageService.DefaultLanguage, "Português (Brasil)", string.Empty) };
        changingLanguage = true;
        cmbSettingsLanguage.Items.Clear();
        cmbSettingsLanguage.Items.AddRange(availableLanguages.Cast<object>().ToArray());
        int selected = availableLanguages.ToList().FindIndex(x => string.Equals(x.Code, settings.Language, StringComparison.OrdinalIgnoreCase));
        cmbSettingsLanguage.SelectedIndex = selected >= 0 ? selected : 0;
        changingLanguage = false;
        ApplyLanguage(availableLanguages[cmbSettingsLanguage.SelectedIndex]);
    }

    private void ApplyLanguage(LanguageInfo language)
    {
        SuspendLayout();
        try
        {
            LanguageService.SetLanguage(language);
            int index = cmbSettingsAutoSaveInterval.SelectedIndex;
            changingLanguage = true;
            cmbSettingsAutoSaveInterval.Items.Clear();
            cmbSettingsAutoSaveInterval.Items.AddRange(new object[]
            {
                LanguageService.T("settings.autosave.interval.1"),
                LanguageService.T("settings.autosave.interval.3"),
                LanguageService.T("settings.autosave.interval.5"),
                LanguageService.T("settings.autosave.interval.10")
            });
            cmbSettingsAutoSaveInterval.SelectedIndex = Math.Clamp(index, 0, 3);
            LanguageService.Apply(this);
        }
        finally
        {
            changingLanguage = false;
            ResumeLayout(true);
        }
    }

    private void cmbSettingsLanguage_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (changingLanguage || cmbSettingsLanguage.SelectedItem is not LanguageInfo language) return;
        settings.Language = language.Code;
        ApplyLanguage(language);
        if (!restoringSession) SaveSettings();
    }
}