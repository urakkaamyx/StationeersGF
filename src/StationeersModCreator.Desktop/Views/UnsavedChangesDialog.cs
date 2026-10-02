using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace StationeersModCreator.Desktop.Views;
public sealed class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        Title = "Unsaved project";
        Width = 440;
        Height = 245;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var keep = new Button
        {
            Content = "Return to project"
        };
        var discard = new Button
        {
            Content = "Discard and close"
        };
        discard.Classes.Add("primary");
        keep.Click += (_, _) => Close(false);
        discard.Click += (_, _) => Close(true);
        Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 19,
            Children =
            {
                new TextBlock
                {
                    Text = "Your project has unsaved changes.",
                    FontSize = 21,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = "Return to stage and save your edits, or discard them to close the tool.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children =
                    {
                        keep,
                        discard
                    }
                }
            }
        };
    }
}
