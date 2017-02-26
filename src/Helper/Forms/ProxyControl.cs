using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace XnaToFna.Forms {
    public class ProxyControl {

        public static ProxyForm Form;

        public virtual Rectangle Bounds { get; set; }

        public IntPtr Handle {
            get {
                return IntPtr.Zero;
            }
        }

        public ProxyControl() {
            // Some games create their own forms, overriding some methods.
            // Handle the control's WndProc accordingly.
            WndProc wndProc = new WndProc(INTERNAL_WndProc);
            INTERNAL_WndProc_Ptr = Marshal.GetFunctionPointerForDelegate(wndProc);
            INTERNAL_WndProc_Prev = PInvokeHooks.WindowHook;
            PInvokeHooks.WindowHook = wndProc;
            PInvokeHooks.WindowHookPtr = INTERNAL_WndProc_Ptr;
        }

        public static ProxyControl FromHandle(IntPtr ptr)
            => Form == null ? Form = new ProxyForm() : Form;

        public ProxyForm FindForm()
            => Form == null ? Form = new ProxyForm() : Form;

        public void SetBounds(int x, int y, int w, int h) {
            Bounds = new Rectangle(x, y, w, h);
        }

        // Some games override those

        protected virtual void SetVisibleCore(bool visible) {

        }

        protected virtual void WndProc(ref ProxyMessage msg) {

        }

        // Used for PInvokeHooks
        private Delegate INTERNAL_WndProc_Prev;
        private IntPtr INTERNAL_WndProc_Ptr;
        private IntPtr INTERNAL_WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            ProxyMessage msg_ = ProxyMessage.Create(hWnd, (int) msg, wParam, lParam);
            if (INTERNAL_WndProc_Prev != null)
                msg_.Result = (IntPtr) INTERNAL_WndProc_Prev.DynamicInvoke(hWnd, msg, wParam, lParam);
            WndProc(ref msg_);
            return msg_.Result;
        }

    }
}
