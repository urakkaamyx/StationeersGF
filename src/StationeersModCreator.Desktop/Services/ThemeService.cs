using Avalonia;
using Avalonia.Media;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;

namespace StationeersModCreator.Desktop.Services;
public sealed class ThemeService : IThemeService
{
    public void Apply(EditorSettings settings)
    {
        var resources = Application.Current!.Resources;
        var hex = settings.Accent switch
        {
            "Amber" => "#FFB35D",
            "Violet" => "#BD9DFF",
            "Rose" => "#FF8AA4",
            _ => "#65E3DB"
        };
        resources["AccentBrush"] = new SolidColorBrush(Color.Parse(hex));
        resources["MutedBrush"] = new SolidColorBrush(Color.Parse(settings.HighContrast ? "#DCE8ED" : "#8D9DAE"));
    }
}
