using Microsoft.Xna.Framework;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.ProxyForms;

namespace XnaToFna {
    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    public static partial class PInvokeHooks {

        public static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong) {
            // All other nIndex values seem to be style-related.
            if (nIndex == -4) {
                Form form = Control.FromHandle(hWnd)?.Form;
                if (form == null)
                    return 0;

                IntPtr prevHook = form.WindowHookPtr;
                form.WindowHookPtr = (IntPtr) dwNewLong;
                form.WindowHook = Marshal.GetDelegateForFunctionPointer(form.WindowHookPtr, typeof(WndProc));
                XnaToFnaHelper.Log($"[PInvokeHooks] Window hook set on ProxyForms.Form #{form.GlobalIndex}");
                return (int) prevHook;
            }

            return 0;
        }

        public static IntPtr CallWindowHook(Messages Msg, IntPtr wParam, IntPtr lParam)
            => CallWindowHook((uint) Msg, wParam, lParam);
        public static IntPtr CallWindowHook(uint Msg, IntPtr wParam, IntPtr lParam)
            => CallWindowHook(GameForm.Instance?.Handle ?? IntPtr.Zero, Msg, wParam, lParam);
        public static IntPtr CallWindowHook(IntPtr hWnd, Messages Msg, IntPtr wParam, IntPtr lParam) 
            => CallWindowHook(hWnd, (uint) Msg, wParam, lParam);
        public static IntPtr CallWindowHook(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) {
            Form form = Control.FromHandle(hWnd) as Form;
            if (form == null || form.WindowHookPtr == IntPtr.Zero)
                return IntPtr.Zero;
            return (IntPtr) form.WindowHook.DynamicInvoke(hWnd, Msg, wParam, lParam);
        }

        public static IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) {
            if (lpPrevWndFunc == IntPtr.Zero)
                return IntPtr.Zero;
            return (IntPtr) Marshal.GetDelegateForFunctionPointer(lpPrevWndFunc, typeof(MulticastDelegate))
                .DynamicInvoke(hWnd, Msg, wParam, lParam);
        }

    }

}
