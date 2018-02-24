using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using XnaToFna.ProxyDrawing;

namespace XnaToFna.ProxyForms {
    public class Control : IDisposable {

        public static List<WeakReference<Control>> AllControls = new List<WeakReference<Control>>();

        public int GlobalIndex;
        public IntPtr Handle {
            get {
                return (IntPtr) GlobalIndex;
            }
        }

        public static Point MousePosition {
            get {
                // The MousePosition property is identical to the Cursor.Position property. --MSDN
                return Cursor.Position;
            }
        }

        public Form Form;

        public virtual Rectangle Bounds { get; set; }

        protected virtual Rectangle _ClientRectangle { get; set; }
        public Rectangle ClientRectangle {
            get {
                return _ClientRectangle;
            }
        }

        public virtual Point Location { get; set; }

        public virtual Cursor Cursor { get; set; }

        public virtual bool Focused { get; protected set; }

        /* MSDN:
         * Mouse events occur in the following order:
         * MouseEnter
         * MouseMove
         * MouseHover / MouseDown / MouseWheel
         * MouseUp
         * MouseLeave
         */

        // TODO: Fire them?

        public event EventHandler MouseEnter;

        public event MouseEventHandler MouseMove;

        public event EventHandler MouseHover;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseWheel;

        public event MouseEventHandler MouseUp;

        public event EventHandler MouseLeave;

        protected bool _IsDisposed = false;
        public bool IsDisposed {
            get {
                return _IsDisposed;
            }
        }

        public Control() {
            GlobalIndex = AllControls.Count + 1;
            XnaToFnaHelper.Log($"[ProxyForms] Creating control {GetType().Name}, globally #{GlobalIndex}");
            AllControls.Add(new WeakReference<Control>(this));
        }

        public static Control FromHandle(IntPtr ptr) {
            int index = (int) ptr - 1;
            if (index < 0 || AllControls.Count <= index)
                return null;
            WeakReference<Control> weakref = AllControls[index];
            Control control;
            if (weakref == null || !weakref.TryGetTarget(out control)) {
                AllControls[index] = null;
                return null;
            }
            return control;
        }

        public Form FindForm()
            => Form ?? GameForm.Instance;

        public void SetBounds(int x, int y, int w, int h) {
            Bounds = new Rectangle(x, y, w, h);
        }

        // Some games use those

        protected virtual void CreateHandle() {
            // no-op
        }

        // TODO: The delegate passed in Control.Invoke should be invoked on the main thread

        public object Invoke(Delegate method) {
            return method.DynamicInvoke();
        }

        public object Invoke(Delegate method, params object[] args) {
            return method.DynamicInvoke(args);
        }

        public IAsyncResult BeginInvoke(Delegate method) {
            return new SyncResult(method.DynamicInvoke());
        }

        public IAsyncResult BeginInvoke(Delegate method, params object[] args) {
            return new SyncResult(method.DynamicInvoke(args));
        }

        public object EndInvoke(IAsyncResult result) {
            return result.AsyncState;
        }

        public Rectangle RectangleToScreen(Rectangle r) {
            // This probably isn't correct.
            Rectangle bounds = Bounds;
            return new Rectangle(
                r.X + bounds.X,
                r.Y + bounds.Y,
                r.Width,
                r.Height
            );
        }

        public Rectangle RectangleToClient(Rectangle r) {
            // This probably isn't correct.
            Rectangle bounds = Bounds;
            return new Rectangle(
                r.X - bounds.X,
                r.Y - bounds.Y,
                r.Width,
                r.Height
            );
        }

        public void Dispose()
            => Dispose(true);
        protected virtual void Dispose(bool disposing) {
            if (_IsDisposed)
                return;

            // no-op

            _IsDisposed = true;
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
