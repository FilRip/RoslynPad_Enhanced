namespace RoslynPad.UI;

public interface IAppDispatcher
{
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    void InvokeAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

    Task InvokeTaskAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default);
}

public enum AppDispatcherPriority
{
    Normal,
    High,
    Low,
}
