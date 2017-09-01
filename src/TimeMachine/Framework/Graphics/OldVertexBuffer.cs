using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldVertexBuffer {

        [RelinkFindableID("System.Void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Int32,Microsoft.Xna.Framework.Graphics.BufferUsage)")]
        public static VertexBuffer ctor(
            GraphicsDevice graphicsDevice,
            int sizeInBytes,
            BufferUsage usage
        ) {
            throw new InvalidProgramException("XnaToFna didn't replace this .ctor call!");
        }

        [RelinkFindableID("System.Void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Type,System.Int32,Microsoft.Xna.Framework.Graphics.BufferUsage)")]
        public static VertexBuffer ctor(
            GraphicsDevice graphicsDevice,
            Type vertexType,
            int elementCount,
            BufferUsage usage
        ) => new VertexBuffer(
            graphicsDevice,
            vertexType,
            elementCount,
            BufferTypeHelper.ConvertBufferUsage(usage)
        );

    }
}
