using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Text;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    public static class FNAHooks {

        public static bool Enabled = true;

        private static bool Hooked = false;

        public static void Hook() {
            if (!Hooked)
                return;
            Hooked = true;

            // Set up FNA to use our custom transformers instead of the default readers.
            Dictionary<string, Func<ContentTypeReader>> typeCreators = (Dictionary<string, Func<ContentTypeReader>>)
                typeof(ContentTypeReaderManager).GetField("typeCreators", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            typeCreators["Microsoft.Xna.Framework.Content.EffectReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"]
                = () => new EffectTransformer();
            typeCreators["Microsoft.Xna.Framework.Content.SoundEffectReader"] // Games somehow don't use the full name for this one...
                = () => new SoundEffectTransformer();

            // Set up all required hooks. Don't store them, as they're permanent.
            Hook(typeof(ContentManager), out orig_GetContentReaderFromXnb);
            Hook(typeof(ContentReader), out orig_ctor_ContentReader);
        }

        internal static MethodBase Find(Type type, string name, List<Type> argTypes, bool hasSelf = true) {
            Type[] argTypeArray = argTypes.ToArray();

            MethodBase found = type.GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                argTypeArray,
                null
                );
            if (found != null)
                return found;

            if (name.StartsWith("ctor_")) {
                found = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    argTypeArray,
                    null
                );
                if (found != null)
                    return found;
            }

            if (name.StartsWith("get_") || name.StartsWith("set_")) {
                PropertyInfo prop = type.GetProperty(name.Substring(4), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (name[0] == 'g')
                    found = prop.GetGetMethod(true);
                else
                    found = prop.GetSetMethod(true);
                if (found != null)
                    return found;
            }

            // If we can't find a matching method, find it without the self parameter.
            argTypes.RemoveAt(0);
            return Find(type, name, argTypes, false);
        }

        internal static Detour Hook<T>(Type type, out T trampoline) where T : class {
            MethodBase from;
            MethodBase to;

            string name = typeof(T).Name.Substring(2);

            List<Type> argTypes =
                typeof(T)
                .GetMethod("Invoke")
                .GetParameters()
                .Select(arg => arg.ParameterType)
                .ToList();

            to = Find(typeof(T).DeclaringType, name, argTypes);
            from = Find(type, name, argTypes);

            Detour detour = new Detour(from, to);
            trampoline = detour.GenerateTrampoline<T>();
            return detour;
        }

        public delegate ContentReader d_GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject);
        public static d_GetContentReaderFromXnb orig_GetContentReaderFromXnb;
        public static ContentReader GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject) {
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

        public static Detour h_ctor_ContentReader;
        public delegate void d_ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject);
        public static d_ctor_ContentReader orig_ctor_ContentReader;
        public static void ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject) {
            // What... what is this? Where am I? *Who* am I?!
            stream = new CopyingStream(stream, null);
            orig_ctor_ContentReader(self, manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
        }

    }
}

