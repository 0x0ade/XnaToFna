using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Mono.Cecil;
using MonoMod;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoMod.Detour;
using System.IO.Compression;
using System.Reflection.Emit;

namespace XnaToFna.ContentTransformers {
    public class GZipContentReader<ContentType> : ContentTypeReader<ContentType> {

        /* Notes:
         * 
         * Don't prepend stuff like //XNATOFNA/gzip/ to the asset name, as it
         * breaks relative external references.
         * 
         * Don't dispose the wrapped content manager, as it disposes any objects.
         * Instead, they _should_ get disposed by the original content manager.
         */

        private ForcedStreamContentManager WrappedContentManager;

        protected override ContentType Read(ContentReader input, ContentType existing) {
            if (WrappedContentManager == null) {
                WrappedContentManager = new ForcedStreamContentManager(input.ContentManager.ServiceProvider);
            }
            WrappedContentManager.RootDirectory = input.ContentManager.RootDirectory;
            WrappedContentManager.Stream = new GZipStream(input.BaseStream, CompressionMode.Decompress, true);
            bool bridgeWasEnabled = FNAHookBridgeXTF.Enabled;
            FNAHookBridgeXTF.Enabled = false;
            ContentType result = WrappedContentManager.Load<ContentType>(input.AssetName);
            FNAHookBridgeXTF.Enabled = bridgeWasEnabled;
            return result;
        }

    }
}
