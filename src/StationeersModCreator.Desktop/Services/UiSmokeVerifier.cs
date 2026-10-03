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
            vm.RecipesCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            VerifySearch(window, vm);
            VerifyBoundRecipeEdit(window, vm, temporary);
            VerifyProjectReopen(vm);
            VerifyAppearance(vm, temporary);
            VerifyArtworkAndExport(window, vm, temporary);
            VerifyNativeStudio(window, vm, temporary);
            VerifyInventory(window, vm, temporary);
            VerifySetupDashboard(window, vm, temporary);
            Console.WriteLine("PASS: 8 rendered UI interaction checks.");
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

    private static void VerifyNativeStudio(MainWindow window, MainViewModel vm, string directory)
    {
        vm.NativeStudioCommand.Execute(null);
        vm.NativeStudio.Editor.Select(vm.NativeStudio.Definitions.Single(x => x.Name == "DefaultStart").Definition);
        vm.NativeStudio.Editor.CloneCommand.Execute(null);
        vm.NativeStudio.Editor.ExportId = "Forge.UiStart";
        vm.NativeStudio.Editor.WorldId = "Mars2";
        vm.NativeStudio.Editor.AssignWorldCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var stage = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "NativeStageButton");
        stage.Command!.Execute(null);
        Require(vm.Changes.Any(x => x.Name == "Forge.UiStart"), "Bound native staging failed: " + vm.NativeStudio.Editor.Status);
        vm.UndoCommand.Execute(null);
        Require(!vm.Changes.Any(x => x.Name == "Forge.UiStart"), "Undo failed for native staging.");
        vm.RedoCommand.Execute(null);
        Require(vm.Changes.Any(x => x.Name == "Forge.UiStart"), "Redo failed for native staging.");
        vm.NativeStudio.Editor.ExportId = "Forge.UnfinishedStart";
        vm.SaveCommand.Execute(null);
        vm.NewProjectCommand.Execute(null);
        vm.OpenCommand.Execute(null);
        Require(vm.NativeStudio.Editor.ExportId == "Forge.UnfinishedStart" && vm.NativeStudio.Editor.HasPendingEdits, "Unfinished native edit lost on reopen.");
        vm.NativeStudio.Editor.RestoreCommand.Execute(null);
        vm.SaveCommand.Execute(null);
        vm.NewProjectCommand.Execute(null);
        vm.OpenCommand.Execute(null);
        Require(vm.Changes.Any(x => x.Name == "Forge.UiStart"), "Native definitions lost on project reopen.");
        vm.ExportCommand.Execute(null);
        using var archive = System.IO.Compression.ZipFile.OpenRead(Path.Combine(directory, "mod.zip"));
        var entry = archive.Entries.Single(x => x.FullName.EndsWith("GameData/forge-definitions.xml"));
        using var stream = entry.Open();
        var xml = System.Xml.Linq.XDocument.Load(stream);
        Require((string? )xml.Descendants("StartCondition").Single().Attribute("Id") == "Forge.UiStart", "Native start export identity incorrect.");
        Require(!archive.Entries.Any(x => x.FullName.EndsWith(".project.json")), "Editable project bundled in native mod.");
        Console.WriteLine("PASS: bound starting-condition clone, exact world assignment, native staging, save/reopen and mod-only export.");
    }

    private static void VerifyInventory(MainWindow window, MainViewModel vm, string directory)
    {
        vm.InventoryCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var inventory=vm.Inventory;
        inventory.Slots.Single(x=>x.Slot.Index==3).SelectCommand.Execute(null);
        inventory.OpenContentsCommand.Execute(null);
        Require(inventory.Slots.Count==6 && inventory.Slots.All(x=>x.HasItem), "EVA suit contents did not open.");
        inventory.Slots.Single(x=>x.Slot.Index==3).SelectCommand.Execute(null);
        inventory.RemoveCommand.Execute(null);
        Require(inventory.Slots.Count(x=>x.HasItem)==5 && inventory.HasPendingEdits,"Filter removal failed.");
        vm.SaveCommand.Execute(null);
        vm.NewProjectCommand.Execute(null);
        vm.OpenCommand.Execute(null);
        Require(vm.Inventory.HasPendingEdits,"Pending inventory lost on project reopen.");
        vm.Inventory.Slots.Single(x=>x.Slot.Index==3).SelectCommand.Execute(null);
        vm.Inventory.OpenContentsCommand.Execute(null);
        Require(vm.Inventory.Slots.Count(x=>x.HasItem)==5,"Pending nested inventory changed on reopen.");
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<Button>().Single(x=>x.Name=="InventoryStageButton").Command!.Execute(null);
        Require(vm.Changes.Any(x=>x.Name=="Forge.MyStart.NewPlayerKit.Human.Normal"),"Bound inventory staging failed: "+vm.Inventory.Status);
        vm.ExportCommand.Execute(null);
        using var archive=System.IO.Compression.ZipFile.OpenRead(Path.Combine(directory,"mod.zip"));
        using var stream=archive.Entries.Single(x=>x.FullName.EndsWith("GameData/forge-definitions.xml")).Open();
        var xml=System.Xml.Linq.XDocument.Load(stream);
        Require(xml.Descendants("Spawn").Any(x=>(string?)x.Attribute("Id")=="Forge.MyStart.NewPlayerKit.Human.Normal"),"Local inventory kit absent in export.");
        Console.WriteLine("PASS: suit contents, filter removal, pending inventory save/reopen, bound staging and local kit export.");
    }

    private static void VerifySetupDashboard(MainWindow window, MainViewModel vm, string directory)
    {
        vm.SetupCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Require(vm.IsSetup && !ReferenceEquals(vm.StartingInventory, vm.RespawnInventory), "Setup editors are shared.");
        var cards=window.GetVisualDescendants().OfType<Button>().Where(x=>x.DataContext is SetupCardViewModel).ToList();
        cards.Single(x=>((SetupCardViewModel)x.DataContext!).Title=="World setup").Command!.Execute(null);
        Require(vm.IsNativeStudio && vm.NativeStudio.Category=="Worlds", "World setup card routing failed.");
        vm.NativeStudio.LandersCommand.Execute(null);
        Require(vm.NativeStudio.Category=="Spawn packages" && vm.NativeStudio.Search=="Lander" && vm.NativeStudio.Definitions.Count>0, "Landers shortcut did not load cargo definitions.");
        vm.NativeStudio.DifficultyCommand.Execute(null);
        Require(vm.NativeStudio.Category=="Difficulty" && vm.NativeStudio.Search=="" && vm.NativeStudio.Definitions.Count>0, "Difficulty shortcut kept stale cargo search.");
        vm.SetupCommand.Execute(null);
        cards.Single(x=>((SetupCardViewModel)x.DataContext!).Title=="Starting setup").Command!.Execute(null);
        Require(vm.IsStarting && vm.Inventory.Event=="NewPlayerKit", "Starting setup routing failed.");
        vm.StartingInventory.RootCommand.Execute(null);
        vm.StartingInventory.Slots.Single(x=>x.Slot.Index==0).SelectCommand.Execute(null);
        vm.StartingInventory.ItemId="ItemDuctTape";
        vm.StartingInventory.AddCommand.Execute(null);
        vm.SetupCommand.Execute(null);
        cards.Single(x=>((SetupCardViewModel)x.DataContext!).Title=="Respawn setup").Command!.Execute(null);
        Require(vm.IsRespawn && vm.Inventory.Event=="RespawnPlayerKit", "Respawn setup routing failed.");
        vm.RespawnInventory.RootCommand.Execute(null);
        vm.RespawnInventory.Slots.Single(x=>x.Slot.Index==0).SelectCommand.Execute(null);
        vm.RespawnInventory.ItemId="ItemDuctTape";
        vm.RespawnInventory.AddCommand.Execute(null);
        Require(vm.StartingInventory.HasPendingEdits && vm.RespawnInventory.HasPendingEdits,"Switching setups lost a draft.");
        var count=vm.Changes.Count;
        vm.Changes.Single(x=>x.Name=="Forge.MyStart").RemoveCommand.Execute(null);
        Require(vm.Changes.Count==count && vm.StartingInventory.HasPendingEdits && vm.RespawnInventory.HasPendingEdits,"Removing a staged definition discarded pending setup drafts.");
        vm.SaveCommand.Execute(null);
        vm.NewProjectCommand.Execute(null);
        vm.OpenCommand.Execute(null);
        Require(vm.StartingInventory.HasPendingEdits && vm.RespawnInventory.HasPendingEdits,"Separate drafts lost on reopen.");
        Require(vm.StartingInventory.CapturePending()!.Context.Event=="NewPlayerKit" && vm.RespawnInventory.CapturePending()!.Context.Event=="RespawnPlayerKit","Saved setup events conflated.");
        File.Delete(Path.Combine(directory,"mod.zip"));
        vm.ExportCommand.Execute(null);
        Require(!File.Exists(Path.Combine(directory,"mod.zip")),"Unstaged separate setups exported.");
        vm.StartingInventory.StageCommand.Execute(null);
        Require(!vm.StartingInventory.HasPendingEdits && vm.RespawnInventory.HasPendingEdits,"Staging starting kit changed respawn draft: "+vm.StartingInventory.Status);
        vm.RespawnInventory.StageCommand.Execute(null);
        Require(!vm.RespawnInventory.HasPendingEdits,"Respawn staging failed: "+vm.RespawnInventory.Status);
        vm.ExportCommand.Execute(null);
        Require(File.Exists(Path.Combine(directory,"mod.zip")),"Separate setups did not export: "+vm.Status);
        using var archive=System.IO.Compression.ZipFile.OpenRead(Path.Combine(directory,"mod.zip"));
        using var stream=archive.Entries.Single(x=>x.FullName.EndsWith("GameData/forge-definitions.xml")).Open();
        var xml=System.Xml.Linq.XDocument.Load(stream);
        foreach(var evt in new[]{"NewPlayerKit","RespawnPlayerKit"})
            Require(xml.Descendants("Spawn").Single(x=>(string?)x.Attribute("Id")=="Forge.MyStart."+evt+".Human.Normal" && x.Elements("Item").Any()).Elements("Item").Any(x=>(string?)x.Attribute("Id")=="ItemDuctTape"&&(string?)x.Attribute("SlotIndex")=="0"),"Independent staged kit item missing.");
        vm.SetupCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var machine=window.GetVisualDescendants().OfType<Button>().First(x=>x.DataContext is MachineCardViewModel m && vm.SetupMachines.Contains(m));
        var selected=(MachineCardViewModel)machine.DataContext!;
        machine.Command!.Execute(null);
        Require(vm.IsRecipes && vm.MachineName==selected.Name && vm.Recipes.All(x=>x.Definition.Section==selected.Definition.Id),"Machine card opened incorrect recipes.");
        Console.WriteLine("PASS: setup cards, independent starting/respawn drafts, save/reopen, staging/export and machine-specific recipes.");
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
        Require(File.Exists(Path.Combine(directory, "project.modforge.json")), "Pending draft was not saved.");
        vm.NewProjectCommand.Execute(null);
        vm.OpenCommand.Execute(null);
        Require(vm.Changes.Count == 0 && vm.SelectedRecipe!.Fields.Single(x => x.Path == "Iron").Value == "5", "Pending recipe draft did not reopen.");
        vm.ExportCommand.Execute(null);
        Require(!File.Exists(Path.Combine(directory, "mod.zip")), "Pending draft exported without staging.");
        window.FindControl<Button>("StageRecipeButton")!.Command!.Execute(null);
        Require(vm.SelectedRecipe.IsStaged && vm.Changes.Count == 1, "Stage command failed.");
        vm.Author = "QA Pilot";
        window.FindControl<Button>("SaveProjectButton")!.Command!.Execute(null);
        Require(File.Exists(Path.Combine(directory, "project.modforge.json")) && !vm.HasUnsavedProject, "Save command failed.");
        Console.WriteLine("PASS: bound numeric edit, pending-draft save/reopen and export guard, stage and project save");
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
