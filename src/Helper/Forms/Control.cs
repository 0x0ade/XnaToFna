using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace XnaToFna.ProxyForms {
    public class Control {

        internal static List<WeakReference<Control>> INTERNAL_AllControls = new List<WeakReference<Control>>();

        internal int _GlobalIndex;
        public IntPtr Handle {
            get {
                return (IntPtr) _GlobalIndex;
            }
        }

        public Form Form;

        public virtual Rectangle Bounds { get; set; }

        public Control() {
            _GlobalIndex = INTERNAL_AllControls.Count;
            XnaToFnaHelper.Log($"[ProxyForms] Creating control {GetType().Name}, globally #{_GlobalIndex}");
            INTERNAL_AllControls.Add(new WeakReference<Control>(this));
        }

        public static Control FromHandle(IntPtr ptr) {
            WeakReference<Control> weakref = INTERNAL_AllControls[(int) ptr];
            Control control;
            if (weakref == null || !weakref.TryGetTarget(out control)) {
                INTERNAL_AllControls[(int) ptr] = null;
                return null;
            }
            return control;
        }

        public Form FindForm()
            => Form ?? Form.GameForm;

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

        protected virtual void WndProc(ref Message msg) {
            // no-op
        }

    }
}
