using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldRenderTarget2D {

        [RelinkFindableID("void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Graphics.SurfaceFormat)")]
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

        [RelinkFindableID("void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Graphics.SurfaceFormat,Microsoft.Xna.Framework.Graphics.MultiSampleType,System.Int32)")]
        public static RenderTarget2D ctor(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            int numberLevels,
            SurfaceFormat format,
            MultiSampleType multiSampleType,
            int multiSampleQuality
        ) => new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            numberLevels != 1,
            format,
            DepthFormat.Depth24Stencil8
        );

        [RelinkFindableID("void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Graphics.SurfaceFormat,Microsoft.Xna.Framework.Graphics.MultiSampleType,System.Int32,Microsoft.Xna.Framework.Graphics.RenderTargetUsage)")]
        public static RenderTarget2D ctor(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            int numberLevels,
            SurfaceFormat format,
            MultiSampleType multiSampleType,
            int multiSampleQuality,
            RenderTargetUsage usage
        ) => new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            numberLevels != 1,
            format,
            DepthFormat.Depth24Stencil8,
            0,
            usage
        );

    }
}
