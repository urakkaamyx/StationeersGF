using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using StationeersModCreator.Desktop.Views;
using StationeersModCreator.Desktop.ViewModels;

namespace StationeersModCreator.Desktop.Services;
public static class PreviewRenderer
{
    public static void Render(string path, string page)
    {
        AppBuilder.Configure<App>().UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var window = new MainWindow();
        var vm = CompositionRoot.Create(window);
        window.DataContext = vm;
        Navigate(vm, page);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("Preview frame unavailable.");
        frame.Save(path);
        window.DataContext = null;
        window.Close();
        Console.WriteLine("Rendered " + page + ": " + path);
    }

    private static void Navigate(MainViewModel vm, string page)
    {
        switch (page)
        {
            case "Attributes":
                vm.AttributesCommand.Execute(null);
                break;
            case "Artwork":
                vm.ArtworkCommand.Execute(null);
                break;
            case "Workspace":
                vm.SelectedRecipe!.Fields.First(x => x.Path == "Iron").Value = "5";
                vm.StageRecipeCommand.Execute(null);
                vm.WorkspaceCommand.Execute(null);
                break;
            case "Settings":
                vm.SettingsCommand.Execute(null);
                break;
        }
    }
}
