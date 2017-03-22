using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [Serializable]
    [RelinkType]
    public class GraphicsDeviceCreationParameters {

        public GraphicsAdapter Adapter { get; internal set; }

        public CreateOptions CreationOptions { get; internal set; }

        public DeviceType DeviceType { get; internal set; }

        public IntPtr FocusWindowHandle { get; internal set; }

        public GraphicsDeviceCreationParameters(
            GraphicsAdapter graphicsAdapter,
            DeviceType deviceType,
            IntPtr windowHandle,
            CreateOptions createOptions
        ) {
            Adapter = graphicsAdapter;
            CreationOptions = createOptions;
            DeviceType = deviceType;
            FocusWindowHandle = windowHandle;
        }

    }
}
