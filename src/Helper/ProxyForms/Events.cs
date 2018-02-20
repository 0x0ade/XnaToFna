using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace XnaToFna.ProxyForms {

    public delegate void FormClosingEventHandler(object sender, FormClosingEventArgs e);
    public class FormClosingEventArgs : CancelEventArgs {
        public CloseReason CloseReason { get; private set; }
        public FormClosingEventArgs(CloseReason closeReason, bool cancel)
            : base(cancel) {
            CloseReason = closeReason;
        }
    }

    public delegate void FormClosedEventHandler(object sender, FormClosedEventArgs e);
    public class FormClosedEventArgs : CancelEventArgs {
        public CloseReason CloseReason { get; private set; }
        public FormClosedEventArgs(CloseReason closeReason) {
            CloseReason = closeReason;
        }
    }

    public delegate void MouseEventHandler(object sender, MouseEventArgs e);
    public class MouseEventArgs : EventArgs {
        public MouseButtons Button { get; private set; }
        public int Clicks { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Delta { get; private set; }
        public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta) {
            Button = button;
            Clicks = clicks;
            X = x;
            Y = y;
            Delta = delta;
        }

    }

}
