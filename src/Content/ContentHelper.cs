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

        public static bool IsFFMPEGAvailable {
            get {
                try {
                    Process which = new Process();
                    which.StartInfo = new ProcessStartInfo {
                        FileName =
                            (PlatformHelper.Current & Platform.Windows) != 0 ? "where" :
                            "which",
                        Arguments = "ffmpeg",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    which.Start();
                    which.WaitForExit();
                    return which.ExitCode == 0;
                } catch {
                    return false;
                }
            }
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
