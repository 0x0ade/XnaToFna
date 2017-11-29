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
            // We may need to refresh the hooks - who knows what the JIT's doing.
            ContentHelper.FNAHooksLegacy.Online.Hook();
            ContentHelper.FNAHooksLegacy.Online.ForcedStream = new GZipStream(input.BaseStream, CompressionMode.Decompress, true);
            return input.ContentManager.Load<ContentType>("///XNATOFNA/gzip/" + input.AssetName);
        }

    }
}
