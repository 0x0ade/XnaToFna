using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace XnaToFna.Forms {
    public class ProxyControl {

        internal static List<WeakReference<ProxyControl>> INTERNAL_AllControls = new List<WeakReference<ProxyControl>>();

        internal int _GlobalIndex;
        public IntPtr Handle {
            get {
                return (IntPtr) _GlobalIndex;
            }
        }

        public ProxyForm Form;

        public virtual Rectangle Bounds { get; set; }

        public ProxyControl() {
            _GlobalIndex = INTERNAL_AllControls.Count;
            XnaToFnaHelper.Log($"[ProxyForm] Creating ProxyControl {GetType().Name}, globally #{_GlobalIndex}");
            INTERNAL_AllControls.Add(new WeakReference<ProxyControl>(this));
        }

        public static ProxyControl FromHandle(IntPtr ptr) {
            WeakReference<ProxyControl> weakref = INTERNAL_AllControls[(int) ptr];
            ProxyControl control;
            if (weakref == null || !weakref.TryGetTarget(out control)) {
                INTERNAL_AllControls[(int) ptr] = null;
                return null;
            }
            return control;
        }

        public ProxyForm FindForm()
            => Form ?? ProxyForm.GameForm;

        public void SetBounds(int x, int y, int w, int h) {
            Bounds = new Rectangle(x, y, w, h);
        }

        // Some games use those

        protected virtual void CreateHandle() {
            // no-op
        }

        // Some games override those

        protected virtual void SetVisibleCore(bool visible) {
            // no-op
        }

        protected virtual void WndProc(ref ProxyMessage msg) {
            // no-op
        }

    }
}
