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

        public enum CrossfadeType : byte {
            Linear,
            Logarithmic,
            EqualPower
        }

        public const uint XGSHeader = 0x46534758; // XGSF
        public const uint XGSHeaderX360 = 0x58475346; // FSGX

        public static void UpdateXACTSettings(string path, BinaryReader reader, BinaryWriter writer) {
            Log($"[UpdateXACTSettings] Updating XACT global settings {path}");
            // This is required as FNA doesn't yet support all crossfade types or X360 content.

            // Check settings header against XNBHeader / XNBHeaderX360
            uint header = reader.ReadUInt32();
            bool x360 = header == XGSHeaderX360;
            writer.Write(XGSHeader);

            // Settings versions (content, tool)
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));

            // The effect of this value is unknown, even in FACT.
            writer.Write(SwapEndian(x360, reader.ReadUInt16()));

            // Timestamp - let's just swap it. What can go wrong?
            writer.Write(SwapEndian(x360, reader.ReadUInt64()));

            // XACT version
            writer.Write(reader.ReadByte());

            ushort categories = SwapEndian(x360, reader.ReadUInt16());
            writer.Write(categories);

            if (!x360) {
                // Metadata that we don't really care about (yet?)
                writer.Write(reader.ReadBytes(6 * 2));
                // ... except for the categories position.
                uint categoriesPos = reader.ReadUInt32();
                writer.Write(categoriesPos);

                // We don't care about anything else up until the categories.
                writer.Write(reader.ReadBytesUntil(categoriesPos));
                for (int i = 0; i < categories; i++) {
                    writer.Write(reader.ReadBytes(1 + 2 * 2));

                    byte flags = reader.ReadByte();
                    CrossfadeType crossfadeType = (CrossfadeType) (byte) (flags & 0x07);

                    if (crossfadeType != CrossfadeType.Linear)
                        Log($"[UpdateXACTSettings] Category #{i + 1} uses unsupported crossfade type {Enum.GetName(typeof(CrossfadeType), crossfadeType)} ({(byte) crossfadeType}) - replacing with Linear");

                    writer.Write((byte) (flags & ~0x07 | (byte) CrossfadeType.Linear));

                    writer.Write(reader.ReadBytes(2 + 2 * 1));
                }

            } else {
                ushort variables = SwapEndian(x360, reader.ReadUInt16());
                writer.Write(variables);
                writer.Write(SwapEndian(x360, reader.ReadUInt16())); // blob 1
                writer.Write(SwapEndian(x360, reader.ReadUInt16())); // blob 2
                ushort rpcs = SwapEndian(x360, reader.ReadUInt16());
                writer.Write(rpcs);
                ushort dspPresets = SwapEndian(x360, reader.ReadUInt16());
                writer.Write(dspPresets);
                ushort dspParams = SwapEndian(x360, reader.ReadUInt16());
                writer.Write(dspParams);

                uint categoriesPos = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(categoriesPos);
                uint variablesPos = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(variablesPos);
                // We skip them right now even though we shouldn't.
                // FIXME: Fill gaps once FNA uses FACT.
                writer.Write(SwapEndian(x360, reader.ReadUInt32())); // blob 1
                writer.Write(SwapEndian(x360, reader.ReadUInt32())); // category name index positions
                writer.Write(SwapEndian(x360, reader.ReadUInt32())); // blob 2
                writer.Write(SwapEndian(x360, reader.ReadUInt32())); // variable name index positions
                writer.Write(SwapEndian(x360, reader.ReadUInt32())); // category name positions
                writer.Write(SwapEndian(x360, reader.ReadUInt32())); // variable name positions
                uint rpcPos = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(rpcPos);
                uint dpsPresetsPos = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(dpsPresetsPos);
                uint dspParamsPos = SwapEndian(x360, reader.ReadUInt32());
                writer.Write(dspParamsPos);

                // The names should be "safe" already, let's just ignore them...

                // We don't care about anything else up until the categories.
                writer.Write(reader.ReadBytesUntil(categoriesPos));
                for (int i = 0; i < categories; i++) {
                    writer.Write(reader.ReadByte());
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));

                    byte flags = reader.ReadByte();
                    CrossfadeType crossfadeType = (CrossfadeType) (byte) (flags & 0x07);

                    if (crossfadeType != CrossfadeType.Linear)
                        Log($"[UpdateXACTSettings] Category #{i + 1} uses unsupported crossfade type {Enum.GetName(typeof(CrossfadeType), crossfadeType)} ({(byte) crossfadeType}) - replacing with Linear");

                    writer.Write((byte) (flags & ~0x07 | (byte) CrossfadeType.Linear));

                    writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    writer.Write(reader.ReadBytes(2 * 1));
                }

                // Same goes to the variables and anything else.
                if (variablesPos != uint.MaxValue) {
                    writer.Write(reader.ReadBytesUntil(variablesPos));
                    for (int i = 0; i < variables; i++) {
                        writer.Write(reader.ReadByte());
                        // 3 floats, but we don't care.
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                    }
                }

                if (rpcPos != uint.MaxValue) {
                    writer.Write(reader.ReadBytesUntil(rpcPos));
                    for (int i = 0; i < rpcs; i++) {
                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                        byte points = reader.ReadByte();
                        writer.Write(points);
                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                        for (int pi = 0; pi < points; pi++) {
                            // 2 floats, but we don't care.
                            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                            writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                            writer.Write(reader.ReadByte());
                        }
                    }
                }

                if (dspParamsPos != uint.MaxValue) {
                    writer.Write(reader.ReadBytesUntil(dspParamsPos));
                    for (int i = 0; i < dspParams; i++) {
                        writer.Write(reader.ReadByte());
                        // 3 floats, but we don't care.
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                        writer.Write(SwapEndian(x360, reader.ReadUInt16()));
                    }
                }

                if (dpsPresetsPos != uint.MaxValue) {
                    writer.Write(reader.ReadBytesUntil(dpsPresetsPos));
                    for (int i = 0; i < dspPresets; i++) {
                        writer.Write(reader.ReadByte());
                        writer.Write(SwapEndian(x360, reader.ReadUInt32()));
                    }
                }

            }

            reader.BaseStream.CopyTo(writer.BaseStream);

        }

    }
}
