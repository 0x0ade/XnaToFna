using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public class EffectPool : IDisposable {

        public bool IsDisposed { get; internal set; }

        public event EventHandler Disposing;

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            IsDisposed = true;
            raise_Disposing(this, new EventArgs());
        }

        protected void raise_Disposing(object sender, EventArgs e) {
            Disposing(sender, e);
        }

    }
}
