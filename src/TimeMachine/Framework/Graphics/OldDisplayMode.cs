using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using XnaToFna.ProxyForms;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldDisplayMode {

        // XNA 4.0 dropped refresh rate access.
        public static int get_RefreshRate(ref DisplayMode dm)
            => 0; // Adapter default.

    }
}
