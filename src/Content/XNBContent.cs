using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using MonoMod.Detour;
using MonoMod.InlineRT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    public static partial class ContentHelper {

        public static bool XNBCompressGZip = true;

        private static Type t_GZipContentReader = typeof(GZipContentReader<>);

        public static void TransformContent(string path) {
            if (Game == null)
                return;
            if (!File.Exists(path))
                // File got removed or renamed - possibly part of a song / video.
                return;

            Log($"[TransformContent] Transforming {path}");

            FNAHooks.Offline.Hook();

            // This may fail horribly if the content is a ValueType (struct).
            object obj = Game.Content.Load<object>(path/*.Substring(0, path.Length - 4)*/);
            (obj as IDisposable)?.Dispose();
            Game.Content.Unload();

            // Update the size embedded in the .xnb.
            UpdateXNBSize(path + ".tmp");

            // Replace .xnb with .tmp
            File.Delete(path);

            if (!XNBCompressGZip) {
                File.Move(path + ".tmp", path);

            } else {
                using (Stream streamTMP = File.Open(path + ".tmp", FileMode.Open, FileAccess.Read))
                using (Stream streamXNB = File.Open(path, FileMode.Create, FileAccess.Write)) {
                    using (BinaryReader reader = new BinaryReader(streamTMP, Encoding.ASCII, true))
                    using (BinaryWriter writer = new BinaryWriter(streamXNB, Encoding.ASCII, true)) {
                        // Copy-paste the first 6 bytes.
                        writer.Write(reader.ReadBytes(6));

                        writer.Write((uint) 0); // Write size = 0 for now, will be updated later.

                        // We've only got a single type reader for the container.
                        writer.Write((byte) 1);
                        writer.Write(t_GZipContentReader.MakeGenericType(obj.GetType()).AssemblyQualifiedName);
                        writer.Write((uint) 0); // Version

                        writer.Write((byte) 0); // No shared resources.

                        writer.Write((byte) 1); // Type reader ID - 0 refers to null.
                    }

                    // Seek TMP to beginning, then GZIP all of it.
                    streamTMP.Seek(0, SeekOrigin.Begin);
                    using (GZipStream gzipXNB = new GZipStream(streamXNB, CompressionMode.Compress, true)) {
                        streamTMP.CopyTo(gzipXNB);
                    }
                }
                File.Delete(path + ".tmp");
                UpdateXNBSize(path);
            }

            FNAHooks.Offline.Enabled = false;

            // If we just loaded a texture, reload and dump it as PNG.
            /*
            if (obj is Texture2D) {
                // FNA can't save DXT1, DXT3 and DXT5 textures... unless we force conversion.
                FNAHooks.Offline.SupportsDxt1 = false;
                FNAHooks.Offline.SupportsS3tc = false;
                using (Texture2D tex = Game.Content.Load<Texture2D>(path)) {
                    Log($"[TransformContent] Dumping texture, original format: {((Texture2D) obj).Format}");
                    if (File.Exists(path + ".png"))
                        File.Delete(path + ".png");
                    using (Stream stream = File.OpenWrite(path + ".png"))
                        tex.SaveAsPng(stream, tex.Width, tex.Height);
                }
                FNAHooks.Offline.SupportsDxt1 = null;
                FNAHooks.Offline.SupportsS3tc = null;
            }
            */

            FNAHooks.Offline.Enabled = true;
        }

        public static void UpdateXNBSize(string path, uint size = 0) {
            using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                if (size == 0)
                    size = (uint) stream.Length;
                // We know that the size is always past the header, version and flags.
                stream.Position = 6;
                writer.Write(size);
            }
        }

    }
}
