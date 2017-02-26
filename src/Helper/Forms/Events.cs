using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace XnaToFna.Forms {

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

}
