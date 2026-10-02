using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class NativeFieldEditorViewModel : ObservableViewModel
{
    private string _value;
    private readonly Action<string, string> _edit;
    public string Path { get; }
    public string Label { get; }
    public string Original { get; }
    public bool IsChanged => Value != Original;

    public string Value
    {
        get => _value;
        set
        {
            if (Set(ref _value, value))
            {
                _edit(Path, value);
                Notify(nameof(IsChanged));
            }
        }
    }

    public NativeFieldEditorViewModel(XmlField field, Action<string, string> edit)
    {
        Path = field.Path;
        Label = field.Label;
        Original = field.Value;
        _value = field.Value;
        _edit = edit;
    }
}
