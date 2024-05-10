using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RoslynPad;

public static partial class WindowExtensions
{
    public static void UseImmersiveDarkMode(this Window window, bool value)
    {
        IntPtr hwnd = IntPtr.Zero;
        window.Dispatcher.Invoke(() =>
        {
            hwnd = new WindowInteropHelper(window).EnsureHandle();
        });
        
        if (hwnd != IntPtr.Zero)
        {
            var error = DwmSetWindowAttribute(
                hwnd,
                DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                value,
                Marshal.SizeOf<bool>());

            if (error != 0)
            {
                throw new Win32Exception(error);
            }
        }
    }

    [LibraryImport("dwmapi")]
    private static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        DwmWindowAttribute attribute,
        [MarshalAs(UnmanagedType.Bool)] in bool pvAttribute,
        int cbAttribute);

    private enum DwmWindowAttribute
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
    }
}
