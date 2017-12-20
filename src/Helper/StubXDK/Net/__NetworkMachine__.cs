using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.Net {
    // Not a XDK type, but some titles refer to signatures mismatching with our replacements.
    public static class __NetworkMachine__ {

        [MonoModHook("System.Void Microsoft.Xna.Framework.Net.NetworkMachine::RemoveFromSession()")]
        public static void RemoveFromSession(/*NetworkMachine*/ object machine) {
            
        }

    }
}
