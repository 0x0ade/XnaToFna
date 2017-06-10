using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using XnaToFna.ProxyDrawing;

namespace XnaToFna.ProxyForms {
    public sealed class Cursor : IDisposable {

        public static List<WeakReference<Cursor>> AllCursors = new List<WeakReference<Cursor>>();

        public int GlobalIndex;
        public IntPtr Handle {
            get {
                return (IntPtr) GlobalIndex;
            }
        }

        public static Cursor Current { get; set; } = new Cursor();

        public static Rectangle Clip {
            get {
                if (MouseEvents.Clip == null)
                    // TODO: Get screen size, somehow.
                    return new Rectangle();

                Microsoft.Xna.Framework.Rectangle value = MouseEvents.Clip.Value;
                return new Rectangle(
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height
                );
            }
            set {
                // TODO: Get screen size, somehow.
                Rectangle screen = new Rectangle();
                if (value == screen) {
                    MouseEvents.Clip = null;
                    return;
                }

                MouseEvents.Clip = new Microsoft.Xna.Framework.Rectangle(
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height
                );
            }
        }

        public static Point Position {
            get {
                Microsoft.Xna.Framework.Rectangle window = XnaToFnaHelper.Game.Window.ClientBounds;
                Microsoft.Xna.Framework.Input.MouseState state = Microsoft.Xna.Framework.Input.Mouse.GetState();
                return new Point(
                    state.X + window.X,
                    state.Y + window.Y
                );
            }
            set {
                Point current = Position;
                if (current == value)
                    return;

                Microsoft.Xna.Framework.Rectangle window = XnaToFnaHelper.Game.Window.ClientBounds;
                Microsoft.Xna.Framework.Input.Mouse.SetPosition(
                    value.X - window.X,
                    value.Y - window.Y
                );
            }
        }

        public Point HotSpot { get; internal set; }

        // No. Just. No.
        // TODO: Implement ProxyDrawing.Size with all its relevant parts in PD.Point and PD.Rectangle...
        // public Size Size { get; internal set; }

        public object Tag { get; set; }

        internal bool INTERNAL_IsNullCursor = false;

        private bool _IsDisposed = false;

        private Cursor() {
            GlobalIndex = AllCursors.Count + 1;
            XnaToFnaHelper.Log($"[ProxyForms] Creating null cursor, globally #{GlobalIndex}");
            INTERNAL_IsNullCursor = true;
            AllCursors.Add(new WeakReference<Cursor>(this));
        }

        public Cursor(Type type, string resource) {
            throw new NotSupportedException("Loading cursors from resources currently not supported!");
        }

        public Cursor(IntPtr handle) {
            GlobalIndex = AllCursors.Count + 1;
            XnaToFnaHelper.Log($"[ProxyForms] Creating reapplied cursor from #{handle}, globally #{GlobalIndex}");
            _Apply(_FromHandle(handle));
            AllCursors.Add(new WeakReference<Cursor>(this));
        }

        public Cursor(string fileName) {
            GlobalIndex = AllCursors.Count + 1;
            XnaToFnaHelper.Log($"[ProxyForms] Creating cursor from file, globally #{GlobalIndex}");
            using (Stream stream = File.OpenRead(fileName))
                _Load(stream);
            AllCursors.Add(new WeakReference<Cursor>(this));
        }
        public Cursor(Stream stream) {
            GlobalIndex = AllCursors.Count + 1;
            XnaToFnaHelper.Log($"[ProxyForms] Creating cursor from stream, globally #{GlobalIndex}");
            _Load(stream);
            AllCursors.Add(new WeakReference<Cursor>(this));
        }

        private static Cursor _FromHandle(IntPtr ptr) {
            int index = (int) ptr - 1;
            if (index < 0 || AllCursors.Count <= index)
                return null;
            WeakReference<Cursor> weakref = AllCursors[index];
            Cursor cursor;
            if (weakref == null || !weakref.TryGetTarget(out cursor)) {
                AllCursors[index] = null;
                return null;
            }
            return cursor;
        }


        private void _Apply(Cursor other) {
            if (other == null) {
                INTERNAL_IsNullCursor = true;
                return;
            }
        }

        private void _Load(Stream stream) {
            // TODO: .ani / .cur loader
        }


        public void Dispose()
            => Dispose(true);
        private void Dispose(bool disposing) {
            if (_IsDisposed)
                return;

            // no-op

            _IsDisposed = true;
        }

    }
}
