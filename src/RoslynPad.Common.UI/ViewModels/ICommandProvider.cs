﻿using System.Composition;

using RoslynPad.Utilities;

namespace RoslynPad.UI;

public interface ICommandProvider
{
    IDelegateCommand Create(Action execute, Func<bool>? canExecute = null);
    IDelegateCommand<T> Create<T>(Action<T?> execute, Func<T?, bool>? canExecute = null);
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    IDelegateCommand CreateAsync(Func<Task> execute, Func<bool>? canExecute = null);
    IDelegateCommand<T> CreateAsync<T>(Func<T?, Task> execute, Func<T?, bool>? canExecute = null);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
}

[Export(typeof(ICommandProvider)), Shared]
internal class CommandProvider : ICommandProvider
{
    public IDelegateCommand Create(Action execute, Func<bool>? canExecute = null)
    {
        return new DelegateCommand(execute, canExecute);
    }

    public IDelegateCommand<T> Create<T>(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        return new DelegateCommand<T>(execute, canExecute);
    }

    public IDelegateCommand CreateAsync(Func<Task> execute, Func<bool>? canExecute = null)
    {
        return new DelegateCommand(execute, canExecute);
    }

    public IDelegateCommand<T> CreateAsync<T>(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        return new DelegateCommand<T>(execute, canExecute);
    }
}
