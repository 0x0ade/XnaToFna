using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but this type isn't included in our replacements.
    public interface IAvatarAnimation {

        ReadOnlyCollection<Matrix> BoneTransforms { get; }
        TimeSpan CurrentPosition { get; set; }
        AvatarExpression Expression { get; }
        TimeSpan Length { get; }

        void Update(TimeSpan elapsedAnimationTime, bool loop);

    }
}
