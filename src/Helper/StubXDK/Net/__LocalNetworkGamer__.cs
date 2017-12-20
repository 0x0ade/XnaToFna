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
    public static class __LocalNetworkGamer__ {

        [MonoModHook("System.Void Microsoft.Xna.Framework.Net.LocalNetworkGamer::SendPartyInvites()")]
        public static void SendPartyInvites(/*LocalNetworkGamer*/ object gamer) {
            
        }

        [MonoModHook("System.Void Microsoft.Xna.Framework.Net.LocalNetworkGamer::EnableSendVoice(Microsoft.Xna.Framework.Net.NetworkGamer,System.Boolean)")]
        public static void EnableSendVoice(/*LocalNetworkGamer*/ object gamer, /*LocalNetworkGamer*/ object remote, bool flag) {

        }

    }
}
