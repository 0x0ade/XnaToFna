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

        public static void Log(string txt) {
            Console.Write("[XnaToFna] [ContentHelper] ");
            Console.WriteLine(txt);
        }

        public static void UpdateContent(string path, bool patchWaveBanks = true, bool patchXACTSettings = true) {
            if (patchWaveBanks && path.EndsWith(".xwb")) {
                PatchContent(path, UpdateWaveBank);
                return;
            }

            if (patchXACTSettings && path.EndsWith(".xgs")) {
                PatchContent(path, UpdateXACTSettings);
                return;
            }

        }

        public static void PatchContent(string path, Action<string, BinaryReader, BinaryWriter> patcher) {
            File.Delete(path + ".tmp");
            using (Stream input = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(input))
            using (Stream output = File.OpenWrite(path + ".tmp"))
            using (BinaryWriter writer = new BinaryWriter(output))
                patcher(path, reader, writer);
            File.Delete(path);
            File.Move(path + ".tmp", path);
        }

        public static void RunFFMPEG(string args, Stream input, Stream output, Action<Process> feeder = null, long inputLength = 0) {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo = new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };
            ffmpeg.Start();

            ffmpeg.AsyncPipeErr();

            Thread inputPipeThread = new Thread((ThreadStart) (() => feeder(ffmpeg)) ?? delegate () {
                byte[] dataRaw = new byte[4096];
                int sizeRaw;
                Stream ffmpegInStream = ffmpeg.StandardInput.BaseStream;
                long offset = 0;
                while (!ffmpeg.HasExited && offset < inputLength) {
                    offset += sizeRaw = input.Read(dataRaw, 0, Math.Min(dataRaw.Length, (int) (inputLength - offset)));
                    ffmpegInStream.Write(dataRaw, 0, sizeRaw);
                    ffmpegInStream.Flush();
                }
                ffmpegInStream.Close();
            }) {
                IsBackground = true
            };
            inputPipeThread.Start();

            Stream ffmpegStream = ffmpeg.StandardOutput.BaseStream;

            byte[] data = new byte[1024];
            int size;
            while (!ffmpeg.HasExited) {
                size = ffmpegStream.Read(data, 0, data.Length);
                output.Write(data, 0, size);
            }
        }

    }
}
