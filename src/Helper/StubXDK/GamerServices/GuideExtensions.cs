using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Assumed to be a static class.
    public static class GuideExtensions {

        public static ConsoleRegion ConsoleRegion {
            get {
                return 0;
            }
        }

        public static void ShowAchievements(this PlayerIndex player) {

        }

    }
}
