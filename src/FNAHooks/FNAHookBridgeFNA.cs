using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    [FNAHooks]
    public static class FNAHookBridgeFNA {
        public static bool Enabled = true;

        public delegate ContentReader d_GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject);
        public static d_GetContentReaderFromXnb orig_GetContentReaderFromXnb = patch_ContentManager.fallback_GetContentReaderFromXnb;
        public static d_GetContentReaderFromXnb GetContentReaderFromXnb = orig_GetContentReaderFromXnb; // Assigned by XTF.

        public delegate void d_ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject);
        public static d_ctor_ContentReader orig_ctor_ContentReader = patch_ContentReader.fallback_ctor_ContentReader;
        public static d_ctor_ContentReader ctor_ContentReader = orig_ctor_ContentReader; // Assigned by XTF.

    }
}

