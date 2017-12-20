using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Assumed to be a static class.
    public static class LeaderboardEntryExtensions {

        [MonoModHook("System.Int32 XnaToFna.StubXDK.GamerServices.LeaderboardEntryExtensions::GetRank(Microsoft.Xna.Framework.GamerServices.LeaderboardEntry)")]
        public static int GetRank(/*LeaderboardEntry*/ object entry) {
            return 0;
        }

    }
}
