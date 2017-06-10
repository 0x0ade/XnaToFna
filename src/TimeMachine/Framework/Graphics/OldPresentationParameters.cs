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
    public static class OldPresentationParameters {

        // XNA 4.0 seems to only support one back buffer.
        public static int get_BackBufferCount(this PresentationParameters @params)
            => 1;
        public static void set_BackBufferCount(this PresentationParameters @params, int value) {}

        // XNA 4.0 dropped refresh rate access / manipulation.
        public static int get_FullScreenRefreshRateInHz(this PresentationParameters @params)
            => 0; // Adapter default.
        public static void set_FullScreenRefreshRateInHz(this PresentationParameters @params, int value) {}

    }
}
