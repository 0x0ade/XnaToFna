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

        static OldGraphicsDeviceManager() {
            _OldData<GraphicsDeviceManager>.Setup = SetupOldData;
        }
        internal static void SetupOldData(_OldData<GraphicsDeviceManager> data, GraphicsDeviceManager dm) {
            WeakReference<GraphicsDeviceManager> weak = data.Weak;
            data["MinimumPixelShaderProfile"] = ShaderProfile.PS_3_0;
            data["MinimumVertexShaderProfile"] = ShaderProfile.PS_3_0;
        }

        public static ShaderProfile get_MinimumPixelShaderProfile(this GraphicsDeviceManager gdm)
            => gdm.GetOldData().Get<ShaderProfile>("MinimumPixelShaderProfile");
        public static void set_MinimumPixelShaderProfile(this GraphicsDeviceManager gdm, ShaderProfile value)
            => gdm.GetOldData().Set("MinimumPixelShaderProfile", value);

        public static ShaderProfile get_MinimumVertexShaderProfile(this GraphicsDeviceManager gdm)
            => gdm.GetOldData().Get<ShaderProfile>("MinimumVertexShaderProfile");
        public static void set_MinimumVertexShaderProfile(this GraphicsDeviceManager gdm, ShaderProfile value)
            => gdm.GetOldData().Set("MinimumVertexShaderProfile", value);

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
}
