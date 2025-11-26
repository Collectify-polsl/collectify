using System;
using System.Windows.Input;

namespace Collectify.App.Commands;

/// <summary>
/// Generyczna wersja komendy, która przyjmuje parametr typu T.
/// Używana np. przy usuwaniu konkretnego wiersza z listy: RelayCommand<ColumnItem>
/// </summary>
/// <typeparam name="T">Typ danych przekazywanych z widoku (np. ColumnItem, string, int).</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

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

    public bool CanExecute(object? parameter)
    {
        // 1. Jeśli parametr jest zgodny z typem T, sprawdzamy warunek
        if (parameter is T t)
        {
            return _canExecute == null || _canExecute(t);
        }

        // 2. Jeśli parametr jest null, a typ T pozwala na nulle (jest klasą), też sprawdzamy
        if (parameter == null && default(T) == null)
        {
            return _canExecute == null || _canExecute(default!);
        }

        // W przeciwnym razie blokujemy przycisk
        return false;
    }

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