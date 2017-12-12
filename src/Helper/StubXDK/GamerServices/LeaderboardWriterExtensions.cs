using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Assumed to be a static class.
    public static class LeaderboardWriterExtensions {

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.LeaderboardWriterExtensions::SetScore(Microsoft.Xna.Framework.GamerServices.LeaderboardWriter,System.Int32)")]
        public static void SetScore(/*LeaderboardWriter*/ object writer, int score) {

        }

    }
}
