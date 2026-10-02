using Avalonia;
using StationeersModCreator.Desktop.Services;

namespace StationeersModCreator.Desktop;
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--preview")
        {
            PreviewRenderer.Render(args[1], args.ElementAtOrDefault(2) ?? "Recipes");
            return;
        }

        if (args.Length == 1 && args[0] == "--ui-checks")
        {
            UiSmokeVerifier.Run();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont();
}
