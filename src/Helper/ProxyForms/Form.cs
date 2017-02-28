using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Threading;

namespace XnaToFna.ProxyForms {
    public class Form : Control {

        // If something using ProxyForms wants to change the hook directly: Feel free to!
        public IntPtr WindowHookPtr;
        public Delegate WindowHook;

        // Used for GetWindowThreadProcessId
        public int ThreadId;

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
            ThreadId = Thread.CurrentThread.ManagedThreadId;
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
