using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldTexture2D {

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.IO.Stream)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, Stream textureStream)
            => Texture2D.FromStream(graphicsDevice, textureStream);

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.IO.Stream,Microsoft.Xna.Framework.Graphics.TextureCreationParameters)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, Stream textureStream, TextureCreationParameters creationParameters)
            => Texture2D.FromStream(graphicsDevice, textureStream).Apply(creationParameters);

        // TODO: [TimeMachine] Use LimitedStream (from FEZMod / ETGMod / ...)?

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.IO.Stream,System.Int32)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, Stream textureStream, int numberBytes)
            => Texture2D.FromStream(graphicsDevice, textureStream);

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.IO.Stream,System.Int32,Microsoft.Xna.Framework.Graphics.TextureCreationParameters)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, Stream textureStream, int numberBytes, TextureCreationParameters creationParameters)
            => Texture2D.FromStream(graphicsDevice, textureStream).Apply(creationParameters);

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.String)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, string filename) {
            using (Stream stream = File.OpenRead(filename))
                return Texture2D.FromStream(graphicsDevice, stream);
        }

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.String,Microsoft.Xna.Framework.Graphics.TextureCreationParameters)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, string filename, TextureCreationParameters creationParameters) {
            using (Stream stream = File.OpenRead(filename))
                return Texture2D.FromStream(graphicsDevice, stream).Apply(creationParameters);
        }

        [RelinkFindableID("Texture2D {0}::FromFile(Microsoft.Xna.Framework.Graphics.GraphicsDevice,System.String,System.Int32,System.Int32)")]
        public static Texture2D FromFile(GraphicsDevice graphicsDevice, string filename, int width, int height) {
            using (Stream stream = File.OpenRead(filename))
                return Texture2D.FromStream(graphicsDevice, stream, width, height, false);
        }

        private static Texture2D Apply(this Texture2D orig, TextureCreationParameters args) {
            // This here isn't any uglier than the rest of the "time machine"...
            Texture2D clone = new Texture2D(
                orig.GraphicsDevice,
                args.Width != 0 ? args.Width : orig.Width,
                args.Height != 0 ? args.Height : orig.Height,
                args.MipLevels != 0,
                args.Format
            );

            Color[] data = new Color[orig.Width * orig.Height];
            orig.GetData(data);

            for (int i = data.Length - 1; i >= 0; i--)
                if (data[i] == args.ColorKey)
                    data[i] = OldColor.TransparentBlack;

            clone.SetData(0, new Rectangle(0, 0, clone.Width, clone.Height), data, 0, data.Length);

            orig.Dispose();
            return clone;
        }

    }
}
