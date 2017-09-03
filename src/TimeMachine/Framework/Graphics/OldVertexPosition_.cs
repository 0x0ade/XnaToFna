using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldVertexPositionTexture {

        public static int SizeInBytes { get; } = Marshal.SizeOf(typeof(VertexPositionTexture));

        public static VertexElement[] VertexElements = VertexPositionTexture.VertexDeclaration.GetVertexElements();

    }

    public static class OldVertexPositionColor {

        public static int SizeInBytes { get; } = Marshal.SizeOf(typeof(VertexPositionColor));

        public static VertexElement[] VertexElements = VertexPositionColor.VertexDeclaration.GetVertexElements();

    }

    public static class OldVertexPositionColorTexture {

        public static int SizeInBytes { get; } = Marshal.SizeOf(typeof(VertexPositionColorTexture));

        public static VertexElement[] VertexElements = VertexPositionColorTexture.VertexDeclaration.GetVertexElements();

    }

    public static class OldVertexPositionNormalTexture {

        public static int SizeInBytes { get; } = Marshal.SizeOf(typeof(VertexPositionNormalTexture));

        public static VertexElement[] VertexElements = VertexPositionNormalTexture.VertexDeclaration.GetVertexElements();

    }
}
