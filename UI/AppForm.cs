using System.Reflection;

namespace RE4_PS2_MOD_WORKSPACE;

/// <summary>Base window that applies the application icon before its native handle is created.</summary>
public class AppForm : Form
{
    private static readonly Icon ApplicationIcon = LoadApplicationIcon();

    public AppForm()
    {
        Icon = (Icon)ApplicationIcon.Clone();
    }

    private static Icon LoadApplicationIcon()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.ico");
        if (stream is not null)
        {
            using var embeddedIcon = new Icon(stream);
            return (Icon)embeddedIcon.Clone();
        }

        Icon? executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return executableIcon is null ? (Icon)SystemIcons.Application.Clone() : executableIcon;
    }
}