using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using MonoMod.InlineRT;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public sealed class VertexStream {

        private readonly static MethodInfo m_set_VertexStride = typeof(VertexDeclaration).GetProperty("VertexStride").GetSetMethod(true);

        internal WeakReference<GraphicsDevice> INTERNAL_deviceRef;
        internal GraphicsDevice INTERNAL_device {
            get {
                return INTERNAL_deviceRef.GetTarget();
            }
        }

        internal int INTERNAL_index;

        internal VertexBufferBinding INTERNAL_binding {
            get {
                return INTERNAL_device.GetVertexBuffers()[INTERNAL_index];
            }
            set {
                GraphicsDevice device = INTERNAL_device;
                VertexBufferBinding[] bindings = device.GetVertexBuffers();
                bindings[INTERNAL_index] = new VertexBufferBinding(
                    value.VertexBuffer,
                    value.VertexOffset,
                    value.InstanceFrequency
                );
                device.SetVertexBuffers(bindings);
            }
        }

        public int OffsetInBytes {
            get {
                return INTERNAL_binding.VertexOffset;
            }
        }
        public VertexBuffer VertexBuffer {
            get {
                return INTERNAL_binding.VertexBuffer;
            }
        }
        public int VertexStride {
            get {
                return INTERNAL_binding.VertexBuffer.VertexDeclaration.VertexStride;
            }
        }

        internal VertexStream(WeakReference<GraphicsDevice> device, int index) {
            INTERNAL_deviceRef = device;
            INTERNAL_index = index;
        }

        public void SetFrequency(int frequency) {
            VertexBufferBinding old = INTERNAL_binding;
            INTERNAL_binding = new VertexBufferBinding(
                old.VertexBuffer,
                old.VertexOffset,
                frequency
            );
        }

        public void SetFrequencyOfIndexData(int frequency) {
            // Sets the stream source frequency divider value for the index data. This may be used to draw several instances of geometry.
            // TODO
            SetFrequency(frequency);
        }

        public void SetFrequencyOfInstanceData(int frequency) {
            // Sets the stream source frequency divider value for the instance data. This may be used to draw several instances of geometry.
            // TODO
            SetFrequency(frequency);
        }

        public void SetSource(VertexBuffer vb, int offsetInBytes, int vertexStride) {
            ReflectionHelper.GetDelegate(m_set_VertexStride)(vb, vertexStride);
            INTERNAL_binding = new VertexBufferBinding(
                vb,
                offsetInBytes,
                INTERNAL_binding.InstanceFrequency
            );
        }

    }
}
