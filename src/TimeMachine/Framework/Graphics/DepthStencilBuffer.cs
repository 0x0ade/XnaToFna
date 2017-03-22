using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public class DepthStencilBuffer : IDisposable {

        public bool IsDisposed { get; internal set; }

        public event EventHandler Disposing;

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            IsDisposed = true;
            Disposing(this, new EventArgs());
        }

        public DepthFormat Format { get; internal set; }

        public GraphicsDevice GraphicsDevice { get; internal set; }

        public int Height { get; internal set; }

        public int MultiSampleQuality { get; internal set; }

        public MultiSampleType MultiSampleType { get; internal set; }

        public string Name { get; set; }

        public object Tag { get; set; }

        public int Width { get; internal set; }

        public bool IsContentLost {
            get {
                return true;
            }
        }

        public DepthStencilBuffer(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            DepthFormat format
        ) : this(
            graphicsDevice,
            width,
            height,
            format,
            MultiSampleType.None,
            0
        ) {
        }

        public DepthStencilBuffer(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            DepthFormat format,
            MultiSampleType multiSampleType,
            int multiSampleQuality
        ) {
            GraphicsDevice = graphicsDevice;
            Width = width;
            Height = height;
            Format = format;
            MultiSampleType = multiSampleType;
            MultiSampleQuality = multiSampleQuality;
        }

    }
}
