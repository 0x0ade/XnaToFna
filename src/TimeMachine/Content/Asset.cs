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

        public static void UpdateAsset(string path, BinaryReader reader, BinaryWriter writer) {
            Log($"[UpdateAsset] Updating content {path}");

            // TODO: [TimeMachine] Update XNB

            // File header should be XNB? (w, m, x, ...) across all versions.
            writer.Write(reader.ReadUInt32());

            // If the content version matches, no need to update.
            // If the content version is unsupported, ignore it.
            byte version = reader.ReadByte();
            if (version < 3 /* 3.0 */ || 4 /* 3.1 */ < version) {
                writer.Write(version);
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            reader.ReadByte();
            writer.Write((byte) 5);

            // TODO: [TimeMachine] Hunt down XNB metadata / structure differences

            // TODO: [TimeMachine] Diff FNA base type readers (4.0 Refresh) with those in Mono.XNA (3.0, incomplete)
            // https://github.com/FNA-XNA/FNA/tree/master/src/Content/ContentReaders
            // https://github.com/predominant/Mono.XNA/tree/master/src/Microsoft.Xna.Framework/Content/Readers

            reader.BaseStream.CopyTo(writer.BaseStream);

        }

    }
}
