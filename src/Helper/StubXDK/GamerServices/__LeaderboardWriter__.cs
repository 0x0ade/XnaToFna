using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but some titles refer to signatures mismatching with our replacements.
    public static class __LeaderboardWriter__ {

        private static Type t_LeaderboardEntry;
        private static ConstructorInfo ctor_LeaderboardEntry;

        [MonoModHook("Microsoft.Xna.Framework.GamerServices.LeaderboardEntry Microsoft.Xna.Framework.GamerServices.LeaderboardWriter::GetLeaderboard(Microsoft.Xna.Framework.GamerServices.LeaderboardIdentity)")]
        public static /*LeaderboardEntry*/ object GetLeaderboard(/*LeaderboardWriter*/ object writer, /*LeaderboardIdentity*/ object identity) {
            // Return an empty entry.
            if (t_LeaderboardEntry == null) {
                t_LeaderboardEntry = StubXDKHelper.GamerServicesAsm.GetType("Microsoft.Xna.Framework.GamerServices.LeaderboardEntry");
                ctor_LeaderboardEntry = t_LeaderboardEntry.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { },
                    null
                );
            }
            return ctor_LeaderboardEntry.Invoke(new object[] { });
        }

    }
}
