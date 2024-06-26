﻿using System.Composition;
using System.Windows;
using System.Windows.Threading;

using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(ITelemetryProvider)), Shared]
internal sealed class TelemetryProvider : TelemetryProviderBase
{
    public override void Initialize(string version, IApplicationSettings settings)
    {
        base.Initialize(version, settings);

        Application.Current.DispatcherUnhandledException += OnUnhandledDispatcherException;
    }

    private void OnUnhandledDispatcherException(object? sender, DispatcherUnhandledExceptionEventArgs args)
    {
        HandleException(args.Exception);
        args.Handled = true;
    }
}
