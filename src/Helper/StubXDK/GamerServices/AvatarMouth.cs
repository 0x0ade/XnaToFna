using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but this type isn't included in our replacements.
    public enum AvatarMouth {

        /* We only care about the type existing on IL-level, not what values it holds...
         * ... but MSDN at least provides the values for this enum.
         * I don't know if they're in this order, though.
         */

        Angry,
        Confused,
        Happy,
        Laughing,
        Neutral,
        PhoneticAi,
        PhoneticDth,
        PhoneticEe,
        PhoneticFv,
        PhoneticL,
        PhoneticO,
        PhoneticW,
        Sad,
        Shocked

    }
}
