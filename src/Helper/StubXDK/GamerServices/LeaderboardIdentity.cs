using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but some titles refer to signatures mismatching with our replacements.
    public struct __LeaderboardIdentity__ {

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.LeaderboardIdentity::set_Key(System.String)")]
        public static void set_Key(/*LeaderboardIdentity*/ ref object identity, string value) {

        }

        [MonoModHook("System.String Microsoft.Xna.Framework.GamerServices.LeaderboardIdentity::get_Key()")]
        public static string get_Key(/*LeaderboardIdentity*/ ref object identity) {
            return "";
        }

    }
}
