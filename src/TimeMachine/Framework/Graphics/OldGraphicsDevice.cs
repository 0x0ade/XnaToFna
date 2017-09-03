using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using XnaToFna.ProxyForms;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldGraphicsDevice {

        static OldGraphicsDevice() {
            _OldData<GraphicsDevice>.Setup = SetupOldData;
        }
        internal static void SetupOldData(_OldData<GraphicsDevice> data, GraphicsDevice device) {
            WeakReference<GraphicsDevice> weak = data.Weak;
            data["Vertices"] = new VertexStreamCollection(weak);
            data["RenderState"] = new RenderState(weak);
            data["VertexDeclaration"] = null;
            data["CreationParameters"] = new GraphicsDeviceCreationParameters(device.Adapter, DeviceType.Hardware, device.Adapter.MonitorHandle, CreateOptions.HardwareVertexProcessing);
            data["GraphicsDeviceCapabilities"] = new GraphicsDeviceCapabilities(weak);
            data["DepthStencilBuffer"] = new DepthStencilBuffer(device, -1, -1, DepthFormat.None);
        }

        public static VertexStreamCollection get_Vertices(this GraphicsDevice device)
            => device.GetOldData()?.Get<VertexStreamCollection>("Vertices");

        // TODO: [TimeMachine] Reuse the VertexDeclaration when drawing primitives without explicit vertex declaration!
        public static VertexDeclaration get_VertexDeclaration(this GraphicsDevice device)
            => device.GetOldData()?.Get<VertexDeclaration>("VertexDeclaration");
        public static void set_VertexDeclaration(this GraphicsDevice device, VertexDeclaration decl)
            => device.GetOldData().Set("VertexDeclaration", decl);

        public static RenderState get_RenderState(this GraphicsDevice device)
            => device.GetOldData()?.Get<RenderState>("RenderState");

        public static GraphicsDeviceCreationParameters get_CreationParameters(this GraphicsDevice device)
            => device.GetOldData()?.Get<GraphicsDeviceCreationParameters>("CreationParameters");

        public static GraphicsDeviceCapabilities get_GraphicsDeviceCapabilities(this GraphicsDevice device)
            => device.GetOldData()?.Get<GraphicsDeviceCapabilities>("GraphicsDeviceCapabilities");

        public static DepthStencilBuffer get_DepthStencilBuffer(this GraphicsDevice device)
            => device.GetOldData()?.Get<DepthStencilBuffer>("DepthStencilBuffer");
        public static void set_DepthStencilBuffer(this GraphicsDevice device, DepthStencilBuffer buffer)
            => device.GetOldData()?.Set("DepthStencilBuffer", buffer);

        public static void ResolveBackBuffer(this GraphicsDevice device, RenderTarget2D target)
            => device.SetRenderTarget(target);

        public static bool CheckDeviceFormat(this GraphicsDevice device, DeviceType deviceType, SurfaceFormat adapterFormat, TextureUsage usage, QueryUsages queryUsages, ResourceType resourceType, SurfaceFormat checkFormat)
            => true;

    }
}
