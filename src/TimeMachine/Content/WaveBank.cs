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

        public static void UpdateWaveBank(string path, BinaryReader reader, BinaryWriter writer) {
            if (!ContentHelper.IsFFMPEGAvailable) {
                Log("[UpdateWaveBank] FFMPEG is missing - won't convert unsupported WaveBanks");
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            Log($"[UpdateWaveBank] Updating wave bank {path}");

            uint offset;

            // File header should be WBND across all versions.
            writer.Write(reader.ReadUInt32());

            // If the content version matches, no need to update.
            // If the content version is unsupported, ignore it.
            uint version = reader.ReadUInt32();
            if (version < 41 || 43 < version) {
                writer.Write(version);
                reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }
            writer.Write((uint) 46);

            // Replace the tool version.
            if (version >= 42)
                reader.ReadUInt32();
            writer.Write((uint) 44);

            // Assuming that those stay the same across versions...
            uint[] regionOffsets = new uint[5];
            uint[] regionLengths = new uint[5];
            long regionPosition = reader.BaseStream.Position; // Used to update the regions after conversion
            for (int i = 0; i < 5; i++) {
                regionOffsets[i] = reader.ReadUInt32();
                writer.Write(regionOffsets[i]);
                regionLengths[i] = reader.ReadUInt32();
                writer.Write(regionLengths[i]);
            }

            // Move regions 2 (old waveEntryOffset) to 3 (new waveEntryOffset) for versions < 42
            if (version < 42) {
                uint tmp;

                tmp = regionOffsets[3];
                regionOffsets[3] = regionOffsets[2];
                regionOffsets[2] = tmp;

                tmp = regionLengths[3];
                regionLengths[3] = regionLengths[2];
                regionLengths[2] = tmp;
            }

            writer.Write(reader.ReadBytesUntil(regionOffsets[0] + 2)); // Offset + streaming flag (ushort)

            ushort flags = reader.ReadUInt16();
            writer.Write(flags);
            if ((flags & 2) == 2) {
                // Compact mode - abort!
                goto End;
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

            uint waveEntryOffset = regionOffsets[3];

            // TODO: [TimeMachine] Is any further manipulation required for old .xwbs?

            End:
            reader.BaseStream.CopyTo(writer.BaseStream);

            offset = (uint) writer.BaseStream.Position;

            // Rewrite regions
            regionLengths[4] = offset - regionOffsets[4];
            writer.BaseStream.Seek(regionPosition, SeekOrigin.Begin);
            for (int i = 0; i < 5; i++) {
                writer.Write(regionOffsets[i]);
                writer.Write(regionLengths[i]);
            }

            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

        }

    }
}
