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
            case "Inventory":
                vm.InventoryCommand.Execute(null);
                break;
            case "InventorySuit":
                vm.InventoryCommand.Execute(null);
                vm.Inventory.Slots.First(x => x.Slot.Index == 3).SelectCommand.Execute(null);
                vm.Inventory.OpenContentsCommand.Execute(null);
                break;
            case "Native":
                vm.NativeStudioCommand.Execute(null);
                vm.NativeStudio.Editor.Select(vm.NativeStudio.Definitions.First(x => x.Name == "DefaultStart").Definition);
                break;
            case "Cargo":
                vm.NativeStudioCommand.Execute(null);
                vm.NativeStudio.LandersCommand.Execute(null);
                vm.NativeStudio.Editor.Select(vm.NativeStudio.Definitions.First(x => x.Name == "DefaultLander").Definition);
                vm.NativeStudio.Editor.Nodes.First(x => x.Tag == "DynamicThing").OpenCommand.Execute(null);
                break;
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
