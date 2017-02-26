using Microsoft.Xna.Framework;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.Forms;

namespace XnaToFna {
    public static partial class PInvokeHooks {

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static IntPtr WindowHookPtr;
        public static Delegate WindowHook;

        public static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong) {
            // hWnd can be freely ignored as it's ProxyControl.Form.Handle anyway
            // All other nIndex values seem to be style-related.
            if (nIndex == -4) {
                IntPtr prevHook = WindowHookPtr;
                WindowHook = Marshal.GetDelegateForFunctionPointer((IntPtr) dwNewLong, typeof(MulticastDelegate));
                XnaToFnaHelper.Log("[PInvokeHooks] Window hook set.");
                return (int) prevHook;
            }

            return 0;
        }

        public static IntPtr CallWindowHook(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
            => (IntPtr) WindowHook.DynamicInvoke(hWnd, Msg, wParam, lParam);

        public static IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) {
            if (lpPrevWndFunc == IntPtr.Zero)
                return IntPtr.Zero;

            return (IntPtr) Marshal.GetDelegateForFunctionPointer(lpPrevWndFunc, typeof(MulticastDelegate))
                .DynamicInvoke(hWnd, Msg, wParam, lParam);
        }

    }

}
