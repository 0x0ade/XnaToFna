using System;
using System.Collections.Generic;
using System.Drawing;

namespace XnaToFna.Forms {
    public class ProxyControl {

        public static ProxyForm Form = new ProxyForm();

        public virtual Rectangle Bounds { get; set; }

        public IntPtr Handle {
            get {
                return IntPtr.Zero;
            }
        }

        public static ProxyControl FromHandle(IntPtr ptr)
            => Form;

        public ProxyForm FindForm()
            => Form;

    }
}
