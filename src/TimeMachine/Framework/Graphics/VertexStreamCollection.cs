using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public sealed class VertexStreamCollection {

        internal WeakReference<GraphicsDevice> INTERNAL_deviceRef;

        public VertexStream this[int index] {
            get {
                return new VertexStream(INTERNAL_deviceRef, index);
            }
        }

        // Used in combination with a GraphicsDevice with VertexStreams proxying to bindings... Smells fishy.
        internal VertexStreamCollection(WeakReference<GraphicsDevice> device) {
            INTERNAL_deviceRef = device;
        }

    }
}
