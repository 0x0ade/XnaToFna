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
        }

        // Yo dawg, I heard you like patching...
        public static class FNAContentManagerHooks {

            private static bool IsHooked = false;

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
                typeCreators["Microsoft.Xna.Framework.Content.SoundEffectReader"] // This somehow isn't the full name...
                    = () => new SoundEffectTransformer();
            }

            private static void Hook<T>(Type type, string name, out T trampoline) {
                trampoline =
                    type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Detour<T>(
                        typeof(FNAContentManagerHooks).GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    );
            }

            private delegate ContentReader d_GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject);
            private static d_GetContentReaderFromXnb orig_GetContentReaderFromXnb;
            private static ContentReader GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject) {
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
                // What... what is this? Where am I? *Who* am I?!
                stream = new CopyingStream(stream, null);
                orig_ctor_ContentReader(self, manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
            }

        }

    }
}
