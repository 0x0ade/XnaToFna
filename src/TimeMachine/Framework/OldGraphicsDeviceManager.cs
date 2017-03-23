using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using XnaToFna.ProxyForms;
using XnaToFna.TimeMachine.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework {
    public static class OldGraphicsDeviceManager {

        // Only use weak references so that GraphicsDeviceManager can get disposed proper~
        // Wait a second, don't they live as long as the game runs anyway?
        internal static ConditionalWeakTable<GraphicsDeviceManager, OldGraphicsDeviceManagerData> Data = new ConditionalWeakTable<GraphicsDeviceManager, OldGraphicsDeviceManagerData>();
        internal static OldGraphicsDeviceManagerData GetOldData(this GraphicsDeviceManager gdm) {
            OldGraphicsDeviceManagerData data;

            if (Data.TryGetValue(gdm, out data))
                return data;

            data = new OldGraphicsDeviceManagerData(gdm);
            Data.Add(gdm, data);
            return data;
        }

        public static ShaderProfile get_MinimumPixelShaderProfile(this GraphicsDeviceManager gdm)
            => gdm.GetOldData().MinimumPixelShaderProfile;
        public static void set_MinimumPixelShaderProfile(this GraphicsDeviceManager gdm, ShaderProfile value)
            => gdm.GetOldData().MinimumPixelShaderProfile = value;

        public static ShaderProfile get_MinimumVertexShaderProfile(this GraphicsDeviceManager gdm)
            => gdm.GetOldData().MinimumVertexShaderProfile;
        public static void set_MinimumVertexShaderProfile(this GraphicsDeviceManager gdm, ShaderProfile value)
            => gdm.GetOldData().MinimumVertexShaderProfile = value;

        // XNA 4.0 changed from EventHandler to EventHandler<> for those events
        public static void add_DeviceCreated(this GraphicsDeviceManager gdm, EventHandler handler)
            => gdm.DeviceCreated += new EventHandler<EventArgs>(handler);
        public static void add_DeviceDisposing(this GraphicsDeviceManager gdm, EventHandler handler)
            => gdm.DeviceDisposing += new EventHandler<EventArgs>(handler);
        public static void add_DeviceReset(this GraphicsDeviceManager gdm, EventHandler handler)
            => gdm.DeviceReset += new EventHandler<EventArgs>(handler);
        public static void add_DeviceResetting(this GraphicsDeviceManager gdm, EventHandler handler)
            => gdm.DeviceResetting += new EventHandler<EventArgs>(handler);
        public static void add_Disposed(this GraphicsDeviceManager gdm, EventHandler handler)
            => gdm.Disposed += new EventHandler<EventArgs>(handler);

    }
    
    public class OldGraphicsDeviceManagerData {
        public ShaderProfile MinimumPixelShaderProfile = ShaderProfile.PS_3_0;
        public ShaderProfile MinimumVertexShaderProfile = ShaderProfile.VS_3_0;

        public OldGraphicsDeviceManagerData(GraphicsDeviceManager gdm) {
            WeakReference<GraphicsDeviceManager> weak = new WeakReference<GraphicsDeviceManager>(gdm);
        }
    }
}
