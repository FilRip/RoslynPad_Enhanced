using System.Runtime.InteropServices;

namespace RoslynPad.Runtime;

internal static partial class WindowsNativeMethods
{
    internal static void DisableWer()
    {
        if (Environment.OSVersion.Version < new Version(6, 1, 0, 0))
        {
            return;
        }

        SetErrorMode(GetErrorMode() |
            ErrorMode.SEM_FAILCRITICALERRORS |
            ErrorMode.SEM_NOOPENFILEERRORBOX |
            ErrorMode.SEM_NOGPFAULTERRORBOX);
    }

    [LibraryImport("kernel32.dll")]
    private static partial ErrorMode SetErrorMode(ErrorMode mode);

    [LibraryImport("kernel32.dll")]
    private static partial ErrorMode GetErrorMode();

    [Flags()]
    private enum ErrorMode
    {
        SEM_FAILCRITICALERRORS = 0x0001,
        SEM_NOGPFAULTERRORBOX = 0x0002,
        SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
        SEM_NOOPENFILEERRORBOX = 0x8000,
    }
}
