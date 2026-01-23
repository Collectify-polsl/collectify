using System;
using System.Windows.Input;

namespace Collectify.App.Commands;

// Implements a basic command pattern that triggers synchronous actions via UI interactions.
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Tworzy nową komendę.
    /// </summary>
    /// <param name="execute">Akcja do wykonania (np. OtworzOkno).</param>
    /// <param name="canExecute">Opcjonalna funkcja sprawdzająca, czy przycisk ma być aktywny.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Checks the custom logic to see if the command is currently allowed to run.
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    // Executes the assigned action synchronously.
    public void Execute(object? parameter)
    {
        _execute();
    }

    public event EventHandler? CanExecuteChanged;

    // Triggers a refresh of the command's executable state in the UI.
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}