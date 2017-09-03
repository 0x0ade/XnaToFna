using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldIndexBuffer {

        [RelinkFindableID("System.Void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Int32,Microsoft.Xna.Framework.Graphics.BufferUsage,Microsoft.Xna.Framework.Graphics.IndexElementSize)")]
        public static IndexBuffer ctor(
            GraphicsDevice graphicsDevice,
            int sizeInBytes,
            BufferUsage usage,
            IndexElementSize elementSize
        ) => new IndexBuffer(
            graphicsDevice,
            elementSize,
            sizeInBytes / BufferTypeHelper.ConvertIndexElementSize(elementSize),
            BufferTypeHelper.ConvertBufferUsage(usage)
        );

        [RelinkFindableID("System.Void {0}::.ctor(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.Type,System.Int32,Microsoft.Xna.Framework.Graphics.BufferUsage)")]
        public static IndexBuffer ctor(
            GraphicsDevice graphicsDevice,
            Type indexType,
            int elementCount,
            BufferUsage usage
        ) => new IndexBuffer(
            graphicsDevice,
            indexType,
            elementCount,
            BufferTypeHelper.ConvertBufferUsage(usage)
        );

        public static int get_SizeInBytes(this IndexBuffer self) => self.IndexCount * BufferTypeHelper.ConvertIndexElementSize(self.IndexElementSize);

    }
}
