using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnaToFna.ProxyForms;

namespace XnaToFna {
    /// <summary>
    /// Required to initialize the XnaToFnaHelper properly without hardcoding the XnaToFnaHelper.Initialize call.
    /// </summary>
    public class XnaToFnaGame : Game {

        public XnaToFnaGame()
            : base() {
            XnaToFnaHelper.Initialize(this);
        }

        protected override void Initialize() {
            base.Initialize();
            // Required to get XNATOFNA_DISPLAY_* to apply.
            XnaToFnaHelper.ApplyChanges((GraphicsDeviceManager) Services.GetService(typeof(IGraphicsDeviceManager)));
        }

        protected override void EndDraw() {
            base.EndDraw();
            // ProxyForm batches the changes and then applies them all at once to f.e. detect being a borderless fullscreen window.
            GameForm.Instance?.ApplyChanges();
        }
        
    }
}
