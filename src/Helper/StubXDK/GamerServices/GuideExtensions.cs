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
            [MonoModHook("Microsoft.Xna.Framework.GamerServices.ConsoleRegion Microsoft.Xna.Framework.GamerServices.GuideExtensions::get_ConsoleRegion()")]
            get {
                return 0;
            }
        }

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.GuideExtensions::ShowAchievements(Microsoft.Xna.Framework.GamerServices.PlayerIndex)")]
        public static void ShowAchievements(this PlayerIndex player) {

        }

    }
}
