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
using XnaToFna;

namespace XnaToFna.TimeMachine {
    public static partial class ContentTimeMachine {

        public static void UpdateSoundBank(string path, BinaryReader reader, BinaryWriter writer) {
            Log($"[UpdateSoundBank] Updating sound bank {path}");

            // TODO: [TimeMachine] Update XACT SoundBank

            // File header should be SDBK across all versions.
            writer.Write(reader.ReadUInt32());

            // If the content version matches, no need to update.
            // If the content version is unsupported, ignore it.
            ushort version = reader.ReadUInt16();
            // Fun fact: Currently only 45 is "supported", and all we're doing is replace the version!
            if (version < 45 || 45 < version) {
                writer.Write(version);
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            writer.Write((ushort) 46);

            // Replace the tool version.
            reader.ReadInt16();
            writer.Write((ushort) 43);

            reader.BaseStream.CopyTo(writer.BaseStream);

        }

    }
}
