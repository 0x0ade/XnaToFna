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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    public static partial class ContentHelper {

        public static void TransformContent(string path) {
            if (Game == null)
                return;
            if (!File.Exists(path))
                // File got removed or renamed - possibly part of a song / video.
                return;

            Log($"[TransformContent] Transforming {path}");

            FNAContentManagerHooks.Hook();

            // This may fail horribly if the content is a ValueType (struct).
            object obj = Game.Content.Load<object>(path/*.Substring(0, path.Length - 4)*/);
            (obj as IDisposable)?.Dispose();
            Game.Content.Unload();

            // Replace .xnb with .tmp
            File.Delete(path);
            File.Move(path + ".tmp", path);

            // Update the size embedded in the .xnb.
            using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                // We know that the size is always past the header, version and flags.
                stream.Position = 6;
                writer.Write((uint) stream.Length);
            }

            FNAContentManagerHooks.Enabled = false;

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

            FNAContentManagerHooks.Enabled = true;
        }

        // Yo dawg, I heard you like patching...
        public static class FNAContentManagerHooks {

            private static bool IsHooked = false;
            public static bool Enabled = true;

            public static void Hook() {
                if (IsHooked)
                    return;
                IsHooked = true;

                Hook(typeof(ContentManager), "GetContentReaderFromXnb", out orig_GetContentReaderFromXnb);

                // Hooking constructors? Seems to work just fine on my machine(tm).
                orig_ctor_ContentReader =
                    typeof(ContentReader).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0]
                    .Detour<d_ctor_ContentReader>(
                        typeof(FNAContentManagerHooks).GetMethod("ctor_ContentReader", BindingFlags.Static | BindingFlags.NonPublic)
                    );

                Dictionary<string, Func<ContentTypeReader>> typeCreators = (Dictionary<string, Func<ContentTypeReader>>)
                    typeof(ContentTypeReaderManager).GetField("typeCreators", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                typeCreators["Microsoft.Xna.Framework.Content.EffectReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"]
                    = () => new EffectTransformer();
                typeCreators["Microsoft.Xna.Framework.Content.SoundEffectReader"] // Games somehow don't use the full name for this one...
                    = () => new SoundEffectTransformer();

                object gl = typeof(GraphicsDevice).GetField("GLDevice", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Game.GraphicsDevice);
                Type t_gl = gl.GetType();
                orig_SupportsDxt1 = (bool) t_gl.GetProperty("SupportsDxt1").GetValue(gl);
                Hook(t_gl, "get_SupportsDxt1", out orig_get_SupportsDxt1);
                orig_SupportsS3tc = (bool) t_gl.GetProperty("SupportsS3tc").GetValue(gl);
                Hook(t_gl, "get_SupportsS3tc", out orig_get_SupportsS3tc);
            }

            private static MethodBase Find(Type type, string name) {
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

            private static void Hook<T>(Type type, string name, out T trampoline) {
                trampoline =
                    Find(type, name)
                    .Detour<T>(
                        Find(typeof(FNAContentManagerHooks), name)
                    );
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
                    writer.Write((byte) (xnbReader.ReadByte() & ~0x80)); // Remove the compression flag.
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
