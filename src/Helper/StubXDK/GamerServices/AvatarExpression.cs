using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but this type isn't included in our replacements.
    public struct AvatarExpression {

        public AvatarEye LeftEye { get; set; }
        public AvatarEyebrow LeftEyebrow { get; set; }
        public AvatarMouth Mouth { get; set; }
        public AvatarEye RightEye { get; set; }
        public AvatarEyebrow RightEyebrow { get; set; }

    }
}
