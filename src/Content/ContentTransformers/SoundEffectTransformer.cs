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
using System.Runtime.Serialization;

namespace XnaToFna.ContentTransformers {
    public class SoundEffectTransformer : ContentTypeReader<SoundEffect> {

        private readonly static Type t_SoundEffect = typeof(SoundEffect);
        private readonly static FieldInfo f_Instances = typeof(SoundEffect).GetField("Instances", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static List<WeakReference> DummyReferences = new List<WeakReference>();

        protected override SoundEffect Read(ContentReader input, SoundEffect existing) {
            CopyingStream mixed = (CopyingStream) input.BaseStream;
            // Disable "copy on read" - we're writing to the output manually.
            mixed.Copy = false;

            long startPos = input.BaseStream.Position;

            // Let's just re-read the 4th byte...
            input.BaseStream.Seek(3, SeekOrigin.Begin);
            char platform = input.ReadChar();
            bool x360 = platform == 'x';
            input.BaseStream.Seek(startPos, SeekOrigin.Begin);

            // Assuming we can seek...
            using (BinaryWriter output = new BinaryWriter(mixed.Output, Encoding.UTF8, true)) {
                uint fmtLength = input.ReadUInt32();

                ushort format = ContentHelper.SwapEndian(x360, input.ReadUInt16());
                // We need the sample rate, but nothing else.
                ushort channels = ContentHelper.SwapEndian(x360, input.ReadUInt16());
                uint rate = ContentHelper.SwapEndian(x360, input.ReadUInt32());
                int dataLength; // Used in this scope later than in the nested scope.

                if (format == 0x0161 ||
                    format == 0x0166) {
                    // Convert xWMA (0x0161) and XMA2 (0x0166) to pcm_u8.

                    // First, we need the data size, so skip the fmt chunk.
                    input.BaseStream.Seek(fmtLength - 2 - 2 - 4, SeekOrigin.Current);
                    dataLength = input.ReadInt32();

                    // Then write default values into the output.
                    output.Write(18U);
                    output.Write((ushort) 0x0001); // Good old PCM.
                    output.Write((ushort) 1); // 1 channel per limitation in ContentHelper.ConvertAudio.
                    output.Write(rate); // Let's reuse the sample rate, what can go wrong?
                    output.Write(0U); // Bytes per second and block alignment. Luckily FNA doesn't seem to care for PCM.
                    output.Write((ushort) 0);
                    output.Write((ushort) 8); // We're converting everything to pcm_u8
                    output.Write((ushort) 0); // No extra fmt data.

                    long dataLengthPos = output.BaseStream.Position;
                    // Keep the size field empty for now, we'll fill it in a second.
                    output.Write(0U);

                    // Feed everything into ffmpeg.
                    input.BaseStream.Seek(startPos, SeekOrigin.Begin);
                    ContentHelper.ConvertAudio(input.BaseStream, mixed.Output, ContentHelper.GenerateSoundEffectFeeder(
                        input,
                        format == 0x0161 ? "XWMA" :
                        "WAVE",
                        format == 0x0166 ? 18U + 34U : // We craft our own XMA2 extra data.
                        fmtLength,
                        (uint) dataLength,
                        0U,
                        x360,
                        format == 0x0161 ? null : // Shouldn't contain extra data.
                        format == 0x0166 ? (Action<BinaryWriter>) ((ffmpegWriter) => {
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt16())); // size of header extra

                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt16())); // number of streams
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // channel mask
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // samples encoded
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // bytes per block
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // start
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // length
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // loop start
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32())); // loop length
                            ffmpegWriter.Write(input.ReadByte()); // loop count
                            ffmpegWriter.Write(input.ReadByte()); // version
                            ffmpegWriter.Write(ContentHelper.SwapEndian(x360, input.ReadUInt16())); // block count

                            // Skip any "extra" extra data.
                            input.BaseStream.Seek(fmtLength - 18 - 34, SeekOrigin.Current);

                            // Seek table luckily not required here.

                        }) :
                        null
                    ), 0);

                    // Now we can update the size field.
                    long offset = output.BaseStream.Position;
                    output.BaseStream.Seek(dataLengthPos, SeekOrigin.Begin);
                    output.Write(offset - dataLengthPos - 4);
                    output.BaseStream.Seek(offset, SeekOrigin.Begin);

                    goto End;
                }


                // If we've got a compatible codec, at least fix endianness.
                {
                    output.Write(fmtLength);
                    output.Write(format);

                    output.Write(channels);
                    output.Write(rate);

                    // Fix all other fields "blindly."
                    output.Write(ContentHelper.SwapEndian(x360, input.ReadUInt32()));
                    output.Write(ContentHelper.SwapEndian(x360, input.ReadUInt16()));
                    output.Write(ContentHelper.SwapEndian(x360, input.ReadUInt16()));
                    output.Write(ContentHelper.SwapEndian(x360, input.ReadUInt16()));

                    // Skip any fmt extra data.
                    output.Write(input.ReadBytes((int) fmtLength - 18));

                    // Just copy the data chunk.
                    dataLength = input.ReadInt32();
                    output.Write(dataLength);
                    output.Write(input.ReadBytes(dataLength));
                }

                End:
                // Write the tailing loop start, end and duration
                output.Write(input.ReadUInt32());
                output.Write(input.ReadUInt32());
                output.Write(input.ReadUInt32());
                // Finaly, re-enable copying and move on.
                mixed.Copy = true;

                // Let's just not return the sound.
                if (existing != null)
                    return existing;
                existing = (SoundEffect) FormatterServices.GetUninitializedObject(t_SoundEffect);
                f_Instances.SetValue(existing, DummyReferences);
                return existing;
            }

        }

    }
}
