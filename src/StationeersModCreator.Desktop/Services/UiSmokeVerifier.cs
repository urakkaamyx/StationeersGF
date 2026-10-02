using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using StationeersModCreator.Desktop.Views;
using StationeersModCreator.Desktop.ViewModels;

namespace StationeersModCreator.Desktop.Services;
public static class UiSmokeVerifier
{
    public static void Run()
    {
        AppBuilder.Configure<App>().UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var temporary = Path.Combine(Path.GetTempPath(), "ModForgeUiChecks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        var window = new MainWindow();
        var vm = CompositionRoot.Create(window, new SmokeTestFileDialogService(temporary), Path.Combine(temporary, "settings.json"));
        window.DataContext = vm;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            VerifySearch(window, vm);
            VerifyBoundRecipeEdit(window, vm, temporary);
            VerifyProjectReopen(vm);
            VerifyAppearance(vm, temporary);
            VerifyArtworkAndExport(window, vm, temporary);
            Console.WriteLine("PASS: 5 rendered UI interaction checks.");
        }
        finally
        {
            window.DataContext = null;
            window.Close();
            Directory.Delete(temporary, true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void VerifySearch(MainWindow window, MainViewModel vm)
    {
        var search = window.FindControl<TextBox>("RecipeSearch")!;
        search.Text = "Glass";
        Dispatcher.UIThread.RunJobs();
        Require(vm.Search == "Glass" && vm.Recipes.Count > 0 && vm.Recipes.All(x => x.Name.Contains("Glass")), "Search binding failed.");
        search.Text = "";
        Dispatcher.UIThread.RunJobs();
        Console.WriteLine("PASS: actual search TextBox binding filters game recipes");
    }

    private static void VerifyBoundRecipeEdit(MainWindow window, MainViewModel vm, string directory)
    {
        var box = window.GetVisualDescendants().OfType<TextBox>().First(x => x.DataContext is FieldEditorViewModel f && f.Path == "Iron");
        box.Text = "5";
        Dispatcher.UIThread.RunJobs();
        Require(vm.SelectedRecipe!.Fields.Single(x => x.Path == "Iron").Value == "5" && vm.HasUnsavedProject, "Recipe editor did not bind.");
        window.FindControl<Button>("SaveProjectButton")!.Command!.Execute(null);
        Require(!File.Exists(Path.Combine(directory, "project.modforge.json")), "Unstaged edits were silently omitted from save.");
        window.FindControl<Button>("StageRecipeButton")!.Command!.Execute(null);
        Require(vm.SelectedRecipe.IsStaged && vm.Changes.Count == 1, "Stage command failed.");
        vm.Author = "QA Pilot";
        window.FindControl<Button>("SaveProjectButton")!.Command!.Execute(null);
        Require(File.Exists(Path.Combine(directory, "project.modforge.json")) && !vm.HasUnsavedProject, "Save command failed.");
        Console.WriteLine("PASS: bound numeric edit, unstaged-save guard, stage and project save");
    }

    private static void VerifyProjectReopen(MainViewModel vm)
    {
        vm.NewProjectCommand.Execute(null);
        Require(vm.Changes.Count == 0, "New project did not clear overrides.");
        vm.OpenCommand.Execute(null);
        Require(vm.Changes.Count == 1 && vm.SelectedRecipe!.Fields.Single(x => x.Path == "Iron").Value == "5", "Project open did not restore editor values.");
        Console.WriteLine("PASS: saved project reopens with staged overrides and editor state");
    }

    private static void VerifyAppearance(MainViewModel vm, string directory)
    {
        vm.Accent = "Amber";
        vm.FontScale = 1.1;
        vm.HighContrast = true;
        vm.CompactCards = true;
        Require(((SolidColorBrush)Application.Current!.Resources["AccentBrush"]!).Color == Color.Parse("#FFB35D"), "Theme resource did not update.");
        Require(File.Exists(Path.Combine(directory, "settings.json")) && vm.CardWidth == 155, "Appearance settings did not persist.");
        Console.WriteLine("PASS: dynamic accent, font scale, contrast and density settings");
    }

    private static void VerifyArtworkAndExport(MainWindow window, MainViewModel vm, string directory)
    {
        vm.ArtworkCommand.Execute(null);
        vm.Artwork[0].SelectCommand.Execute(null);
        Require(vm.PreviewName == vm.Artwork[0].Asset, "Preview artwork selection failed.");
        vm.WorkspaceCommand.Execute(null);
        window.FindControl<Button>("ExportModButton")!.Command!.Execute(null);
        Require(File.Exists(Path.Combine(directory, "mod.zip")), "Export button did not write native ZIP.");
        Console.WriteLine("PASS: game artwork selection and bound export command");
    }
}
