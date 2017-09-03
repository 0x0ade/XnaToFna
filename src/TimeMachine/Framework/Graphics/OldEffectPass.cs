using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldEffectPass {

        public static void Begin(this EffectPass self) => self.Apply();
        public static void End(this EffectPass self) { }

    }
}
