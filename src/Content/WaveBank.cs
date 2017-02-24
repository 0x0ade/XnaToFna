using Mono.Cecil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public static partial class ContentHelper {

        public static class XWMAInfo {
            public static int[] BytesPerSecond = { 12000, 24000, 4000, 6000, 8000, 20000 };
            public static short[] BlockAlign = { 929, 1487, 1280, 2230, 8917, 8192, 4459, 5945, 2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462 };
        }

        public static void UpdateWaveBank(string path, BinaryReader reader, BinaryWriter writer) {
            Log($"[UpdateWaveBank] Updating wave bank {path}");

            uint offset;

            // WaveBank header and versions
            writer.Write(reader.ReadBytes(3 * 4));

            uint[] regionOffsets = new uint[5];
            uint[] regionLengths = new uint[5];
            long regionPosition = reader.BaseStream.Position; // Used to update the regions after conversion
            for (int i = 0; i < 5; i++) {
                regionOffsets[i] = reader.ReadUInt32();
                writer.Write(regionOffsets[i]);
                regionLengths[i] = reader.ReadUInt32();
                writer.Write(regionLengths[i]);
            }

            writer.Write(reader.ReadBytesUntil(regionOffsets[0] + 2)); // Offset + streaming flag (ushort)

            ushort flags = reader.ReadUInt16();
            writer.Write(flags);
            if ((flags & 2) == 2) {
                // Compact mode - abort!
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            uint count = reader.ReadUInt32();
            writer.Write(count);
            writer.Write(reader.ReadBytes(64)); // Name

            uint metaSize = reader.ReadUInt32();
            writer.Write(metaSize);
            writer.Write(reader.ReadUInt32()); // Name size
            writer.Write(reader.ReadUInt32()); // Alignment

            uint playRegionOffset = regionOffsets[4];
            if (playRegionOffset == 0)
                playRegionOffset = regionOffsets[1] + count * metaSize;


            uint[] duration = new uint[count];

            long[] playOffsetPos = new long[count]; // Used to update the offsets after conversion
            uint[] playOffset = new uint[count];
            uint[] playOffsetUpdated = new uint[count];

            long[] playLengthPos = new long[count]; // Used to update the lengths after conversion
            int[] playLength = new int[count];
            int[] playLengthUpdated = new int[count];

            long[] formatPos = new long[count]; // Used to update the codecs after conversion
            uint[] codec = new uint[count];
            uint[] channels = new uint[count];
            uint[] rate = new uint[count];
            uint[] align = new uint[count];
            uint[] depth = new uint[count];

            offset = regionOffsets[1];
            uint durationRaw;
            uint format = 0;
            // Metadata
            for (int i = 0; i < count; i++) {
                writer.Write(reader.ReadBytesUntil(offset));

                if (metaSize >= 4) {
                    durationRaw = reader.ReadUInt32();
                    writer.Write(durationRaw);
                    duration[i] = durationRaw >> 4;
                }
                if (metaSize >= 8) {
                    formatPos[i] = reader.BaseStream.Position;
                    writer.Write(format = reader.ReadUInt32());
                }
                if (metaSize >= 12) {
                    playOffsetPos[i] = reader.BaseStream.Position;
                    writer.Write(playOffset[i] = playOffsetUpdated[i] = reader.ReadUInt32());
                }
                if (metaSize >= 16) {
                    playLengthPos[i] = reader.BaseStream.Position;
                    writer.Write(((uint) (playLength[i] = playLengthUpdated[i] = (int) reader.ReadUInt32())));
                }
                if (metaSize >= 20)
                    writer.Write(reader.ReadUInt32());
                if (metaSize >= 24) {
                    writer.Write(reader.ReadUInt32());
                } else if (playLength[i] != 0)
                    playLength[i] = (int) regionLengths[4];

                offset += metaSize;
                playOffset[i] += playRegionOffset;

                codec[i] =      (format >> 0) &             ((1 << 2)  - 1);
                channels[i] =   (format >> 2) &             ((1 << 3)  - 1);
                rate[i] =       (format >> (2 + 3)) &       ((1 << 18) - 1);
                align[i] =      (format >> (2 + 3 + 18)) &  ((1 << 8)  - 1);
                depth[i] =      (format >> (2 + 3 + 18 + 8));
            }

            // Sound data
            for (int i = 0; i < count; i++) {
                writer.Write(reader.ReadBytesUntil(playOffset[i]));

                if (codec[i] != 1 && codec[i] != 3) {
                    writer.Write(reader.ReadBytes(playLength[i]));
                    continue;
                }

                offset = (uint) writer.BaseStream.Position;

                Action<Process> feeder = null;

                if (codec[i] == 3) // XWMA
                    feeder = delegate (Process ffmpeg) {
                        Stream ffmpegStream = ffmpeg.StandardInput.BaseStream;

                        using (BinaryWriter ffmpegWriter = new BinaryWriter(ffmpegStream, Encoding.ASCII, true)) {
                            short blockAlign =
                                align[i] > XWMAInfo.BlockAlign.Length ?
                                XWMAInfo.BlockAlign[align[i] & 0x0F] :
                                XWMAInfo.BlockAlign[align[i]];
                            int packets = playLength[i] / blockAlign;
                            int blocks = (int) Math.Ceiling(duration[i] / 2048D);
                            int blocksPerPacket = blocks / packets;
                            int spareBlocks = blocks - blocksPerPacket * packets;

                            ffmpegWriter.Write("RIFF".ToCharArray());
                            ffmpegWriter.Write(playLength[i] + 4 + 4 + 8 + 4 + 2 + 2 + 4 + 4 + 2 + 4 + 4 + 4 + packets * 4 + 4 + 4 - 8);
                            ffmpegWriter.Write("XWMAfmt ".ToCharArray());
                            ffmpegWriter.Write(0x12);
                            ffmpegWriter.Write((short) 0x0161);
                            ffmpegWriter.Write((short) channels[i]);
                            ffmpegWriter.Write(rate[i]);
                            ffmpegWriter.Write(
                                align[i] >= XWMAInfo.BytesPerSecond.Length ?
                                XWMAInfo.BytesPerSecond[align[i] >> 5] :
                                XWMAInfo.BytesPerSecond[align[i]]
                            );
                            ffmpegWriter.Write(blockAlign);
                            ffmpegWriter.Write(0x0F);
                            ffmpegWriter.Write("dpds".ToCharArray());
                            ffmpegWriter.Write(packets * 4);
                            for (int packet = 0, accu = 0; packet < packets; packet++) {
                                accu += blocksPerPacket * 4096;
                                if (spareBlocks > 0) {
                                    accu += 4096;
                                    --spareBlocks;
                                }
                                ffmpegWriter.Write(accu);
                            }
                            ffmpegWriter.Write("data".ToCharArray());
                            ffmpegWriter.Write(playLength[i]);
                            ffmpegWriter.Flush();
                        }

                        byte[] dataRaw = new byte[4096];
                        int sizeRaw;
                        long destination = reader.BaseStream.Position + playLength[i];
                        while (!ffmpeg.HasExited && reader.BaseStream.Position < destination) {
                            sizeRaw = reader.BaseStream.Read(dataRaw, 0, Math.Min(dataRaw.Length, (int) (destination - reader.BaseStream.Position)));
                            ffmpegStream.Write(dataRaw, 0, sizeRaw);
                            ffmpegStream.Flush();
                        }

                        ffmpegStream.Close();
                    };
                
                // What about xma?

                Log($"[UpdateWaveBank] Converting #{i}");
                ContentHelper.RunFFMPEG($"-y -i - -acodec pcm_u8 -f wav -", reader.BaseStream, writer.BaseStream, feeder: feeder, inputLength: playLength[i]);

                uint length = (uint) writer.BaseStream.Position - offset;
                offset = (uint) writer.BaseStream.Position;
                uint lengthOffset = length - (uint) playLength[i];

                // Update codec
                codec[i] = 0;
                if (formatPos[i] != 0) {
                    writer.BaseStream.Seek(formatPos[i], SeekOrigin.Begin);
                    writer.Write(format =
                        (codec[i] << 0) |
                        (channels[i] << 2) |
                        (rate[i] << (2 + 3)) |
                        (align[i] << (2 + 3 + 18)) |
                        (depth[i] << (2 + 3 + 18 + 8))
                    );
                }

                // Update length and all subsequent positions
                if (playLengthPos[i] != 0) {
                    writer.BaseStream.Seek(playLengthPos[i], SeekOrigin.Begin);
                    writer.Write(playLengthUpdated[i] = (int) length);
                }
                for (int ii = i + 1; ii < count; ii++) {
                    if (playOffsetPos[ii] != 0) {
                        writer.BaseStream.Seek(playOffsetPos[ii], SeekOrigin.Begin);
                        writer.Write(playOffsetUpdated[ii] += lengthOffset);
                    }
                }

                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
            }

            offset = (uint) writer.BaseStream.Position;

            // Rewrite regions
            regionLengths[4] = offset - regionOffsets[4];
            writer.BaseStream.Seek(regionPosition, SeekOrigin.Begin);
            for (int i = 0; i < 5; i++) {
                writer.Write(regionOffsets[i]);
                writer.Write(regionLengths[i]);
            }

            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

            reader.BaseStream.CopyTo(writer.BaseStream);

        }

    }
}
