using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Assumed to be a static class.
    public static class GamerExtensions {

        [MonoModHook("System.UInt64 Microsoft.Xna.Framework.GamerServices.GamerExtensions::GetXuid(Microsoft.Xna.Framework.GamerServices.Gamer)")]
        public static ulong GetXuid(/*Gamer*/ object gamer) {
            return 0UL;
        }

    }
}
