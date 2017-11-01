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

        protected override ContentType Read(ContentReader input, ContentType existing) {
            GZipContentReaderFNAHooks.Hook(); // We may need to refresh the hooks - who knows what the JIT's doing.
            GZipContentReaderFNAHooks.ForcedStream = new GZipStream(input.BaseStream, CompressionMode.Decompress, true);
            return input.ContentManager.Load<ContentType>("///XNATOFNA/gzip/" + input.AssetName);
        }

    }

    // Yo dawg, I heard you like patching...
    public static class GZipContentReaderFNAHooks {

        public static bool Enabled = true;
        private static bool IsHooked = false;

        public static void Hook() {
            ContentHelper.FNAHooks.Hook(IsHooked, typeof(ContentManager), "OpenStream", ref orig_OpenStream);
            IsHooked = true;
        }

        public static Stream ForcedStream;
        private delegate Stream d_OpenStream(ContentManager self, string name);
        private static d_OpenStream orig_OpenStream;
        private static Stream OpenStream(ContentManager self, string name) {
            if (!Enabled)
                return orig_OpenStream(self, name);

            bool isXTF = name.StartsWith("///XNATOFNA/");
            if (isXTF)
                name = name.Substring(12);

            Stream stream;
            if (isXTF && ForcedStream != null) {
                stream = ForcedStream;
                ForcedStream = stream;
            } else {
                stream = orig_OpenStream(self, name);
            }
            return stream;
        }

    }
}
