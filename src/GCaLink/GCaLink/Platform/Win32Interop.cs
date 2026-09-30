using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GCaLink.Platform
{
    class Win32Interop
    {
        private const int GWL_EXSTYLE = -20;

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const uint LWA_ALPHA = 0x00000002;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr FindWindow(string? className, string windowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", ExactSpelling = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true)]
        private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint colorKey, byte alpha, uint flags);

        [DllImport("user32.dll")]
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

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLong64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
        {
            return IntPtr.Size == 8
                ? SetWindowLong64(hWnd, nIndex, value)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, value.ToInt32()));
        }

        public static void EnableClickThrough(IntPtr hwnd)
        {
            var styles = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            styles |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(styles));
        }

        public static void DisableClickThrough(IntPtr hwnd)
        {
            var styles = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            styles &= ~WS_EX_TRANSPARENT;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(styles));
        }

        public static bool SetWindowOpacity(IntPtr hwnd, byte opacity)
        {
            var styles = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            styles |= WS_EX_LAYERED;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(styles));
            return SetLayeredWindowAttributes(hwnd, 0, opacity, LWA_ALPHA);
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

    }
}
