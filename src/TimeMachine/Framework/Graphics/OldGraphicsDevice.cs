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

        // Only use weak references so that GraphicsDevices can get disposed proper~
        // Wait a second, don't they live as long as the game runs anyway?
        internal static ConditionalWeakTable<GraphicsDevice, OldGraphicsDeviceData> Data = new ConditionalWeakTable<GraphicsDevice, OldGraphicsDeviceData>();
        internal static OldGraphicsDeviceData GetOldData(this GraphicsDevice device) {
            OldGraphicsDeviceData data;

            if (Data.TryGetValue(device, out data))
                return data;

            data = new OldGraphicsDeviceData(device);
            Data.Add(device, data);
            return data;
        }

        public static VertexStreamCollection get_Vertices(this GraphicsDevice device)
            => device.GetOldData()?.Vertices;

        public static RenderState get_RenderState(this GraphicsDevice device)
            => device.GetOldData()?.RenderState;

        public static GraphicsDeviceCreationParameters get_CreationParameters(this GraphicsDevice device)
            => device.GetOldData()?.CreationParameters;

        public static GraphicsDeviceCapabilities get_GraphicsDeviceCapabilities(this GraphicsDevice device)
            => device.GetOldData()?.GraphicsDeviceCapabilities;

        public static DepthStencilBuffer get_DepthStencilBuffer(this GraphicsDevice device)
            => device.GetOldData()?.DepthStencilBuffer;
        public static void set_DepthStencilBuffer(this GraphicsDevice device, DepthStencilBuffer buffer)
            => device.GetOldData().DepthStencilBuffer = buffer;

        public static void ResolveBackBuffer(this GraphicsDevice device, RenderTarget2D target)
            => device.SetRenderTarget(target);

        public static bool CheckDeviceFormat(this GraphicsDevice device, DeviceType deviceType, SurfaceFormat adapterFormat, TextureUsage usage, QueryUsages queryUsages, ResourceType resourceType, SurfaceFormat checkFormat)
            => true;

    }
    
    public class OldGraphicsDeviceData {
        public VertexStreamCollection Vertices;
        public RenderState RenderState;
        public GraphicsDeviceCreationParameters CreationParameters;
        public GraphicsDeviceCapabilities GraphicsDeviceCapabilities;
        public DepthStencilBuffer DepthStencilBuffer;

        public OldGraphicsDeviceData(GraphicsDevice device) {
            WeakReference<GraphicsDevice> weak = new WeakReference<GraphicsDevice>(device);

            Vertices = new VertexStreamCollection(weak);
            RenderState = new RenderState(weak);
            CreationParameters = new GraphicsDeviceCreationParameters(device.Adapter, DeviceType.Hardware, device.Adapter.MonitorHandle, CreateOptions.HardwareVertexProcessing);
            GraphicsDeviceCapabilities = new GraphicsDeviceCapabilities(weak);
            DepthStencilBuffer = new DepthStencilBuffer(device, -1, -1, DepthFormat.None);
        }
    }
}
