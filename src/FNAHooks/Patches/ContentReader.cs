#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
using Microsoft.Xna.Framework.Graphics;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnaToFna;

namespace Microsoft.Xna.Framework.Content {
    [FNAHooks]
    public class patch_ContentReader {

        public static void fallback_ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject)
            => ((patch_ContentReader) (object) self).orig_ctor_ContentReader(manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);

        // Hooking constructors? Seems to work just fine on my machine(tm).
        private void orig_ctor_ContentReader(ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject) { }
        [MonoModConstructor]
        private void ctor_ContentReader(ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject) {
            /*if (!FNAHookBridgeFNA.Enabled || FNAHookBridgeFNA.GetContentReaderFromXnb == null) {
                orig_ctor_ContentReader(manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
                return;
            }
            */
            FNAHookBridgeFNA.ctor_ContentReader((ContentReader) (object) this, manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
        }

    }
}
