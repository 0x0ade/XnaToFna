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

            FNAHooks.Hook();

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

            FNAHooks.Enabled = false;

            // If we just loaded a texture, reload and dump it as PNG.
            /*
            if (obj is Texture2D) {
                // FNA can't save DXT1, DXT3 and DXT5 textures... unless we force conversion.
                FNAContentManagerHooks.SupportsDxt1 = false;
                FNAContentManagerHooks.SupportsS3tc = false;
                using (Texture2D tex = Game.Content.Load<Texture2D>(path)) {
                    Log($"[TransformContent] Dumping texture, original format: {((Texture2D) obj).Format}");
                    if (File.Exists(path + ".png"))
                        File.Delete(path + ".png");
                    using (Stream stream = File.OpenWrite(path + ".png"))
                        tex.SaveAsPng(stream, tex.Width, tex.Height);
                }
                FNAContentManagerHooks.SupportsDxt1 = null;
                FNAContentManagerHooks.SupportsS3tc = null;
            }
            */

            FNAHooks.Enabled = true;
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

        // Yo dawg, I heard you like patching...
        public static class FNAHooks {

            private static bool IsHooked = false;
            public static bool Enabled = true;

            public static void Hook() {
                if (!IsHooked) {
                    Dictionary<string, Func<ContentTypeReader>> typeCreators = (Dictionary<string, Func<ContentTypeReader>>)
                        typeof(ContentTypeReaderManager).GetField("typeCreators", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                    typeCreators["Microsoft.Xna.Framework.Content.EffectReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"]
                        = () => new EffectTransformer();
                    typeCreators["Microsoft.Xna.Framework.Content.SoundEffectReader"] // Games somehow don't use the full name for this one...
                        = () => new SoundEffectTransformer();
                }

                Hook(IsHooked, typeof(ContentManager), "GetContentReaderFromXnb", ref orig_GetContentReaderFromXnb);

                // Hooking constructors? Seems to work just fine on my machine(tm).
                Hook(IsHooked, typeof(ContentReader), ".ctor[0]", ref orig_ctor_ContentReader);

                object gl = typeof(GraphicsDevice).GetField("GLDevice", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Game.GraphicsDevice);
                Type t_gl = gl.GetType();
                orig_SupportsDxt1 = (bool) t_gl.GetProperty("SupportsDxt1").GetValue(gl);
                Hook(IsHooked, t_gl, "get_SupportsDxt1", ref orig_get_SupportsDxt1);
                orig_SupportsS3tc = (bool) t_gl.GetProperty("SupportsS3tc").GetValue(gl);
                Hook(IsHooked, t_gl, "get_SupportsS3tc", ref orig_get_SupportsS3tc);

                IsHooked = true;
            }

            internal static MethodBase Find(Type type, string name) {
                MethodBase found = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (found != null)
                    return found;

                if (name.StartsWith("get_") || name.StartsWith("set_")) {
                    PropertyInfo prop = type.GetProperty(name.Substring(4), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (name[0] == 'g')
                        found = prop.GetGetMethod(true);
                    else
                        found = prop.GetSetMethod(true);
                }

                return found;
            }
            internal static void Hook<T>(bool isHooked, Type type, string name, ref T trampoline) {
                MethodBase from;
                MethodBase to;
                if (name.StartsWith(".ctor[")) {
                    ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    int index;
                    if (int.TryParse(name.Substring(6, name.Length - 6 - 1), out index))
                        from = ctors[index];
                    else
                        from = ctors[0];
                    to = Find(typeof(T).DeclaringType, "ctor_" + type.Name);
                } else {
                    from = Find(type, name);
                    to = Find(typeof(T).DeclaringType, name);
                }

                // Keeps the detour intact. The JIT likes to revert our changes...
                if (isHooked) {
                    RuntimeDetour.Refresh(from);
                    return;
                }

                T tmp =
                    from
                    .Detour<T>(
                        to
                    );
                if (trampoline == null)
                    trampoline = tmp;
            }

            private delegate ContentReader d_GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject);
            private static d_GetContentReaderFromXnb orig_GetContentReaderFromXnb;
            private static ContentReader GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject) {
                if (!Enabled)
                    return orig_GetContentReaderFromXnb(self, originalAssetName, ref stream, xnbReader, platform, recordDisposableObject);

                // output will be disposed with the ContentReader.
                Stream output = File.OpenWrite(originalAssetName + ".tmp");

                // We need to read the first 6 bytes ourselves (4 header bytes + version + flags).
                long xnbPos = xnbReader.BaseStream.Position;
                xnbReader.BaseStream.Seek(0, SeekOrigin.Begin);

                using (BinaryWriter writer = new BinaryWriter(output, Encoding.ASCII, true)) {
                    writer.Write(xnbReader.ReadBytes(5));

                    byte flags = xnbReader.ReadByte();
                    flags = (byte) (flags & ~0x80); // Remove the compression flag.
                    writer.Write(flags);

                    writer.Write(0); // Write size = 0 for now, will be updated later.
                }

                xnbReader.BaseStream.Seek(xnbPos, SeekOrigin.Begin);

                ContentReader reader = orig_GetContentReaderFromXnb(self, originalAssetName, ref stream, xnbReader, platform, recordDisposableObject);
                ((CopyingStream) reader.BaseStream).Output = output;
                return reader;
            }

            private delegate void d_ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject);
            private static d_ctor_ContentReader orig_ctor_ContentReader;
            private static void ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject) {
                if (!Enabled) {
                    orig_ctor_ContentReader(self, manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
                    return;
                }

                // What... what is this? Where am I? *Who* am I?!
                stream = new CopyingStream(stream, null);
                orig_ctor_ContentReader(self, manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
            }

            public static bool? SupportsDxt1;
            private static bool orig_SupportsDxt1;
            private delegate bool d_get_SupportsDxt1(object self);
            private static d_get_SupportsDxt1 orig_get_SupportsDxt1;
            private static bool get_SupportsDxt1(object self) {
                return SupportsDxt1 ?? orig_SupportsDxt1;
            }

            public static bool? SupportsS3tc;
            private static bool orig_SupportsS3tc;
            private delegate bool d_get_SupportsS3tc(object self);
            private static d_get_SupportsS3tc orig_get_SupportsS3tc;
            private static bool get_SupportsS3tc(object self) {
                return SupportsS3tc ?? orig_SupportsS3tc;
            }

        }

    }
}
