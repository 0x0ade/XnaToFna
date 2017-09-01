using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldVertexDeclaration {

        [RelinkFindableID("System.Void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,Microsoft.Xna.Framework.Graphics.VertexElement[])")]
        public static VertexDeclaration ctor(
            GraphicsDevice graphicsDevice,
            VertexElement[] elements
        ) => new VertexDeclaration(elements);

    }
}
