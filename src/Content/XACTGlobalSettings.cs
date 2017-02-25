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

        public enum CrossfadeType : byte {
            Linear,
            Logarithmic,
            EqualPower
        }

        public static void UpdateXACTSettings(string path, BinaryReader reader, BinaryWriter writer) {
            Log($"[UpdateXACTSettings] Updating XACT global settings {path}");
            // This is required as FNA doesn't yet support all crossfade types.

            // Settings header and versions
            writer.Write(reader.ReadBytes(4 + 3 * 2 + 8 + 1));

            ushort categories = reader.ReadUInt16();
            writer.Write(categories);
            writer.Write(reader.ReadBytes(6 * 2)); // Unused metadata
            uint categoriesPos = reader.ReadUInt32();
            writer.Write(categoriesPos);

            writer.Write(reader.ReadBytesUntil(categoriesPos));
            for (int i = 0; i < categories; i++) {
                writer.Write(reader.ReadBytes(1 + 2 * 2));

                byte flags = reader.ReadByte();
                CrossfadeType crossfadeType = (CrossfadeType) (byte) (flags & 0x07);
                int rest = flags >> 3;

                if (crossfadeType != CrossfadeType.Linear)
                    Log($"[UpdateXACTSettings] Category #{i + 1} uses unsupported crossfade type {Enum.GetName(typeof(CrossfadeType), crossfadeType)} - replacing with Linear");

                writer.Write((byte) (flags & ~0x07 | (byte) CrossfadeType.Linear));

                writer.Write(reader.ReadBytes(2 + 2 * 1));
            }

            reader.BaseStream.CopyTo(writer.BaseStream);

        }

    }
}
