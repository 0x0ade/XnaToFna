using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace XnaToFna {
    public class XnaToFnaProxyForm : Form {
        
        protected override void OnStyleChanged(EventArgs e) {
            base.OnStyleChanged(e);

            // Either borderless, or fullscreen. Not both.
            bool borderless = FormBorderStyle == FormBorderStyle.None;
            bool fullscreen = borderless && WindowState == FormWindowState.Maximized;
            borderless &= !fullscreen;

            XnaToFnaHelper.Game.Window.IsBorderlessEXT = borderless;

            GraphicsDeviceManager gdm = XnaToFnaHelper.GetService<IGraphicsDeviceManager, GraphicsDeviceManager>();
            if (gdm != null) {
                gdm.IsFullScreen = fullscreen;

                gdm.ApplyChanges();
            }
        }

    }
}
