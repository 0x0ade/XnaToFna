using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    [RelinkTypeInTheMiddle]
    public class OldEffect : Effect {

        public EffectPool EffectPool { get; private set; }

        // New constructors

        public OldEffect(
            GraphicsDevice graphicsDevice,
            byte[] effectCode
        )
            : base(graphicsDevice, effectCode) {
        }

        protected OldEffect(
            OldEffect cloneSource
        )
            : base(cloneSource) {

        }

        // Old constructors

        public OldEffect(
            GraphicsDevice graphicsDevice,
            byte[] effectCode,
            CompilerOptions options,
            EffectPool pool
        ) : base(graphicsDevice, effectCode) {
            EffectPool = pool;
        }

        protected OldEffect(
            GraphicsDevice graphicsDevice,
            OldEffect cloneSource
        ) : base(cloneSource) {
            // TODO: [TimeMachine] Find out if the EffectPool carries over.
            EffectPool = (cloneSource as OldEffect)?.EffectPool;
        }

        public OldEffect(
            GraphicsDevice graphicsDevice,
            Stream effectCodeFileStream,
            CompilerOptions options,
            EffectPool pool
        ) : base(graphicsDevice, _ReadStream(effectCodeFileStream)) {
            EffectPool = pool;
        }

        public OldEffect(
            GraphicsDevice graphicsDevice,
            Stream effectCodeFileStream,
            int numberBytes,
            CompilerOptions options,
            EffectPool pool
        ) : base(graphicsDevice, _ReadStream(effectCodeFileStream, numberBytes)) {
            EffectPool = pool;
        }

        public OldEffect(
            GraphicsDevice graphicsDevice,
            string effectCodeFile,
            CompilerOptions options,
            EffectPool pool
        ) : base(graphicsDevice, File.ReadAllBytes(effectCodeFile)) {
            EffectPool = pool;
        }

        private readonly static byte[] _ReadStream_buffer = new byte[2048];
        private static byte[] _ReadStream(Stream stream, int length = -1) {
            using (MemoryStream ms = new MemoryStream()) {
                if (length < 0)
                    stream.CopyTo(ms);
                else {
                    int read;
                    while (ms.Position < length && (read = stream.Read(_ReadStream_buffer, 0, _ReadStream_buffer.Length)) > 0)
                        ms.Write(_ReadStream_buffer, 0, Math.Min(read, length - (int) ms.Position));
                }
                return ms.ToArray();
            }
        }

        public void CommitChanges() {
            for (int i = CurrentTechnique.Passes.Count - 1; i > -1; --i)
                CurrentTechnique.Passes[i].Apply();
        }

        // TODO: [TimeMachine] Find out which SaveStateMode is the default one.
        public void Begin() => Begin(SaveStateMode.None);
        public void Begin(SaveStateMode saveStateMode) {
            // TODO: [TimeMachine] Implement SaveStateMode.SaveState
            if (saveStateMode == SaveStateMode.SaveState)
                throw new NotSupportedException("SaveStateMode.SaveState currently not supported by XnaToFna");

            // Do nothing else - CurrentTechnique.Passes[0].Begin() gets called next.
        }

        public void End() {
            // Any state "resetting" will end up here.
        }

    }
}
