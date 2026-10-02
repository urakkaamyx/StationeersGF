using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StationeersModCreator.Desktop.ViewModels;

namespace StationeersModCreator.Desktop.Views;
public partial class MainWindow : Window
{
    private bool _allowClose;
    private bool _confirming;
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Closing += ConfirmClose;
    }

    private async void ConfirmClose(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not MainViewModel { HasUnsavedProject: true })
            return;
        e.Cancel = true;
        if (_confirming)
            return;
        _confirming = true;
        try
        {
            if (await new UnsavedChangesDialog().ShowDialog<bool>(this))
            {
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _confirming = false;
        }
    }
}
