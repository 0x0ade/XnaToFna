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

        public static void Moved()
            => PInvoke.CallHooks(Messages.WM_MOUSEMOVE, IntPtr.Zero, IntPtr.Zero);

        public static void LMBDown()
            => PInvoke.CallHooks(Messages.WM_LBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
        public static void LMBUp()
            => PInvoke.CallHooks(Messages.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);

        public static void RMBDown()
            => PInvoke.CallHooks(Messages.WM_RBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
        public static void RMBUp()
            => PInvoke.CallHooks(Messages.WM_RBUTTONUP, IntPtr.Zero, IntPtr.Zero);

        public static void MMBDown()
            => PInvoke.CallHooks(Messages.WM_MBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
        public static void MMBUp()
            => PInvoke.CallHooks(Messages.WM_MBUTTONUP, IntPtr.Zero, IntPtr.Zero);

        public static void XMBDown(int mb)
            => PInvoke.CallHooks(Messages.WM_XBUTTONDOWN, (IntPtr) (mb << 16), IntPtr.Zero);
        public static void XMBUp(int mb)
            => PInvoke.CallHooks(Messages.WM_XBUTTONUP, (IntPtr) (mb << 16), IntPtr.Zero);

        public static void Wheel(int scroll)
            // Damn, FNA is even accurate to WHEEL_DELTA being 120!
            => PInvoke.CallHooks(Messages.WM_MOUSEWHEEL, (IntPtr) (scroll << 16), IntPtr.Zero);

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
                state = Mouse.GetState();
            }

            if (state.X != PreviousState.X ||
                state.Y != PreviousState.Y)
                Moved();

            if (state.LeftButton == ButtonState.Pressed && PreviousState.LeftButton == ButtonState.Released)
                LMBDown();
            else if (state.LeftButton == ButtonState.Released && PreviousState.LeftButton == ButtonState.Pressed)
                LMBUp();

            if (state.RightButton == ButtonState.Pressed && PreviousState.RightButton == ButtonState.Released)
                RMBDown();
            else if (state.RightButton == ButtonState.Released && PreviousState.RightButton == ButtonState.Pressed)
                RMBUp();

            if (state.MiddleButton == ButtonState.Pressed && PreviousState.MiddleButton == ButtonState.Released)
                MMBDown();
            else if (state.MiddleButton == ButtonState.Released && PreviousState.MiddleButton == ButtonState.Pressed)
                MMBUp();

            if (state.XButton1 == ButtonState.Pressed && PreviousState.XButton1 == ButtonState.Released)
                XMBDown(1);
            else if (state.XButton1 == ButtonState.Released && PreviousState.XButton1 == ButtonState.Pressed)
                XMBUp(1);

            if (state.XButton2 == ButtonState.Pressed && PreviousState.XButton2 == ButtonState.Released)
                XMBDown(2);
            else if (state.XButton2 == ButtonState.Released && PreviousState.XButton2 == ButtonState.Pressed)
                XMBUp(2);

            if (state.ScrollWheelValue != PreviousState.ScrollWheelValue)
                Wheel(state.ScrollWheelValue - PreviousState.ScrollWheelValue);

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

            if (MouseEvents.Clip != null) {
                rect = MouseEvents.Clip.Value;
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
                        MouseEvents.Clip = null;
                        return true;
                    }
            }

            XnaToFnaHelper.Log($"[CursorEvents] Game tries to ClipCursor inside {rect}");
            MouseEvents.Clip = rect;
            return true;
        }

    }
}
