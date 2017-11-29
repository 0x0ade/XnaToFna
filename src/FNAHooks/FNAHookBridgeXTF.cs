using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    public static class FNAHookBridgeXTF {
        public static Assembly FNA;
        public static Type t_FNAHookBridgeFNA;
        public readonly static Type t_FNAHookBridgeXTF = typeof(FNAHookBridgeXTF);

        public static void Init(Assembly fna) {
            FNA = fna;

            Dictionary<string, Func<ContentTypeReader>> typeCreators = (Dictionary<string, Func<ContentTypeReader>>)
                typeof(ContentTypeReaderManager).GetField("typeCreators", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            typeCreators["Microsoft.Xna.Framework.Content.EffectReader, Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"]
                = () => new EffectTransformer();
            typeCreators["Microsoft.Xna.Framework.Content.SoundEffectReader"] // Games somehow don't use the full name for this one...
                = () => new SoundEffectTransformer();

            t_FNAHookBridgeFNA = FNA.GetType(typeof(FNAHookBridgeFNA).FullName);
            RuntimeHelpers.RunClassConstructor(t_FNAHookBridgeFNA.TypeHandle);

            Exchange<d_GetContentReaderFromXnb>();
            Exchange<d_ctor_ContentReader>();
        }

        public static void Exchange<T>() where T : class {
            Type d_x = typeof(T);
            string name = d_x.Name.Substring(2);
            FieldInfo f_f_orig = t_FNAHookBridgeFNA.GetField("orig_" + name);
            FieldInfo f_f_hook = t_FNAHookBridgeFNA.GetField(name);
            Type d_f = f_f_hook.FieldType;
            FieldInfo f_x_orig = t_FNAHookBridgeXTF.GetField("orig_" + name);
            FieldInfo f_x_hook = t_FNAHookBridgeXTF.GetField(name);

            f_x_orig.SetValue(null, CastDelegate(f_f_orig.GetValue(null), d_x));
            f_f_hook.SetValue(null, CastDelegate(f_x_hook.GetValue(null), d_f));
        }

        public delegate ContentReader d_GetContentReaderFromXnb(ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject);
        public static d_GetContentReaderFromXnb orig_GetContentReaderFromXnb; // Taken from FNA half of the bridge.
        public static d_GetContentReaderFromXnb GetContentReaderFromXnb = (ContentManager self, string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject)
        => {
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
        };

        public delegate void d_ctor_ContentReader(ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject);
        public static d_ctor_ContentReader orig_ctor_ContentReader;  // Taken from FNA half of the bridge.
        public static d_ctor_ContentReader ctor_ContentReader = (ContentReader self, ContentManager manager, Stream stream, GraphicsDevice graphicsDevice, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject)
        => {
            // What... what is this? Where am I? *Who* am I?!
            stream = new CopyingStream(stream, null);
            orig_ctor_ContentReader(self, manager, stream, graphicsDevice, assetName, version, platform, recordDisposableObject);
        };

        public static Delegate CastDelegate(object raw, Type type) {
            Delegate source = raw as Delegate;
            if (source == null)
                return null;
            Delegate[] delegates = source.GetInvocationList();
            if (delegates.Length == 1)
                return Delegate.CreateDelegate(type, delegates[0].Target, delegates[0].Method);
            Delegate[] delegatesDest = new Delegate[delegates.Length];
            for (int i = 0; i < delegates.Length; i++)
                delegatesDest[i] = CastDelegate(delegates[i], type);
            return Delegate.Combine(delegatesDest);
        }

    }
}

