using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;

namespace XnaToFna.ProxyForms {
    public class Form : Control {

        private const uint SDL_WINDOW_FULLSCREEN_DESKTOP_ONLY = 0x00001000;

        // If something using ProxyForms wants to change the hook directly: Feel free to!
        public IntPtr WindowHookPtr;
        public Delegate WindowHook;

        public override System.Drawing.Rectangle Bounds {
            get; set;
        }

        public virtual FormBorderStyle FormBorderStyle {
            get; set;
        }

        public virtual FormWindowState WindowState {
            get; set;
        }


        public Form() {
            Form = this;
        }

        public event FormClosingEventHandler FormClosing;
        public event FormClosedEventHandler FormClosed;
        protected virtual void OnFormClosing(FormClosingEventArgs e) {
        }
        protected virtual void OnFormClosed(FormClosedEventArgs e) {
        }
        public void Close() {
            FormClosingEventArgs closingArgs = new FormClosingEventArgs(CloseReason.None, false);
            OnFormClosing(closingArgs);
            FormClosing(this, closingArgs);

            FormClosedEventArgs closedArgs = new FormClosedEventArgs(CloseReason.None);
            OnFormClosed(closedArgs);
            FormClosed(this, closedArgs);
        }

        protected override void WndProc(ref Message msg)
            => msg.Result = (IntPtr) WindowHook?.DynamicInvoke(msg.HWnd, msg.Msg, msg.WParam, msg.LParam);

    }
}
