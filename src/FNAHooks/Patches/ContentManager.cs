#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
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
    public class patch_ContentManager : ContentManager {

        public patch_ContentManager(IServiceProvider serviceProvider)
            : base(serviceProvider) {
            // no-op.
        }

        public static ContentReader fallback_GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject)
            => ((patch_ContentManager) self).orig_GetContentReaderFromXnb(originalAssetName, ref stream, xnbReader, platform, recordDisposableObject);

        private ContentReader orig_GetContentReaderFromXnb(string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject) { return null; }
        private ContentReader GetContentReaderFromXnb(string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject) {
            if (!FNAHookBridgeFNA.Enabled || FNAHookBridgeFNA.GetContentReaderFromXnb == null)
                return orig_GetContentReaderFromXnb(originalAssetName, ref stream, xnbReader, platform, recordDisposableObject);
            return FNAHookBridgeFNA.GetContentReaderFromXnb(this, originalAssetName, ref stream, xnbReader, platform, recordDisposableObject);
        }

    }
}
