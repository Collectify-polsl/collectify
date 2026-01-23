using System;
using System.Windows.Input;

namespace Collectify.App.Commands;

/// <summary>
/// Generic version of the command that accepts a parameter of type T.
/// Used for example when deleting a specific row from a list: RelayCommand<ColumnItem>
/// </summary>
/// <typeparam name="T">Data type passed from the view (e.g., ColumnItem, string, int).</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

    // Initializes a new command instance with typed execution and validation logic.
    public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    // Checks if the parameter is compatible with type T and meets the execution criteria.
    public bool CanExecute(object? parameter)
    {
        if (parameter is T t)
        {
            return _canExecute == null || _canExecute(t);
        }

        if (parameter == null && default(T) == null)
        {
            return _canExecute == null || _canExecute(default!);
        }

        return false;
    }

    // Runs the action by casting the input parameter to the expected generic type.
    public void Execute(object? parameter)
    {
        if (parameter is T t)
        {
            _execute(t);
        }
        else if (parameter == null && default(T) == null)
        {
            _execute(default!);
        }
    }
}