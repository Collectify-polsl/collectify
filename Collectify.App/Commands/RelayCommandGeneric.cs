using System;
using System.Windows.Input;

namespace Collectify.App.Commands;

/// <summary>
/// A generic command that takes a parameter of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the parameter (e.g., ColumnItem, string, int).</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The action to execute with the parameter.</param>
    /// <param name="canExecute">The function that determines whether the command can execute.</param>
    public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        // 1. If parameter is T, check condition
        if (parameter is T t)
        {
            return _canExecute == null || _canExecute(t);
        }

        // 2. If parameter is null and T allows nulls, check condition
        if (parameter == null && default(T) == null)
        {
            return _canExecute == null || _canExecute(default!);
        }

        return false;
    }

    /// <inheritdoc />
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