using System.Text.Json;
using StationeersModCreator.Core.Interfaces;
using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Services;
public sealed class ProjectHistory : IProjectHistory
{
    private readonly List<string> _undo = [];
    private readonly List<string> _redo = [];
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Capture(ModProject project)
    {
        var state = JsonSerializer.Deserialize<ModProject>(JsonSerializer.Serialize(project))!;
        state.PendingRecipes.Clear();
        state.PendingAttributes.Clear();
        state.PendingDefinition = null;
        _undo.Add(JsonSerializer.Serialize(state));
        if (_undo.Count > 100)
            _undo.RemoveAt(0);
        _redo.Clear();
    }

    public ModProject Undo(ModProject current)
    {
        if (!CanUndo)
            throw new InvalidOperationException("Nothing to undo.");
        _redo.Add(JsonSerializer.Serialize(current));
        return Pop(_undo);
    }

    public ModProject Redo(ModProject current)
    {
        if (!CanRedo)
            throw new InvalidOperationException("Nothing to redo.");
        _undo.Add(JsonSerializer.Serialize(current));
        return Pop(_redo);
    }

    private static ModProject Pop(List<string> stack)
    {
        var json = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return JsonSerializer.Deserialize<ModProject>(json)!;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
