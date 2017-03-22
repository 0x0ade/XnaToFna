using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldColor {

        public static Color TransparentBlack {
            get {
                return new Color(0, 0, 0, 0);
            }
        }

        public static Color TransparentWhite {
            get {
                return new Color(255, 255, 255, 0);
            }
        }

    }
}
