using System.Windows.Input;

namespace StationeersModCreator.Desktop.ViewModels;
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _running;
    public AsyncRelayCommand(Func<Task> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => !_running;
    public event EventHandler? CanExecuteChanged;
    public async void Execute(object? parameter)
    {
        if (_running)
            return;
        _running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await _execute();
        }
        finally
        {
            _running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
