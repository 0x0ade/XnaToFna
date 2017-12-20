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

        // Many thanks to Ethan Lee and everyone else involved in reverse-engineering XACT!
        // This is heavily based on FNA / FACT.

        public enum SoundBankEventType : uint {
            Stop = 0,
            PlayWave = 1,
            PlayWaveTrackVariation = 3,
            PlayWaveEffectVariation = 4,
            PlayWaveTrackEffectVariation = 6,
            Pitch = 7,
            Volume = 8,
            Marker = 9,
            PitchRepeating = 16,
            VolumeRepeating = 17,
            MarkerRepeating = 18
        }

        public const uint XSBHeader = 0x4B424453; // SDBK
        public const uint XSBHeaderX360 = 0x5344424B; // KBDS

        public static void UpdateSoundBank(string path, BinaryReader reader, BinaryWriter writer) {
            Log($"[UpdateSoundBank] Updating sound bank {path}");

            // Check SoundBank header against XSBHeader / XSBHeaderX360
            uint header = reader.ReadUInt32();
            bool x360 = header == XSBHeaderX360;
            writer.Write(XSBHeader);

            // We don't need to do anything if it's already in PC format.
            if (!x360) {
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }

            // SoundBank versions (content, tool)
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));

            // Checksum
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));

            // Timestamp - let's just swap it. What can go wrong?
            writer.Write(SwapEndian(x360, reader.ReadUInt64()));

            // Platform maybe?
            byte platform = reader.ReadByte();
            writer.Write((byte) 1);
            if ((x360 && platform != 3) ||
                (!x360 && platform != 1)) {
                Log($"[UpdateSoundBank] Possible platform mismatch! Platform: 0x{platform.ToString("X2")}; Big endian (X360): {x360}");
            }

            ushort cuesSimple = SwapEndian(x360, reader.ReadUInt16());
            writer.Write(cuesSimple);
            ushort cuesComplex = SwapEndian(x360, reader.ReadUInt16());
            writer.Write(cuesComplex);
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
            writer.Write(reader.ReadByte());
            ushort sounds = SwapEndian(x360, reader.ReadUInt16());
            writer.Write(sounds);
            long cueNamesLengthPos = reader.BaseStream.Position; // We need to update this afterwards.
            ushort cueNamesLength = SwapEndian(x360, reader.ReadUInt16());
            writer.Write(cueNamesLength);
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));

            long cuesSimplePosPos = reader.BaseStream.Position; // We need to update this afterwards.
            uint cuesSimplePos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(cuesSimplePos);
            long cuesComplexPosPos = reader.BaseStream.Position; // We need to update this afterwards.
            uint cuesComplexPos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(cuesComplexPos);
            long cueNamesPosPos = reader.BaseStream.Position; // We need to update this afterwards.
            uint cueNamesPos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(cueNamesPos);
            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
            long variationsPosPos = reader.BaseStream.Position; // We need to update this afterwards.
            uint variationsPos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(variationsPos);
            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
            long waveBankNamesPosPos = reader.BaseStream.Position; // We need to update this afterwards.
            uint waveBankNamesPos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(waveBankNamesPos);
            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
            long cueNamesIndicesPosPos = reader.BaseStream.Position; // We need to update this afterwards.
            uint cueNamesIndicesPos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(cueNamesIndicesPos);
            uint soundsPos = SwapEndian(x360, reader.ReadUInt32());
            writer.Write(soundsPos);

            writer.Write(reader.ReadBytes(64));

            // We don't care about what lies here...

            writer.Write(reader.ReadBytesUntil(soundsPos));
            for (ushort i = 0; i < sounds; i++) {
                long start = reader.BaseStream.Position;
                byte flags = reader.ReadByte();
                writer.Write(flags);

                byte clips = 1;

                writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                writer.Write(reader.ReadByte());
                writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                writer.Write(reader.ReadByte());
                ushort length = SwapEndian(x360, reader.ReadUInt16());
                writer.Write(length);

                if ((flags & 0x01) != 0x00) {
                    writer.Write(clips = reader.ReadByte());
                } else {
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    writer.Write(reader.ReadByte());
                }

                if ((flags & 0x0E) != 0x00) {
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                    ushort copies = 0;
                    if ((flags & 0x02) != 0x00)
                        copies += 1;
                    if ((flags & 0x04) != 0x00)
                        copies += clips;

                    for (ushort ci = 0; ci < copies; ci++) {
                        byte count = reader.ReadByte();
                        writer.Write(count);
                        for (byte cii = 0; cii < count; cii++)
                            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                    }
                }

                if ((flags & 0x10) != 0x00) {
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    byte count = reader.ReadByte();
                    writer.Write(count);
                    for (byte ci = 0; ci < count; ci++)
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                }

                if ((flags & 0x01) != 0x00) {
                    for (byte ci = 0; ci < clips; ci++) {
                        writer.Write(reader.ReadByte());
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                        writer.Write(reader.ReadByte());
                        writer.Write(reader.ReadByte());
                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    }

                    for (byte ci = 0; ci < clips; ci++) {
                        byte events = reader.ReadByte();
                        writer.Write(events);

                        for (byte ei = 0; ei < events; ei++) {
                            uint info = SwapEndian(x360, reader.ReadUInt32());
                            writer.Write(info);
                            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                            SoundBankEventType type = (SoundBankEventType) (info & 0x1F);

                            byte pad = reader.ReadByte();
                            if (pad != 0xFF)
                                Log($"[UpdateSoundBank] Expected 0xFF between event info and data, got 0x{pad.ToString("X2")} instead! ({i}, {ci}, {ei})");
                            writer.Write(pad);

                            ushort count = 0;
                            ushort variation = 0;
                            switch (type) {
                                case SoundBankEventType.Stop:
                                    writer.Write(reader.ReadByte());
                                    break;

                                case SoundBankEventType.PlayWave:
                                    writer.Write(reader.ReadByte());
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    break;

                                case SoundBankEventType.PlayWaveTrackVariation:
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                                    // I can't find any data confirming the following, but it fixes a few things?
                                    // The same thing seems to happen with the varying count and flags:
                                    // 1x32 instead of 2x16
                                    if (platform == 1) {
                                        count = SwapEndian(x360, reader.ReadUInt16());
                                        variation = SwapEndian(x360, reader.ReadUInt16());
                                    } else {
                                        variation = SwapEndian(x360, reader.ReadUInt16());
                                        count = SwapEndian(x360, reader.ReadUInt16());
                                    }
                                    writer.Write(count);
                                    writer.Write(variation);

                                    writer.Write(reader.ReadUInt32()); // Skip 4 unknown bytes.
                                    for (ushort cci = 0; cci < count; cci++) {
                                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                        writer.Write(reader.ReadByte());
                                        writer.Write(reader.ReadByte());
                                        writer.Write(reader.ReadByte());
                                    }
                                    break;

                                case SoundBankEventType.PlayWaveEffectVariation:
                                    writer.Write(reader.ReadByte());
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    // 4 floats, but we don't care.
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    break;

                                case SoundBankEventType.PlayWaveTrackEffectVariation:
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    // 4 floats, but we don't care.
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                                    // I can't find any data confirming the following, but it fixes a few things?
                                    // The same thing seems to happen with the varying count and flags:
                                    // 1x32 instead of 2x16
                                    if (platform == 1) {
                                        count = SwapEndian(x360, reader.ReadUInt16());
                                        variation = SwapEndian(x360, reader.ReadUInt16());
                                    } else {
                                        variation = SwapEndian(x360, reader.ReadUInt16());
                                        count = SwapEndian(x360, reader.ReadUInt16());
                                    }
                                    writer.Write(count);
                                    writer.Write(variation);

                                    // Skip 4 unknown bytes.
                                    writer.Write(reader.ReadUInt32());
                                    for (ushort cci = 0; cci < count; cci++) {
                                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                        writer.Write(reader.ReadByte());
                                        writer.Write(reader.ReadByte());
                                        writer.Write(reader.ReadByte());
                                    }
                                    break;

                                case SoundBankEventType.Pitch:
                                case SoundBankEventType.PitchRepeating:
                                case SoundBankEventType.Volume:
                                case SoundBankEventType.VolumeRepeating:
                                    byte eventFlags = reader.ReadByte();
                                    writer.Write(eventFlags);
                                    if ((eventFlags & 0x01) != 0x00) {
                                        // 3 floats, but we don't care.
                                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    } else {
                                        writer.Write(reader.ReadByte());
                                        // 2 floats, but we don't care.
                                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                        // Skip 5 unknown bytes.
                                        writer.Write(reader.ReadUInt32());
                                        writer.Write(reader.ReadByte());
                                        if (type == SoundBankEventType.PitchRepeating ||
                                            type == SoundBankEventType.VolumeRepeating) {
                                            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                        }
                                    }
                                    break;

                                case SoundBankEventType.Marker:
                                case SoundBankEventType.MarkerRepeating:
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    break;

                                default:
                                    // Um...
                                    break;
                            }

                        }

                    }
                }

                if (reader.BaseStream.Position < start + length) {
                    // Seek reader - the original SoundBank contains extra data we don't care about.
                    reader.BaseStream.Seek(start + length, SeekOrigin.Begin);
                } else if (reader.BaseStream.Position > start + length) {
                    Log($"[UpdateSoundBank] Warning: Length of sound data didn't match read data! Expect further errors with this soundbank. ({i})");
                }

            }

            if (cuesSimple != 0) {
                writer.Write(reader.ReadBytesUntil(cuesSimplePos));
                writer.Flush();
                cuesSimplePos = (uint) writer.BaseStream.Position;
                for (ushort i = 0; i < cuesSimple; i++) {
                    writer.Write(reader.ReadByte());
                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                }
            }

            if (cuesComplex != 0) {
                ushort variations = 0;
                writer.Write(reader.ReadBytesUntil(cuesComplexPos));
                writer.Flush();
                cuesComplexPos = (uint) writer.BaseStream.Position;
                for (ushort i = 0; i < cuesComplex; i++) {
                    byte flags = reader.ReadByte();
                    if ((flags & 0x04) == 0x00) {
                        variations++;
                    }
                    writer.Write(flags);
                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                    writer.Write(reader.ReadByte());
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    writer.Write(reader.ReadByte());
                }

                if (variations != 0) {
                    // Variation data seems to... vary... between X360 and PC.
                    writer.Write(reader.ReadBytesUntil(variationsPos));
                    writer.Flush();
                    variationsPos = (uint) writer.BaseStream.Position;
                    for (ushort i = 0; i < variations; i++) {
                        ushort count;
                        ushort flags;
                        if (platform == 1) {
                            count = SwapEndian(x360, reader.ReadUInt16());
                            flags = SwapEndian(x360, reader.ReadUInt16());
                        } else {
                            flags = SwapEndian(x360, reader.ReadUInt16());
                            count = SwapEndian(x360, reader.ReadUInt16());
                        }
                        writer.Write(count);
                        writer.Write(flags);
                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                        switch ((flags >> 3) & 0x07) {
                            case 0:
                                for (ushort ci = 0; ci < count; ci++) {
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                }
                                break;

                            case 1:
                                for (ushort ci = 0; ci < count; ci++) {
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(reader.ReadByte());
                                    writer.Write(reader.ReadByte());
                                }
                                break;

                            case 3:
                                for (ushort ci = 0; ci < count; ci++) {
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    // 3 floats, but we don't care.
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                    writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                                }
                                break;

                            case 4:
                                for (ushort ci = 0; ci < count; ci++) {
                                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                                    writer.Write(reader.ReadByte());
                                }
                                break;

                            default:
                                // Um...
                                break;
                        }
                    }
                }
            }

            // Keep track of name indices. Update the table in the file afterwards.
            uint[] cueNameIndices = new uint[cuesSimple + cuesComplex];
            writer.Write(reader.ReadBytesUntil(cueNamesPos));
            writer.Flush();
            cueNamesPos = (uint) writer.BaseStream.Position;
            cueNamesLength = 0; // This seems to be 0 in X360; let's just count on our own...
            for (int i = 0; i < cueNameIndices.Length; i++) {
                writer.Flush();
                cueNameIndices[i] = (uint) writer.BaseStream.Position;

                // If the name isn't empty, continue on.
                if (reader.PeekChar() != 0) {
                    for (int c; (c = reader.PeekChar()) != 0; cueNamesLength++)
                        writer.Write(reader.ReadByte());
                    writer.Write(reader.ReadByte()); 
                    cueNamesLength++;
                    continue;
                }

                // If the name is empty, replace it with a dummy.
                string name = $"Nameless Cue #{i}";
                cueNamesLength += (ushort) name.Length;
                writer.Write(name.ToCharArray());
                writer.Write(reader.ReadByte());
            }

            // Update the cue name table length and all other positions.
            writer.Flush();
            long offset = writer.BaseStream.Position;

            writer.BaseStream.Seek(cueNamesLengthPos, SeekOrigin.Begin);
            writer.Write(cueNamesLength);

            writer.Flush();
            writer.BaseStream.Seek(cueNamesIndicesPos, SeekOrigin.Begin);
            for (int i = 0; i < cueNameIndices.Length; i++)
                writer.Write(cueNameIndices[i]);

            writer.Flush();
            writer.BaseStream.Seek(cuesSimplePosPos, SeekOrigin.Begin);
            writer.Write(cuesSimplePos);

            writer.Flush();
            writer.BaseStream.Seek(cuesComplexPosPos, SeekOrigin.Begin);
            writer.Write(cuesComplexPos);

            writer.Flush();
            writer.BaseStream.Seek(cueNamesPosPos, SeekOrigin.Begin);
            writer.Write(cueNamesPos);

            writer.Flush();
            writer.BaseStream.Seek(variationsPosPos, SeekOrigin.Begin);
            writer.Write(variationsPos);

            writer.Flush();
            writer.BaseStream.Seek(waveBankNamesPosPos, SeekOrigin.Begin);
            writer.Write(waveBankNamesPos);

            writer.Flush();
            writer.BaseStream.Seek(cueNamesIndicesPosPos, SeekOrigin.Begin);
            writer.Write(cueNamesIndicesPos);

            writer.Flush();
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

            // Nothing else should come afterwards...
            reader.BaseStream.CopyTo(writer.BaseStream);
        }

    }
}
