using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldVertexElement {

        [RelinkName(".ctor")]
        public static VertexElement ctor(
            short stream,
            short offset,
            VertexElementFormat elementFormat,
            VertexElementMethod elementMethod,
            VertexElementUsage elementUsage,
            byte usageIndex
        ) => new VertexElement(
            offset,
            elementFormat,
            elementUsage,
            usageIndex
        );

        public static short get_Stream(this VertexElement self) => 0;
        public static void set_Stream(this VertexElement self, short value) { }

        public static VertexElementMethod get_VertexElementMethod(this VertexElement self) => 0;
        public static void set_VertexElementMethod(this VertexElement self, VertexElementMethod value) { }

        public static short get_Offset(this VertexElement self) => (short) self.Offset;
        public static void set_Offset(this VertexElement self, short value) => self.Offset = value;

        public static byte get_UsageIndex(this VertexElement self) => (byte) self.UsageIndex;
        public static void set_UsageIndex(this VertexElement self, byte value) => self.UsageIndex = value;

    }
}
