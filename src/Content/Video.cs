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

        public static void UpdateVideo(string path, BinaryReader reader = null, BinaryWriter writer = null) {
            if (!IsFFMPEGAvailable) {
                Log("[UpdateVideo] FFMPEG is missing - won't convert unsupported video files");
                if (reader != null && writer != null)
                    reader.BaseStream.CopyTo(writer.BaseStream);
                return;
            }

            // Rename the .xnb as it only causes conflicts.
            string pathXnb = Path.ChangeExtension(path, "xnb");
            if (File.Exists(pathXnb))
                File.Move(pathXnb, pathXnb + "_");

            string pathOutput = Path.ChangeExtension(path, "ogv");
            // If not writing to a stream and the ogv already exists, keep the ogv.
            if (writer == null && (!string.IsNullOrEmpty(path) && File.Exists(pathOutput)))
                return;

            Log($"[UpdateVideo] Updating video {path}");

            if (writer == null)
                RunFFMPEG($"-i {(reader == null ? $"\"{path}\"" : "-")} -acodec libvorbis -vcodec libtheora \"{pathOutput}\"", reader?.BaseStream, null);
            else
                RunFFMPEG($"-y -i - -acodec libvorbis -vcodec libtheora -", reader.BaseStream, writer.BaseStream);
        }

    }
}
