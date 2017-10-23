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

        // Many thanks to Ethan Lee and everyone else involved for his reverse-engineering work that powers this!
        // This is heavily based on FNA / FACT.

        public static class XWMAInfo {
            public readonly static int[] BytesPerSecond = { 12000, 24000, 4000, 6000, 8000, 20000 };
            public readonly static short[] BlockAlign = { 929, 1487, 1280, 2230, 8917, 8192, 4459, 5945, 2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462 };
        }

        // Assume the same info - XMA is just WMA Pro, right? Right?! >.<
        public static class XMAInfo {
            public readonly static int[] BytesPerSecond = { 12000, 24000, 4000, 6000, 8000, 20000 };
            public readonly static short[] BlockAlign = { 929, 1487, 1280, 2230, 8917, 8192, 4459, 5945, 2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462 };
        }

        public const uint XWBHeader = 0x444E4257; // WBND
        public const uint XWBHeaderX360 = 0x57424E44; // DNBW

        public static void UpdateWaveBank(string path, BinaryReader reader, BinaryWriter writer) {
            if (!IsFFMPEGAvailable) {
                Log("[UpdateWaveBank] FFMPEG is missing - won't convert unsupported WaveBanks");
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            Log($"[UpdateWaveBank] Updating wave bank {path}");

            uint offset;

            // Check WaveBank header against XNBHeader / XNBHeaderX360
            uint header = reader.ReadUInt32();
            bool x360 = header == XWBHeaderX360;
            writer.Write(XWBHeader);

            // WaveBank versions (content, tool)
            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
            writer.Write(SwapEndian(x360, reader.ReadUInt32()));

            uint[] regionOffsets = new uint[5];
            uint[] regionLengths = new uint[5];
            long regionPosition = reader.BaseStream.Position; // Used to update the regions after conversion
            for (int i = 0; i < 5; i++) {
                regionOffsets[i] = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(regionOffsets[i]);
                regionLengths[i] = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(regionLengths[i]);
            }

            // We don't really care about what's going on here... but we should, especially taking X360 into account.

            writer.Write(reader.ReadBytesUntil(regionOffsets[0])); // Offset

            uint flags = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(flags);
            if ((flags & 0x00000002) == 0x00000002) {
                // Compact mode - abort!
                if (x360)
                    throw new InvalidDataException("Can't handle compact mode Xbox 360 wave banks - Content directory left in unstable state");
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            uint count = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(count);
            writer.Write(reader.ReadBytes(64)); // Name

            uint metaSize = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(metaSize);
            writer.Write(SwapEndian(x360, reader.ReadUInt32())); // Name size
            writer.Write(SwapEndian(x360, reader.ReadUInt32())); // Alignment

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
                // Whoops, we're leaving a bunch of data as-is...
                writer.Write(reader.ReadBytesUntil(offset));

                if (metaSize >= 4) {
                    durationRaw = SwapEndian(x360, reader.ReadUInt32());
                    writer.Write(durationRaw);
                    duration[i] = durationRaw >> 4;
                }
                if (metaSize >= 8) {
                    formatPos[i] = reader.BaseStream.Position;
                    writer.Write(format = SwapEndian(x360, reader.ReadUInt32()));
                }
                if (metaSize >= 12) {
                    playOffsetPos[i] = reader.BaseStream.Position;
                    writer.Write(playOffset[i] = playOffsetUpdated[i] = SwapEndian(x360, reader.ReadUInt32()));
                }
                if (metaSize >= 16) {
                    playLengthPos[i] = reader.BaseStream.Position;
                    writer.Write(((uint) (playLength[i] = playLengthUpdated[i] = (int) SwapEndian(x360, reader.ReadUInt32()))));
                }
                if (metaSize >= 20)
                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                if (metaSize >= 24) {
                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
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


            // Read "seek tables" if they exist. They're required for XMA support. (Thanks, xnb_parse!)
            uint[][] seekTables = new uint[count][];
            if ((flags & 0x00080000) == 0x00080000) {
                // Whoops, we're leaving a bunch of data as-is...
                writer.Write(reader.ReadBytesUntil(regionOffsets[2]));

                uint[] seekOffsets = new uint[count];
                for (int i = 0; i < count; i++) {
                    seekOffsets[i] = SwapEndian(x360, reader.ReadUInt32());
                    writer.Write(seekOffsets[i]);
                }

                offset = (uint) writer.BaseStream.Position;
                for (int i = 0; i < count; i++) {
                    writer.Write(reader.ReadBytesUntil(offset + seekOffsets[i]));
                    uint packetCount = SwapEndian(x360, reader.ReadUInt32());
                    writer.Write(packetCount);
                    uint[] data = seekTables[i] = new uint[packetCount];
                    for (int pi = 0; pi < packetCount; pi++) {
                        data[pi] = SwapEndian(x360, reader.ReadUInt32());
                        writer.Write(data[pi]);
                    }
                }
            }

            // Sound data
            for (int i = 0; i < count; i++) {
                writer.Write(reader.ReadBytesUntil(playOffset[i]));

                if (codec[i] != 1 && codec[i] != 3) {
                    writer.Write(reader.ReadBytes(playLength[i]));
                    continue;
                }

                offset = (uint) writer.BaseStream.Position;

                // We need to feed FFMPEG with correctly formatted data.
                Action<Process> feeder = null;

                if (codec[i] == 3) // XWMA
                    feeder = delegate (Process ffmpeg) {
                        Stream ffmpegStream = ffmpeg.StandardInput.BaseStream;

                        using (BinaryWriter ffmpegWriter = new BinaryWriter(ffmpegStream, Encoding.ASCII, true)) {
                            short blockAlign =
                                align[i] >= XWMAInfo.BlockAlign.Length ?
                                XWMAInfo.BlockAlign[align[i] & 0x0F] :
                                XWMAInfo.BlockAlign[align[i]];
                            int packets = playLength[i] / blockAlign;
                            int blocks = (int) Math.Ceiling(duration[i] / 2048D);
                            int blocksPerPacket = blocks / packets;
                            int spareBlocks = blocks - blocksPerPacket * packets;

                            ffmpegWriter.Write("RIFF".ToCharArray());
                            ffmpegWriter.Write(playLength[i] + 4 + 4 + 8 /**/ + 4 + 2 + 2 + 4 + 4 + 2 + 2 + 2 /**/ + 4 + 4 + packets * 4 /**/ + 4 + 4 - 8);
                            ffmpegWriter.Write("XWMAfmt ".ToCharArray());

                            ffmpegWriter.Write(18);
                            ffmpegWriter.Write((short) 0x0161);
                            ffmpegWriter.Write((short) channels[i]);
                            ffmpegWriter.Write(rate[i]);
                            ffmpegWriter.Write(
                                align[i] >= XWMAInfo.BytesPerSecond.Length ?
                                XWMAInfo.BytesPerSecond[align[i] >> 5] :
                                XWMAInfo.BytesPerSecond[align[i]]
                            );
                            ffmpegWriter.Write(blockAlign);
                            ffmpegWriter.Write((short) 0x0F);
                            ffmpegWriter.Write((short) 0x00);

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

                else if (codec[i] == 1) // XMA
                    feeder = delegate (Process ffmpeg) {
                        Stream ffmpegStream = ffmpeg.StandardInput.BaseStream;

                        using (BinaryWriter ffmpegWriter = new BinaryWriter(ffmpegStream, Encoding.ASCII, true)) {
                            short blockAlign =
                                align[i] >= XMAInfo.BlockAlign.Length ?
                                XMAInfo.BlockAlign[align[i] & 0x0F] :
                                XMAInfo.BlockAlign[align[i]];
                            int packets = playLength[i] / blockAlign;
                            int blocks = (int) Math.Ceiling(duration[i] / 2048D);
                            int blocksPerPacket = blocks / packets;
                            int spareBlocks = blocks - blocksPerPacket * packets;

                            uint[] seekData = seekTables[i];

                            ffmpegWriter.Write("RIFF".ToCharArray());
                            ffmpegWriter.Write(playLength[i] + 4 + 4 + 8 /**/ + 4 + 2 + 2 + 4 + 4 + 2 + 2 + 2 /**/ + 2 + 4 + 6 * 4 + 1 + 1 + 2 /**/ + 4 + 4 + seekData.Length * 4 /**/ + 4 + 4 - 8);
                            ffmpegWriter.Write("WAVEfmt ".ToCharArray());

                            ffmpegWriter.Write(18 + 34);
                            ffmpegWriter.Write((short) 0x0166);
                            ffmpegWriter.Write((short) channels[i]);
                            ffmpegWriter.Write(rate[i]);
                            ffmpegWriter.Write(
                                align[i] >= XMAInfo.BytesPerSecond.Length ?
                                XMAInfo.BytesPerSecond[align[i] >> 5] :
                                XMAInfo.BytesPerSecond[align[i]]
                            );
                            ffmpegWriter.Write(blockAlign);
                            ffmpegWriter.Write((short) 0x0F);
                            ffmpegWriter.Write((short) 34); // size of header extra

                            ffmpegWriter.Write((short) 1); // number of streams
                            ffmpegWriter.Write(channels[i] == 2 ? 3U : 0U); // channel mask
                            // The following values are definitely incorrect, but they should work until they don't.
                            ffmpegWriter.Write(0U); // samples encoded
                            ffmpegWriter.Write(0U); // bytes per block
                            ffmpegWriter.Write(0U); // start
                            ffmpegWriter.Write(0U); // length
                            ffmpegWriter.Write(0U); // loop start
                            ffmpegWriter.Write(0U); // loop length
                            ffmpegWriter.Write((byte) 0); // loop count
                            ffmpegWriter.Write((byte) 0x04); // version
                            ffmpegWriter.Write((short) 1); // block count

                            ffmpegWriter.Write("seek".ToCharArray());
                            ffmpegWriter.Write(seekData.Length * 4);
                            for (int si = 0; si < seekData.Length; si++) {
                                ffmpegWriter.Write(seekData[si]);
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

                Log($"[UpdateWaveBank] Converting #{i}");
                // FIXME: stereo causes "Hell Yeah!" to sound horrible with pcm_u8 and OpenAL to simply fail everywhere with pcm_s16le
                RunFFMPEG($"-y -i - -acodec pcm_u8 -ac 1 -f wav -", reader.BaseStream, writer.BaseStream, feeder: feeder, inputLength: playLength[i]);
                channels[i] = 1;

                uint length = (uint) writer.BaseStream.Position - offset;
                offset = (uint) writer.BaseStream.Position;
                uint lengthOffset = length - (uint) playLength[i];

                // Update codec and format
                codec[i] = 0;
                depth[i] = 0; // 0: pcm_u8; 1: pcm_s16le
                align[i] = 0;
                if (formatPos[i] != 0) {
                    writer.BaseStream.Seek(formatPos[i], SeekOrigin.Begin);
                    writer.Write(
                        ((codec[i] & ((1 << 2) - 1))    << 0) |
                        ((channels[i] & ((1 << 3) - 1)) << 2) |
                        ((rate[i] & ((1 << 18) - 1))    << (2 + 3)) |
                        ((align[i] & ((1 << 8) - 1))    << (2 + 3 + 18)) |
                        (depth[i]                       << (2 + 3 + 18 + 8))
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
