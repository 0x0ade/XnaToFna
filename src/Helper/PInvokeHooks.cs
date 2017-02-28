using Microsoft.Xna.Framework;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using XnaToFna.ProxyForms;

namespace XnaToFna {
    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    public static class PInvokeHelper {
        public static int MessageSize = Marshal.SizeOf(typeof(Message));

        // Global hooks added by SetWindowsHookEx.
        // Delegate is required as the actual type stems from the game and differs from HookProc.
        public static Dictionary<HookType, List<Delegate>> Hooks = new Dictionary<HookType, List<Delegate>>();
        // Required to properly keep track of the hook handle and to efficiently unregister hooks.
        public static List<Tuple<HookType, Delegate, int>> AllHooks = new List<Tuple<HookType, Delegate, int>>();
        public static ThreadLocal<List<Delegate>> CurrentHookChain = new ThreadLocal<List<Delegate>>();
        public static ThreadLocal<int> CurrentHookIndex = new ThreadLocal<int>();

        static PInvokeHelper() {
            Array hookTypes = Enum.GetValues(typeof(HookType));
            foreach (HookType hookType in hookTypes)
                Hooks[hookType] = new List<Delegate>();
        }

        public static void CallHooks(Messages Msg, IntPtr wParam, IntPtr lParam, bool global = true, bool window = true, bool allWindows = false)
            => CallHooks(Msg, wParam, new Message() {
                HWnd = IntPtr.Zero, // What do we give global hooks?
                Msg = (int) Msg,
                WParam = wParam,
                LParam = lParam
            }, global: global, window: window, allWindows: allWindows);
        public static void CallHooks(Messages Msg, IntPtr wParam, Message lParamMsg, bool global = true, bool window = true, bool allWindows = false) {
            IntPtr lParam = Marshal.AllocHGlobal(MessageSize);
            Marshal.StructureToPtr(lParamMsg, lParam, false);
            CallHooks(Msg, wParam, lParam, lParamMsg: ref lParamMsg, global: global, window: window, allWindows: allWindows);
            Marshal.FreeHGlobal(lParam);
        }
        public static void CallHooks(Messages Msg, IntPtr wParam, IntPtr lParam, ref Message lParamMsg, bool global = true, bool window = true, bool allWindows = false) {
            // Order of hook calling?
            if (global) {
                // WH_GETMESSAGE seems to await 1 as wParam and a message struct as lParam.
                CallHookChain(HookType.WH_GETMESSAGE, (IntPtr) 1, lParam, ref lParamMsg);
                // TODO CallHooks should handle most HookTypes.
            }

            if (allWindows) {
                for (int i = 0; i < Control.AllControls.Count; i++)
                    // The global index + 1 also functions as the control handle.
                    lParamMsg.Result = CallWindowHook((IntPtr) (i + 1), Msg, wParam, lParam);
            } else if (window)
                lParamMsg.Result = CallWindowHook(Msg, wParam, lParam);

            return;
        }

        public static IntPtr CallHookChain(HookType hookType, IntPtr wParam, IntPtr lParam, ref Message lParamMsg) {
            List<Delegate> hooks = Hooks[hookType];
            if (hooks.Count == 0)
                return IntPtr.Zero;
            CurrentHookChain.Value = hooks;
            for (int i = 0; i < hooks.Count; i++) {
                Delegate hook = hooks[i];
                // Find the first non-null (still registered) hook.
                if (hook == null)
                    continue;
                CurrentHookIndex.Value = i;
                // HookProc expects HC_ACTION (0; take action) or < 0 (pass to next hook).
                object[] args = { 0, wParam, lParamMsg };
                object result = hook.DynamicInvoke(args);
                lParamMsg = (Message) args[2];
                return result != null ? (IntPtr) Convert.ToInt32(result) : IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        public static IntPtr ContinueHookChain(int nCode, IntPtr wParam, IntPtr lParam) {
            List<Delegate> hooks = CurrentHookChain.Value;
            for (int i = CurrentHookIndex.Value + 1; i < hooks.Count; i++) {
                Delegate hook = hooks[i];
                // Find the next non-null (still registered) hook.
                if (hook == null)
                    continue;
                CurrentHookIndex.Value = i;
                return (IntPtr) hook.DynamicInvoke(nCode < 0 ? nCode + 1 : 0, wParam, lParam);
            }
            // End of chain. What should we return here?
            return IntPtr.Zero;
        }

        public static IntPtr CallWindowHook(Messages Msg, IntPtr wParam, IntPtr lParam)
            => CallWindowHook(GameForm.Instance?.Handle ?? IntPtr.Zero, (uint) Msg, wParam, lParam);
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

        public static IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId) {
            // TODO SetWindowsHookEx currently ignores the module and thread.
            int handle = PInvokeHelper.AllHooks.Count + 1;
            List<Delegate> hooks = PInvokeHelper.Hooks[hookType];
            PInvokeHelper.AllHooks.Add(Tuple.Create<HookType, Delegate, int>(hookType, lpfn, hooks.Count));
            hooks.Add(lpfn);
            XnaToFnaHelper.Log($"[PInvokeHooks] Added global hook #{handle} of type {hookType}");
            return (IntPtr) handle;
        }

        public static bool UnhookWindowsHookEx(IntPtr hhk) {
            int index = (int) hhk - 1;
            if (index < 0 || PInvokeHelper.Hooks.Count <= index ||
                PInvokeHelper.AllHooks[index] == null)
                return true; // Too lazy to implement Set/GetLastError with Windows' 16000 error codes...

            Tuple<HookType, Delegate, int> hook = PInvokeHelper.AllHooks[index];
            PInvokeHelper.AllHooks[index] = null;
            PInvokeHelper.Hooks[hook.Item1].RemoveAt(hook.Item3);
            return true;
        }

        public static IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)
            => PInvokeHelper.ContinueHookChain(nCode, wParam, lParam);

        public static bool TranslateMessage(ref Message m) {
            // Currently, all messages are in their pure form.
            // This may become required as soon as something requires the message (lParam) before translation.
            return true;
        }

        public static uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId) {
            Form form = Control.FromHandle(hWnd) as Form;
            if (form == null) {
                XnaToFnaHelper.Log($"[PInvokeHooks] Called GetWindowThreadProcessId for non-existing hWnd {hWnd}");
                form = GameForm.Instance;
            }
            // Yes, that's required, as DLC Quest passes IntPtr.Zero
            unsafe {
                fixed (uint* lpdwProcesId_ = &lpdwProcessId)
                    if ((int) lpdwProcesId_ != 0)
                        lpdwProcessId = 0; // Optional
            }
            return (uint) (form?.ThreadId ?? 0);
        }

    }

}
