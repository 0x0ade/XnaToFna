using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkType]
    public struct TextureCreationParameters {

        public static TextureCreationParameters Default {
            get {
                return new TextureCreationParameters(
                    0, 0, 0, 0,
                    SurfaceFormat.Color, // Default: Unknown
                    TextureUsage.None,
                    OldColor.TransparentBlack,
                    FilterOptions.Dither | FilterOptions.Triangle,
                    FilterOptions.Box
                );
            }
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Depth { get; set; }

        public int MipLevels { get; set; }

        public SurfaceFormat Format { get; set; }

        public Color ColorKey { get; set; }

        public FilterOptions Filter { get; set; }

        public FilterOptions MipFilter { get; set; }

        public TextureCreationParameters(
            int width,
            int height,
            int depth,
            int mipLevels,
            SurfaceFormat format,
            TextureUsage textureUsage,
            Color colorKey,
            FilterOptions filter,
            FilterOptions mipFilter
        ) {
            Width = width;
            Height = height;
            Depth = depth;
            MipLevels = mipLevels;
            Format = format;
            ColorKey = colorKey;
            Filter = filter;
            MipFilter = mipFilter;
        }

    }
}
