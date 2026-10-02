namespace StationeersModCreator.Desktop.ViewModels;
public sealed class FieldEditorViewModel : ObservableViewModel
{
    private string _value;
    public string Path { get; }
    public string Label { get; }
    public string Original { get; }
    public string Group { get; }

    public string Value
    {
        get => _value;
        set
        {
            if (Set(ref _value, value))
                Notify(nameof(IsChanged));
        }
    }

    public bool IsChanged => Value != Original;

    public FieldEditorViewModel(string path, string label, string value, string group)
    {
        Path = path;
        Label = System.Text.RegularExpressions.Regex.Replace(path.Replace("/", " · "), "(?<=[a-z])(?=[A-Z])", " ");
        Original = value;
        _value = value;
        Group = group;
    }

    public void Reset() => Value = Original;
}
