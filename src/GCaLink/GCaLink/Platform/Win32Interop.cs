using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GCaLink.Services;

namespace GCaLink.Platform
{
    class Win32Interop
    {
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const uint LWA_ALPHA = 0x00000002;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr FindWindow(string? className, string windowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true, SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", ExactSpelling = true, SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint colorKey, byte alpha, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private static bool TryGetWindowLongPtr(IntPtr hWnd, int nIndex, out long value)
        {
            Marshal.SetLastPInvokeError(0);
            IntPtr result = IntPtr.Size == 8
                ? GetWindowLong64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
            value = result.ToInt64();
            return result != IntPtr.Zero || Marshal.GetLastPInvokeError() == 0;
        }

        private static bool TrySetWindowLongPtr(IntPtr hWnd, int nIndex, long value)
        {
            Marshal.SetLastPInvokeError(0);
            IntPtr previousValue = IntPtr.Size == 8
                ? SetWindowLong64(hWnd, nIndex, new IntPtr(value))
                : new IntPtr(SetWindowLong32(hWnd, nIndex, unchecked((int)value)));
            return previousValue != IntPtr.Zero || Marshal.GetLastPInvokeError() == 0;
        }

        public static void EnableClickThrough(IntPtr hwnd)
        {
            if (TryGetWindowLongPtr(hwnd, GWL_EXSTYLE, out long styles))
            {
                styles |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                TrySetWindowLongPtr(hwnd, GWL_EXSTYLE, styles);
            }
        }

        public static void DisableClickThrough(IntPtr hwnd)
        {
            if (TryGetWindowLongPtr(hwnd, GWL_EXSTYLE, out long styles))
            {
                styles &= ~WS_EX_TRANSPARENT;
                TrySetWindowLongPtr(hwnd, GWL_EXSTYLE, styles);
            }
        }

        public static bool SetWindowOpacity(IntPtr hwnd, byte opacity)
        {
            if (!TryGetWindowLongPtr(hwnd, GWL_EXSTYLE, out long styles))
            {
                return false;
            }

            styles |= WS_EX_LAYERED;
            return TrySetWindowLongPtr(hwnd, GWL_EXSTYLE, styles) &&
                SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
        }

        public static void BringToFront(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        public static bool TryActivateWindowByTitle(string title)
        {
            IntPtr hwnd = FindWindow(null, title);
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
            return true;
        }

        public static void RemoveTopMost(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        public static bool SetDesktopWidgetMode(IntPtr hwnd, bool widgetManagerActive)
        {
            if (!TryGetWindowLongPtr(hwnd, GWL_EXSTYLE, out long extendedStyles) ||
                !TryGetWindowLongPtr(hwnd, GWL_STYLE, out long windowStyles))
            {
                return false;
            }

            if (widgetManagerActive)
            {
                extendedStyles &= ~(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                windowStyles |= WS_CAPTION | WS_THICKFRAME;
            }
            else
            {
                extendedStyles |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                windowStyles &= ~(WS_CAPTION | WS_THICKFRAME);
            }
            windowStyles &= ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX);

            if (!TrySetWindowLongPtr(hwnd, GWL_EXSTYLE, extendedStyles) ||
                !TrySetWindowLongPtr(hwnd, GWL_STYLE, windowStyles))
            {
                return false;
            }

            bool positioned = SetWindowPos(
                hwnd,
                widgetManagerActive ? HWND_TOP : HWND_NOTOPMOST,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            if (!positioned)
            {
                LoggerService.Log(
                    $"Win32Interop: Failed to position hwnd=0x{hwnd.ToInt64():X} for widget manager mode active={widgetManagerActive}.",
                    LoggerStatusEnum.WARNING);
            }
            return positioned;
        }

        public static bool TrySetWidgetManagerActiveByTitle(string title, bool widgetManagerActive)
        {
            IntPtr hwnd = FindWindow(null, title);
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(hwnd, out uint ownerProcessId);
            if (!SetDesktopWidgetMode(hwnd, widgetManagerActive))
            {
                LoggerService.Log(
                    $"Win32Interop: Found window title='{title}', hwnd=0x{hwnd.ToInt64():X}, ownerProcess={ownerProcessId}, but failed to apply managerActive={widgetManagerActive}.",
                    LoggerStatusEnum.WARNING);
                return false;
            }

            LoggerService.Log(
                $"Win32Interop: Managed existing window title='{title}', hwnd=0x{hwnd.ToInt64():X}, ownerProcess={ownerProcessId}, managerActive={widgetManagerActive}.");
            if (widgetManagerActive)
            {
                ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
            }

            return true;
        }

    }
}
