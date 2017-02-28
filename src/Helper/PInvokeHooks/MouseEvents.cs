using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.InlineRT;
using SDL2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using XnaToFna.ProxyForms;

namespace XnaToFna {
    public static class MouseEvents {

        public static Rectangle? Clip;
        public static MouseState PreviousState;

        public static void Update() {
            MouseState state = Mouse.GetState();

            if (Clip != null) {
                // Manually clip "inside" the window
                Rectangle window = XnaToFnaHelper.Game.Window.ClientBounds;
                int mx = state.X + window.X;
                int my = state.Y + window.Y;
                Rectangle clip = Clip.Value;
                if (mx < clip.Left)
                    mx = clip.Left;
                else if (clip.Right <= mx)
                    mx = clip.Right;
                if (my < clip.Top)
                    my = clip.Top;
                else if (clip.Bottom <= my)
                    my = clip.Bottom;
                mx -= window.X;
                my -= window.Y;
                if (mx != state.X || my != state.Y)
                    Mouse.SetPosition(mx, my);
            }

            PreviousState = Mouse.GetState();
        }

    }

    public static partial class PInvokeHooks {

        public static bool GetClipCursor(ref Rectangle rect) {
            // Who knows what games may pass in here after DLC Quest gave us a null ref...
            unsafe
            {
                fixed (Rectangle* rect_ = &rect)
                    if ((long) rect_ == 0)
                        return true; // Too lazy for Set/GetLastError again.
            }

            if (Mouse.IsRelativeMouseModeEXT) {
                rect = XnaToFnaHelper.Game.Window.ClientBounds;
            } else {
                // MSDN: The structure receives the dimensions of the screen if the cursor is not confined to a rectangle.
                DisplayMode dm = XnaToFnaHelper.Game.GraphicsDevice.Adapter.CurrentDisplayMode;
                rect = new Rectangle(0, 0, dm.Width, dm.Height);
            }
            return true;
        }

        public static bool ClipCursor(ref Rectangle rect) {
            unsafe
            {
                fixed (Rectangle* rect_ = &rect)
                    if ((long) rect_ == 0) {
                        // MSDN: If this parameter is NULL, the cursor is free to move anywhere on the screen.
                        XnaToFnaHelper.Log($"[CursorEvents] Cursor released from ClipCursor");
                        Mouse.IsRelativeMouseModeEXT = false;
                        MouseEvents.Clip = null;
                        return true;
                    }
            }

            XnaToFnaHelper.Log($"[CursorEvents] Game tries to ClipCursor inside {rect}");
            Mouse.IsRelativeMouseModeEXT = XnaToFnaHelper.Game.Window.ClientBounds.Contains(rect);
            MouseEvents.Clip = rect;
            return true;
        }

    }
}
