using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldResolveTexture2D {

        [RelinkName(".ctor")]
        public static RenderTarget2D ctor(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            int numberLevels,
            SurfaceFormat format
        ) => new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            numberLevels != 1,
            format,
            DepthFormat.Depth24Stencil8
        );

    }
}
